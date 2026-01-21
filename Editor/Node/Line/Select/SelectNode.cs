using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [NodeMenu("Select", Order = 11)]
    public class SelectNode : LineNode, ILineProvider
    {
        private Dictionary<Port, IMGUI_TextField> choices = new();
        private Button addOptionButton;

        public SelectNode() : base() { }
        public SelectNode(string guid) : base(guid) { }
        public SelectNode(NodeData data) : base(data)
        {
            // 다운케스팅이 불가능한 경우
            if (data is not SelectNodeData selectNodeData)
            {
                // 타이틀과 위치만 설정
                return;
            }

            // 현재 있는 모든 출력 포트 제거
            foreach (var port in choices.Keys)
            {
                outputContainer.Remove(port);
            }

            // 리스트 초기화
            choices.Clear();

            // 데이터에 저장된 포트 추가
            var useLocalization = VisualScriptingSettings.UseLocalization;
            for (int i = 0; i < selectNodeData.options.Count; i++)
            {
                var key = selectNodeData.optionKeys[i];
                var name = selectNodeData.options[i];

                // 로컬라이제이션을 사용하면 키 값을 통해 가져오고, 아니라면 기존 입력값 사용
                string nameValue = useLocalization ? GetOptionName(key) : name;

                // 해당 내용을 토대로 옵션 출력 포트 추가
                AddOption(nameValue);
            }
        }

        private string GetOptionName(string key)
        {
            var optionTable = VisualScriptingGraphState.Instance.selectionTable;
            var entry = optionTable?.GetEntry(key);

            return entry != null ? entry.Value : $"Error: key({key}) is not found";
        }

        public Line ToLine()
        {
            var data = ToData() as SelectNodeData;

            var useLocalization = VisualScriptingSettings.UseLocalization;
            var options = useLocalization ? data.optionKeys : data.options;

            return new SelectLine(data.guid, options);
        }

        public override NodeData ToData()
        {
            var data = new SelectNodeData();

            data.guid = guid;
            data.name = nodeName;
            data.pos = position;
            data.optionKeys = GetOptionKeys();
            data.options = choices.Values.Select(field => field.value).ToList();

            return data;
        }

        private List<string> GetOptionKeys()
        {
            return Enumerable.Range(0, choices.Count)
                            .Select(i => $"{VisualScriptingSettings.SelectOptionKeyPrefix}_{guid}{i}")
                            .ToList();
        }

        public override void Draw()
        {
            base.Draw();

            // Output 추가 버튼
            addOptionButton = new Button(() => AddOption("Option Name"));
            addOptionButton.text = "Add Options";
            mainContainer.Insert(1, addOptionButton);

            // Input 연결 추가
            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "Prev";
            inputContainer.Add(inputPort);

            // 초기 선택지 하나 추가
            AddOption("Option Name");

            RefreshExpandedState();
        }

        private void AddOption(string option)
        {
            // 만들 수 있는 최대 선택지 개수 확인
            if (choices.Count >= VisualScriptingSettings.MaxChoice)
            {
                // 오버되면 만들기 X
                return;
            }

            // Output 연결 추가
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outputPort.portName = "";

            // Output의 이름을 선택지의 이름으로 설정
            var outputTextField = new IMGUI_TextField();
            outputTextField.value = option;
            outputTextField.RegisterValueChangedCallback(evt => NotifyModified());
            outputTextField.AddToClassList("line-node__select-textfield");
            outputPort.Add(outputTextField);

            // 옵션 제거 버튼
            var removeButton = new Button(() => RemoveOption(outputPort));
            removeButton.text = "X";
            outputPort.Add(removeButton);

            // 완성된 Output 포트 UI에 추가
            outputContainer.Add(outputPort);

            // 리스트에 추가
            choices.Add(outputPort, outputTextField);

            // 초기 옵션 생성이 아닌 추가적인 옵션을 생성한 경우
            if (choices.Count > 1)
            {
                // 노드 변경 알림
                NotifyModified();
            }
        }

        private void RemoveOption(Port outputPort)
        {
            // 옵션이 최소 하나의 값을 가지도록 1개 이하면 무시
            if (choices.Count <= 1) return;

            // 리스트 내에서 발견하지 못한 경우 무시
            if (!choices.Remove(outputPort)) return;

            // UI 상에서 포트 제거
            outputContainer.Remove(outputPort);

            // 노드 변경 알림
            NotifyModified();
        }
    }
}