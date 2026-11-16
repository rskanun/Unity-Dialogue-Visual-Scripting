using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.IO;
using UnityEditor.Localization.Plugins.XLIFF.V20;




#if USE_LOCALIZATION
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
#endif

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class VisualScriptingGraphView : GraphView
    {
        public event Action OnAnyNodeModified;

        public VisualScriptingGraphView()
        {
            // 뷰포인트 확대 및 축소, 콘텐츠의 이동 및 선택 등의 조작 추가
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 배경에 그리드 생성
            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);

            // 스타일 설정
            var graphStyle = StyleSheetManager.GetStyleSheet("GraphViewStyle.uss");
            styleSheets.Add(graphStyle);
            var nodeStyle = StyleSheetManager.GetStyleSheet("NodeStyle.uss");
            styleSheets.Add(nodeStyle);

            // 그래프 뷰 업데이트 이벤트 등록
            graphViewChanged += OnElementRemoved;
            serializeGraphElements = SerializeElements;
            canPasteSerializedData = CanPasteSerializedDataCallbacks;
            unserializeAndPaste = UnserializeAndPasteElements;
        }

        private GraphViewChange OnElementRemoved(GraphViewChange graphViewChange)
        {
            // 삭제된 요소가 있는 경우에만 발동
            if (graphViewChange.elementsToRemove != null)
            {
                var lineNodes = graphViewChange.elementsToRemove.OfType<LineNode>();
                foreach (var node in lineNodes)
                {
                    node.OnNodeModified -= NotifyAnyNodeModified;
                }
            }

            return graphViewChange;
        }

        private void NotifyAnyNodeModified()
        {
            OnAnyNodeModified?.Invoke();
        }

        private string SerializeElements(IEnumerable<GraphElement> elements)
        {
            var wrapper = new CopyPasteWrapper();
            var copyNodeGuids = new HashSet<string>();

            // LineNode 복사
            foreach (var e in elements)
            {
                if (e is not LineNode lineNode) continue;

                // NodeData로 만들어 직렬화
                wrapper.nodes.Add(lineNode.ToData());
                copyNodeGuids.Add(lineNode.guid);
            }

            // 복사된 개체가 없는 경우
            if (wrapper.nodes.Count == 0)
            {
                return string.Empty;
            }

            // Edge 복사
            foreach (var e in elements)
            {
                if (e is not Edge edge) continue;
                if (edge.output?.node is not LineNode outputNode) continue;
                if (edge.input?.node is not LineNode inputNode) continue;

                // 선택된 Node 간의 Edge만 복사
                if (!copyNodeGuids.Contains(outputNode.guid)) continue;
                if (!copyNodeGuids.Contains(inputNode.guid)) continue;

                wrapper.edges.Add(new EdgeData
                {
                    outputNodeGuid = outputNode.guid,
                    outputIndex = outputNode.outputContainer.IndexOf(edge.output),
                    inputNodeGuid = inputNode.guid,
                    inputIndex = inputNode.inputContainer.IndexOf(edge.input)
                });
            }

            return JsonUtility.ToJson(wrapper);
        }

        private bool CanPasteSerializedDataCallbacks(string data)
        {
            if (string.IsNullOrEmpty(data)) return false;

            try
            {
                var wrapper = JsonUtility.FromJson<CopyPasteWrapper>(data);
                return wrapper != null && wrapper.nodes != null && wrapper.nodes.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void UnserializeAndPasteElements(string operationName, string data)
        {
            var wrapper = JsonUtility.FromJson<CopyPasteWrapper>(data);
            if (wrapper == null || wrapper.nodes == null || wrapper.nodes.Count == 0)
            {
                return;
            }

            ClearSelection();

            // 기존 GUID -> 새 GUID 맵핑 테이블
            var guidMap = new Dictionary<string, string>(wrapper.nodes.Count);
            var graphNodeGuids = nodes.OfType<LineNode>().Select(n => n.guid).ToHashSet();
            foreach (var nodeData in wrapper.nodes)
            {
                if (nodeData == null) continue;

                // 중복인 경우에만 새로 등록
                string guid = graphNodeGuids.Contains(nodeData.guid) ? Guid.NewGuid().ToString() : nodeData.guid;
                guidMap[nodeData.guid] = guid;
            }

            // NodeData 기반 LineNode 생성
            var createdNodes = new Dictionary<string, LineNode>(wrapper.nodes.Count);
            foreach (var nodeData in wrapper.nodes)
            {
                if (nodeData == null) continue;

                // 새 GUID 할당 및 위치 살짝 옮기기
                string newGuid = guidMap[nodeData.guid];
                nodeData.guid = newGuid;
                nodeData.pos += new Vector2(30f, 30f);

                // 타입에 맞는 노드 생성
                var type = nodeData.NodeType;
                var node = Activator.CreateInstance(type, nodeData) as LineNode;

                // 알맞은 노드를 생성하지 못했다면 스킵
                if (node == null) continue;

                // 해당 노드에 이벤트 등록
                node.OnNodeModified += NotifyAnyNodeModified;

                AddElement(node);
                AddToSelection(node);
                createdNodes[node.guid] = node;
            }

            // 복제 노드 Edge 재연결
            foreach (var edgeData in wrapper.edges)
            {
                // 복제 노드와 연결되지 않은 Edge 차단
                if (!guidMap.TryGetValue(edgeData.outputNodeGuid, out string outputGuid)) continue;
                if (!guidMap.TryGetValue(edgeData.inputNodeGuid, out string inputGuid)) continue;

                // 생성 노드에 연결될 수 없는 Edge 차단
                if (!createdNodes.TryGetValue(outputGuid, out var outputNode)) continue;
                if (!createdNodes.TryGetValue(inputGuid, out var inputNode)) continue;

                // 포트 가져오기
                var outputPort = outputNode.outputContainer[edgeData.outputIndex] as Port;
                var inputPort = inputNode.inputContainer[edgeData.inputIndex] as Port;

                // 유효성 판단
                if (outputPort == null || inputPort == null) continue;

                // 투 포트를 연결하는 엣지 생성
                var newEdge = outputPort.ConnectTo(inputPort);
                AddElement(newEdge);
                AddToSelection(newEdge);
            }

            // 완료 라이프사이클 호출
            foreach (var node in createdNodes.Values)
            {
                node.OnLoadCompleted();
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new();

            bool isInExtension(Port port)
            {
                return port.parent == port.node.extensionContainer;
            }

            foreach (var port in ports)
            {
                // 우로보로스 차단
                if (startPort == port) continue;
                if (startPort.node == port.node) continue;
                if (startPort.direction == port.direction) continue;

                // extension끼리 연결되게 설정
                if (isInExtension(startPort) != isInExtension(port)) continue;

                compatiblePorts.Add(port);
            }

            return compatiblePorts;
        }

        /// <summary>
        /// 우클릭 메뉴 항목 관리
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            GraphContextMenu(evt);
            ElementContextMenu(evt);
        }

        /// <summary>
        /// 그래프 내에서 사용되는 메뉴
        /// </summary>
        private void GraphContextMenu(ContextualMenuPopulateEvent evt)
        {
            // 배경화면(?)에서만 해당 메뉴가 열리도록 설정
            if (evt.target is not GraphView)
            {
                return;
            }

            // 노드 메뉴에 등록될 어트리뷰트 목록 가져오기
            var types = TypeCache.GetTypesWithAttribute<NodeMenuAttribute>();
            var entries = types.Select(t => (type: t, attr: t.GetCustomAttribute<NodeMenuAttribute>()))
                            .OrderBy(p => p.attr.Order);

            // 생성될 메뉴가 없다면 종료
            if (types.Count <= 0) return;

            // 메뉴에 추가
            int prevOrder = entries.FirstOrDefault().attr.Order;
            foreach (var entry in entries)
            {
                // order 10 단위로 구분선 설치
                if ((prevOrder / 10) != (entry.attr.Order / 10))
                {
                    evt.menu.AppendSeparator("Create/");
                }

                // 이전 순서 저장
                prevOrder = entry.attr.Order;

                // Create 탭을 통해 생성하려는 객체 보이기
                var menuName = $"Create/{entry.attr.MenuName}";

                evt.menu.AppendAction(menuName, action => CreateNode(entry.type, action.eventInfo.mousePosition));
            }
        }

        /// <summary>
        /// 마우스 위치에 새로운 노드 생성
        /// </summary>
        private LineNode CreateNode(Type nodeType, Vector2 mousePosition)
        {
            // LineNode가 아닌 것들을 먼저 차단
            if (nodeType == null || nodeType.IsAssignableFrom(typeof(LineNode)))
            {
                Debug.LogError($"'{nodeType.Name}' does not inherit from LineNode.");
                throw new ArgumentException();
            }

            // 마우스 위치에 노드 생성
            var pos = contentViewContainer.WorldToLocal(mousePosition);

            // 노드 생성
            var node = Activator.CreateInstance(nodeType) as LineNode;
            node.SetPosition(new Rect(pos, new Vector2(350, 200)));

            // 해당 노드에 변경 이벤트 추가
            node.OnNodeModified += NotifyAnyNodeModified;

            // 노드 추가
            AddElement(node);

            // 노드 생성 알림
            OnAnyNodeModified?.Invoke();

            return node;
        }

        /// <summary>
        /// 그래프 내의 요소(ex: Node, Edge)에서 사용되는 메뉴
        /// </summary>
        private void ElementContextMenu(ContextualMenuPopulateEvent evt)
        {
            // Node나 Edge에 대해서만 해당 메뉴가 열리도록 설정
            if (evt.target is not Node && evt.target is not Edge)
            {
                return;
            }

            // 해당 요소 삭제
            evt.menu.AppendAction("Delete", action => DeleteSelection());
        }

        public void SaveScenario(ScenarioGraph graph)
        {
            var graphData = graph.graphData;

            // 로컬라이제이션 테이블 동기화를 위한 이전 노드 저장
            var oldNodes = new List<NodeData>(graph.graphData.nodes);

            // 시나리오 변환을 위해 그래프 저장
            SaveGraph(graphData);

            // 인게임에 쓰일 수 있는 시나리오 파일로 변환
            CompileScenario(graph);

#if USE_LOCALIZATION
            // 로컬라이제이션 업데이트
            SyncDialogueTable(graph.dialogueTableCollection, graphData.nodes, oldNodes);
            SyncSelectionTable(graph.selectionTableCollection, graphData.nodes, oldNodes);
#endif

            // 에셋 저장
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// GraphFile에 현재 그래프 정보 저장
        /// </summary>
        /// <param name="graphData">저장될 파일 에셋</param>
        public void SaveGraph(GraphData graphData)
        {
            // 그래프 뷰 저장
            graphData.viewScale = viewTransform.scale;
            graphData.viewPosition = viewTransform.position;

            // 노드 및 엣지 정보 저장
            SaveNode(graphData);
            SaveEdge(graphData);
        }

        private void SaveNode(GraphData graphData)
        {
            // 기존 데이터 초기화
            graphData.nodes.Clear();

            // 그래프에 있는 노드 데이터화
            foreach (var node in nodes)
            {
                // LineNode에 대해서만 저장
                if (node is not LineNode lineNode)
                {
                    continue;
                }

                // Node 내부에서 자체적으로 데이터화 시키기
                graphData.nodes.Add(lineNode.ToData());
            }
        }

        private void SaveEdge(GraphData graphData)
        {
            // 기존 데이터 초기화
            graphData.edges.Clear();

            // 그래프에 있는 엣지 데이터화
            foreach (var edge in edges)
            {
                var outputNode = edge.output.node as LineNode;
                var outputIndex = outputNode.outputContainer.IndexOf(edge.output);

                var inputNode = edge.input.node as LineNode;
                var inputIndex = inputNode.inputContainer.IndexOf(edge.input);

                graphData.edges.Add(new EdgeData()
                {
                    outputNodeGuid = outputNode.guid,
                    outputIndex = outputIndex,
                    inputNodeGuid = inputNode.guid,
                    inputIndex = inputIndex
                });
            }
        }

        public void LoadGraph(GraphData file)
        {
            // 현재 그래프에 있는 모든 요소 삭제
            DeleteElements(graphElements);

            // 그래프 뷰 되돌리기
            viewTransform.scale = file.viewScale;
            viewTransform.position = file.viewPosition;

            // 데이터 기반 노드 및 엣지 생성
            LoadNode(file);
            LoadEdge(file);

            // 모든 그래프 뷰 로드를 끝마쳤다면 노드에 알림
            foreach (var node in nodes.OfType<LineNode>())
            {
                node.OnLoadCompleted();
            }
        }

        private void LoadNode(GraphData file)
        {
            foreach (var data in file.nodes)
            {
                // 타입에 맞는 노드 생성
                var type = data.NodeType;
                var node = Activator.CreateInstance(type, data) as LineNode;

                // 알맞은 노드를 생성하지 못했다면 스킵
                if (node == null) continue;

                // 해당 노드에 이벤트 등록
                node.OnNodeModified += NotifyAnyNodeModified;

                // 노드 추가
                AddElement(node);
            }
        }

        private void LoadEdge(GraphData file)
        {
            var nodeGuidLookup = nodes.Select(node => node as LineNode)
                .Where(node => node != null)
                .ToDictionary(node => node.guid, node => node);

            foreach (var data in file.edges)
            {
                // guid가 일치하는 노드 가져오기
                if (!nodeGuidLookup.TryGetValue(data.outputNodeGuid, out var outputNode) ||
                    !nodeGuidLookup.TryGetValue(data.inputNodeGuid, out var inputNode))
                {
                    // 하나라도 현재 그래프에 없는 노드라면 엣지 생성 X
                    continue;
                }

                // 포트 가져오기
                var outputPort = outputNode.outputContainer[data.outputIndex] as Port;
                var inputPort = inputNode.inputContainer[data.inputIndex] as Port;

                // 투 포트를 연결하는 엣지 생성
                AddElement(outputPort.ConnectTo(inputPort));
            }
        }

        private void CompileScenario(ScenarioGraph graph)
        {
            if (graph == null) return;

            // 빌드 에셋 저장 경로
            var buildPath = GetBuildPath(graph);

            // 이전 빌드된 파일이 있는 지 탐색
            var scenario = AssetDatabase.LoadAssetAtPath<Scenario>(buildPath);
            bool isNewAsset = false;

            if (scenario == null)
            {
                // 이전에 빌드된 파일이 없는 경우 새로 생성
                scenario = ScriptableObject.CreateInstance<Scenario>();
                isNewAsset = true;
            }

            // 생성된 런타임 객체 시나리오에 데이터 저장
            BuildScenario(scenario);

#if USE_LOCALIZATION
            // 언어 테이블 데이터 저장
            scenario.nameTable = graph.nameTableCollection?.name;
            scenario.dialogueTable = graph.dialogueTableCollection?.name;
            scenario.selectionTable = graph.selectionTableCollection?.name;
#endif

            // 에셋 새로 만들기 또는 덮어쓰기 형태로 저장
            if (isNewAsset) AssetDatabase.CreateAsset(scenario, buildPath);
            else EditorUtility.SetDirty(scenario);

            // 빌드 처리
            graph.MarkAsBuilt();
        }

        private string GetBuildPath(ScenarioGraph graph)
        {
            if (graph == null) return null;

            // 기존 에셋 경로에 저장
            var assetPath = AssetDatabase.GetAssetPath(graph);
            var fileDir = Path.GetDirectoryName(assetPath);
            var fileName = graph.name + ".build.asset";
            return Path.Combine(fileDir, fileName);
        }

        private void BuildScenario(Scenario scenario)
        {
            if (scenario == null) return;

            // 시나리오의 시작인 태그로부터 시나리오 빌드
            var tagNodes = nodes.OfType<LineTag>();

            // 방문 노드
            var visits = new HashSet<string>();

            // 탐색 시작
            foreach (var tag in tagNodes)
            {
                // 시나리오 내 대본 구별 번호
                int num = tag.ID;

                // dfs 탐색 진행
                var stack = new Stack<LineNode>();
                stack.Push(tag);

                // 탐색을 진행하며 라인 객체 빌드 및 연결
                while (stack.Count > 0)
                {
                    var node = stack.Pop();

                    // 재방문 방지
                    if (visits.Contains(node.guid))
                    {
                        continue;
                    }

                    visits.Add(node.guid);

                    // 해당 노드의 출력 포트와 연결된 노드 탐색
                    var ports = node.outputContainer.Query<Port>().ToList();
                    var nextNodes = ports.SelectMany(p => p.connections)
                                    .Select(e => e.input.node)
                                    .OfType<LineNode>();

                    // 현재 노드가 라인 객체로 표현할 수 있는 경우
                    if (node is ILineProvider provider)
                    {
                        // 노드 정보를 토대로 라인 객체 생성
                        var line = provider.ToLine();

                        // 이어진 다음 노드와 연결
                        line.nextLineGuids = nextNodes.Select(node => node.guid).ToList();

                        // 시나리오에 추가
                        scenario.AddLine(num, line);
                    }

                    // 다음 탐색 노드 넣기
                    foreach (var next in nextNodes)
                    {
                        // 방문한 노드는 넘기기
                        if (visits.Contains(next.guid))
                            continue;

                        stack.Push(next);
                    }
                }
            }
        }

#if USE_LOCALIZATION
        private void SyncDialogueTable(StringTableCollection collection, List<NodeData> nodes, List<NodeData> oldNodes)
        {
            // 로컬라이제이션에 추가되거나 업데이트될 엔트리
            var entries = nodes.OfType<TextNodeData>()
                .ToDictionary(data => data.dialogueKey, data => data.dialogue);
            var delKeys = oldNodes.Except(nodes)
                .OfType<TextNodeData>()
                .Select(d => d.dialogueKey);

            // 로컬라이제이션 테이블 업데이트
            SyncTable(collection, entries, delKeys);
        }

        private void SyncSelectionTable(StringTableCollection collection, List<NodeData> nodes, List<NodeData> oldNodes)
        {
            // 로컬라이제이션에 추가되거나 업데이트될 엔트리
            var entries = nodes.OfType<SelectNodeData>()
                .SelectMany(data => data.optionKeys.Zip(data.options, (k, v) => new { k, v }))
                .ToDictionary(pair => pair.k, pair => pair.v);
            var delKeys = oldNodes.Except(nodes)
                .OfType<SelectNodeData>()
                .SelectMany(d => d.optionKeys);

            // 로컬라이제이션 테이블 업데이트
            SyncTable(collection, entries, delKeys);
        }

        private void SyncTable(StringTableCollection collection, Dictionary<string, string> entries, IEnumerable<string> delKeys)
        {
            if (collection == null) return;

            var locale = VisualScriptingSettings.ProjectLocale;
            var table = collection.GetTable(locale.Identifier) as StringTable;

            if (table == null || collection.SharedData == null) return;

            // 각 노드에 대한 로컬라이제이션 테이블 추가 및 수정
            foreach (var (k, v) in entries)
            {
                table.AddEntry(k, v);
            }

            // 더 이상 안 쓰이는 키 제거
            foreach (var key in delKeys)
            {
                table.SharedData.RemoveKey(key);
            }

            // 로컬라이제이션 변경 사항 저장
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }
#endif
    }
}