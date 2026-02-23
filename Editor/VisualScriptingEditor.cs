using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if USE_LOCALIZATION
using UnityEngine.Localization.Settings;
#endif

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class VisualScriptingEditor : EditorWindow
    {
        private GraphData cacheData;
        private VisualScriptingGraphView graphView;

        // 그래프 상황
        private bool isLoading;
        private bool isDirty;

        [MenuItem("Window/Dialogue Visual Scripting")]
        public static void OpenWindow()
        {
            GetWindow<VisualScriptingEditor>("Dialogue Visual Scripting");
        }

        private async void OnEnable()
        {
            isLoading = true;

            // GraphView 생성하고 창에 추가
            ConstructGraphView();

            // 툴바 생성
            DrawToolbar();

            // 작업 중이던 데이터 다시 불러오기
            await RestoreEditorState();

            // 키다운 이벤트 등록
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

            isLoading = false;
        }

        private void OnDisable()
        {
            // 창 종료 시, GraphView 제거
            if (graphView != null)
            {
                // 그래프 뷰 제거
                rootVisualElement.Remove(graphView);
            }

            // 작업창 리로드 이벤트 제거
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;

            // 그래프 업데이트 이벤트 제거
            graphView.OnAnyNodeModified -= OnNodeModified;
            graphView.graphViewChanged -= OnGraphChanged;

            // 키다운 이벤트 등록
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnBeforeAssemblyReload()
        {
            // 플레이 모드 진입 시엔 작동하지 않고 넘어가기
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // 현재 작업 내역 임시 세이브
            graphView.SaveTempGraph(cacheData);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // 텍스트 필드 입력 중엔 무시
            if (evt.target is TextField || evt.target is IMGUIContainer)
            {
                return;
            }

            bool isPressedControlKey = (evt.modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0;
            if (evt.keyCode == KeyCode.S && isPressedControlKey)
            {
                // 에디터 저장 실행
                Save();
            }
        }

        private void ConstructGraphView()
        {
            graphView = new VisualScriptingGraphView();

            // GraphView 생성
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);

            // 현재 사용 중인 그래프 뷰로 등록
            VisualScriptingGraphState.instance.graphView = graphView;

            // 노드 탐색창 설정
            NodeSearchWindow.Initialize(graphView);

            // 그래프 업데이트 이벤트 추가
            graphView.OnAnyNodeModified += OnNodeModified;
            graphView.graphViewChanged += OnGraphChanged;
        }

        private void DrawToolbar()
        {
            // 툴바 생성
            var toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);

            // 툴바에 메뉴 추가
            toolbar.Add(DrawFileMenu());
            toolbar.Add(DrawEditManu());
        }

        private async Task RestoreEditorState()
        {
            if (cacheData == null)
            {
                // 마지막으로 열었던 파일 가져오기
                var lastOpenedFile = GetLastOpenedFile();

                // 마지막으로 연 파일이 없는 경우 임시 파일 할당
                if (lastOpenedFile == null)
                {
                    lastOpenedFile = CreateNewFile();
                }

                // 현재 파일로 설정
                SetCurrentFile(lastOpenedFile);
            }

#if USE_LOCALIZATION
            if (VisualScriptingSettings.UseLocalization)
            {
                // 로컬라이제이션 셋팅이 끝날 때까지 잠시 대기
                await LocalizationSettings.SelectedLocaleAsync.Task;
            }

#endif

            // 그래프 열기
            graphView.LoadGraph(cacheData);

            // 작업창 리로드 이벤트 추가
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        /// <summary>
        /// 저장 및 불러오기와 같이 파일과 관련된 메뉴 생성
        /// </summary>
        /// <returns></returns>
        private ToolbarMenu DrawFileMenu()
        {
            // 파일 메뉴(드롭다운 형식) 생성
            var menu = new ToolbarMenu();
            menu.text = "File";

            // 메뉴에 포함될 액션
            // 파일 생성
            menu.menu.AppendAction("New File", act => OpenNewFile());
            menu.menu.AppendAction("Open File", act => OpenFile());
            menu.menu.AppendSeparator();

            // 파일 저장
            menu.menu.AppendAction("Save File", act => Save());
            menu.menu.AppendAction("Save As...", act => SaveAs());

            return menu;
        }

        /// <summary>
        /// 새로운 파일을 현재 에디터에 열기
        /// </summary>
        public void OpenNewFile()
        {
            // 새 시나리오 파일 생성
            var newFile = CreateNewFile();

            // 새 파일을 현재 파일로 지정
            SetCurrentFile(newFile);

            // 새 파일 열기
            graphView.LoadGraph(newFile.graphData);

            // 변경사항 없음 알림
            MarkAsSaved();
        }

        private ScenarioGraph CreateNewFile()
        {
            // 임시로 사용될 새 파일 생성
            var newFile = CreateInstance<ScenarioGraph>();

            // 파일의 이름과 휘발되지 않도록 flag 설정
            newFile.name = "New Scenario";
            newFile.hideFlags = HideFlags.HideAndDontSave;

            return newFile;
        }

        /// <summary>
        /// 에셋 형태로 만들어둔 파일을 에디터로 불러오기
        /// </summary>
        public void OpenFile()
        {
            string path = EditorUtility.OpenFilePanel("Load File", "Assets", "asset");

            // 파일 유무 파악
            if (string.IsNullOrEmpty(path))
            {
                // 불러올 파일이 없는 경우 그대로 종료
                return;
            }

            // 유니티 상대 경로로 변환
            path = path.Substring(path.IndexOf("Assets"));

            // 파일 불러오기
            var currentFile = AssetDatabase.LoadAssetAtPath<ScenarioGraph>(path);

            // 해당 에셋이 Scenario 인지 확인
            if (currentFile == null)
            {
                // Scenario 객체가 아닌 경우 파일 열기 종료
                Debug.LogError("Unsupported asset type. This editor can only open 'Scenario' assets.");
                return;
            }

            // 현재 작업 파일을 로드한 파일로 변경
            SetCurrentFile(currentFile);
            EditorPrefs.SetString(VisualScriptingSettings.LastOpenedFileKey, path);

            // 현재 에디터에 그래프 열기
            graphView.LoadGraph(currentFile.graphData);

            // 변경사항 없음 알림
            MarkAsSaved();
        }

        /// <summary>
        /// 현재 열린 파일에 데이터 덮어씌우기
        /// </summary>
        public void Save()
        {
            var currentFile = VisualScriptingGraphState.instance.currentFile;

            // 만약 현재 열린 파일이 없다면, 다른 이름으로 저장
            if (!EditorUtility.IsPersistent(currentFile))
            {
                SaveAs();
                return;
            }

            // 현재 상황을 열려있는 파일에 저장
            graphView.SaveScenario(currentFile);

            // 저장된 파일의 내용을 캐시 데이터에도 복사
            RefreshCacheData(currentFile.graphData);

            // 에셋 파일에 덮어씌우기
            EditorUtility.SetDirty(currentFile);
            AssetDatabase.SaveAssets();

            // 저장되었음을 알림
            MarkAsSaved();
        }

        /// <summary>
        /// 경로상 파일에 데이터를 덮어씌우거나 새로 생성하기
        /// </summary>
        public void SaveAs()
        {
            var currentData = VisualScriptingGraphState.instance.currentFile;

            // 저장 위치
            string path = EditorUtility.SaveFilePanelInProject("Save File", "New Scenario", "asset", "Select a file to save the graph data.");

            // 저장 위치 유무 파악
            if (string.IsNullOrEmpty(path))
            {
                // 지정하지 않은 경우 그대로 종료
                return;
            }

            // 해당 경로로부터 에셋 데이터 가져오기
            var loadFile = AssetDatabase.LoadAssetAtPath<ScenarioGraph>(path);

            // 에셋 데이터 로드에 실패한 경우
            if (loadFile == null)
            {
                // 해당 경로에 에셋 만들기
                loadFile = CreateInstance<ScenarioGraph>();
                AssetDatabase.CreateAsset(loadFile, path);
            }

            // 로드한 에셋에 현재 데이터 덮어씌우기
            EditorUtility.CopySerialized(currentData, loadFile);

            // 현재 상태를 저장하려는 데이터에 저장
            graphView.SaveScenario(loadFile);

            // 파일 이름 할당
            loadFile.name = Path.GetFileNameWithoutExtension(path);

            // 현재 작업 파일을 저장한 파일로 변경
            SetCurrentFile(loadFile);
            EditorPrefs.SetString(VisualScriptingSettings.LastOpenedFileKey, path);

            // 변경 사항 기록
            EditorUtility.SetDirty(loadFile);
            AssetDatabase.SaveAssets();

            // 저장되었음을 알림
            MarkAsSaved();
        }

        private ToolbarMenu DrawEditManu()
        {
            //  메뉴(드롭다운 형식) 생성
            var menu = new ToolbarMenu();
            menu.text = "Edit";

            // 메뉴에 포함될 액션
            // 로컬라이제이션 세팅
            menu.menu.AppendAction("Localization Setting", act => GraphSettingEditor.ShowWindow());

            return menu;
        }

        private ScenarioGraph GetLastOpenedFile()
        {
            // 해당 컴퓨터에서 마지막으로 작업한 파일 위치를 EditorPrefs에서 가져오기
            string key = VisualScriptingSettings.LastOpenedFileKey;
            string path = EditorPrefs.GetString(key);

            // 해당 파일을 찾아 리턴
            return AssetDatabase.LoadAssetAtPath<ScenarioGraph>(path);
        }

        private GraphViewChange OnGraphChanged(GraphViewChange graphViewChange)
        {
            // 로딩 중이 아닌 경우
            if (!isLoading)
            {
                // 변경사항이 있음을 알림
                MarkAsDirty();
            }

            return graphViewChange;
        }

        private void OnNodeModified()
        {
            // 로딩 중이 아닌 경우
            if (!isLoading)
            {
                // 변경사항이 있음을 알림
                MarkAsDirty();
            }
        }

        private void MarkAsDirty()
        {
            // 이미 표식이 있다면 무시
            if (isDirty) return;

            isDirty = true;
            hasUnsavedChanges = true;
        }

        private void MarkAsSaved()
        {
            // 이미 표식이 없다면 무시
            if (!isDirty) return;

            isDirty = false;
            hasUnsavedChanges = false;
        }

        private void SetCurrentFile(ScenarioGraph file)
        {
            // 현재 파일 교체
            VisualScriptingGraphState.instance.currentFile = file;

            // 캐시 데이터를 해당 파일의 그래프 데이터로 고쳐쓰기
            RefreshCacheData(file.graphData);
        }

        private void RefreshCacheData(GraphData data)
        {
            // 그래프 데이터를 Json을 이용해 직렬화
            var json = JsonUtility.ToJson(data);

            // 다시 역직렬화를 사용하여 모든 데이터를 깊은 복사해 캐시 데이터에 붙여넣기
            cacheData = JsonUtility.FromJson<GraphData>(json);
        }
    }
}