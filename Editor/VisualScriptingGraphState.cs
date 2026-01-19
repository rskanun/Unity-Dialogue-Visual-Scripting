using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if USE_LOCALIZATION
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
#endif

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class VisualScriptingGraphState : ScriptableObject
    {
        // 저장 파일 위치
        private const string FILE_DIRECTORY = "Assets/VisualDialogueScripting";
        private const string FILE_PATH = "Assets/VisualDialogueScripting/VisualScriptingGraphState.asset";

        private static VisualScriptingGraphState _instance;
        public static VisualScriptingGraphState Instance
        {
            get
            {
                if (_instance != null) return _instance;

                // Asset 폴더 탐색
                string guid = AssetDatabase.FindAssets("t:VisualScriptingGraphState").FirstOrDefault();
                if (guid != null)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    _instance = AssetDatabase.LoadAssetAtPath<VisualScriptingGraphState>(path);
                }

                // Asset 폴더 내에 존재하지 않는 경우
                if (_instance == null)
                {
                    // 파일 경로가 없을 경우 폴더 생성
                    if (!AssetDatabase.IsValidFolder(FILE_DIRECTORY))
                    {
                        string[] folders = FILE_DIRECTORY.Split('/');
                        string currentPath = folders[0];

                        for (int i = 1; i < folders.Length; i++)
                        {
                            if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
                            {
                                AssetDatabase.CreateFolder(currentPath, folders[i]);
                            }
                            currentPath += "/" + folders[i];
                        }
                    }

                    // 해당 경로에 생성
                    _instance = CreateInstance<VisualScriptingGraphState>();
                    AssetDatabase.CreateAsset(_instance, FILE_PATH);
                }
                return _instance;
            }
        }

        // 설정값(Localization Table 값 등) 변경 이벤트
        public static event Action OnSettingChanged;

        private ScenarioGraph _currentFile;
        public ScenarioGraph currentFile
        {
            get => _currentFile;
            set => _currentFile = value;
        }

        private VisualScriptingGraphView _graphView;
        public VisualScriptingGraphView graphView
        {
            get => _graphView;
            set => _graphView = value;
        }

#if USE_LOCALIZATION
        public StringTableCollection nameTableCollection => currentFile.nameTableCollection;
        public StringTable nameTable => GetStringTable(nameTableCollection);

        public StringTableCollection dialogueTableCollection => currentFile.dialogueTableCollection;
        public StringTable dialogueTable => GetStringTable(dialogueTableCollection);

        public StringTableCollection selectionTableCollection => currentFile.selectionTableCollection;
        public StringTable selectionTable => GetStringTable(selectionTableCollection);

        public static void NotifySettingChanged()
        {
            OnSettingChanged?.Invoke();
        }

        private StringTable GetStringTable(StringTableCollection tableCollection)
        {
            // 로컬라이제이션을 사용하지 않는 경우 null 리턴
            if (!VisualScriptingSettings.UseLocalization)
            {
                return null;
            }

            var locale = VisualScriptingSettings.ProjectLocale;

            if (locale == null)
            {
                Debug.LogError("Localization Locale is not set up.");
                return null;
            }

            // 대표 언어를 기반으로 StringTable 리턴
            return tableCollection?.GetTable(locale.Identifier) as StringTable;
        }
#endif
    }
}