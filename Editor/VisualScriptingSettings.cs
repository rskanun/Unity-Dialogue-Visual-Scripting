using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

#if USE_LOCALIZATION
using UnityEngine.Localization;
using UnityEditor.Localization;
using UnityEngine.Localization.Settings;
#endif

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class VisualScriptingSettings : ScriptableObject
    {
        // 저장 파일 위치
        private const string FILE_DIRECTORY = "Assets/DVS_Editor";
        private const string FILE_PATH = "Assets/DVS_Editor/VisualScriptingSettings.asset";

        private static VisualScriptingSettings _instance;
        public static VisualScriptingSettings Instance
        {
            get
            {
                if (_instance != null) return _instance;

                // Asset 폴더 탐색
                string guid = AssetDatabase.FindAssets("t:VisualScriptingSettings").FirstOrDefault();
                if (guid != null)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    _instance = AssetDatabase.LoadAssetAtPath<VisualScriptingSettings>(path);
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
                    _instance = CreateInstance<VisualScriptingSettings>();
                    AssetDatabase.CreateAsset(_instance, FILE_PATH);
                }
                return _instance;
            }
        }

#if USE_LOCALIZATION
        /// <summary>
        /// 현재 프로젝트에 설정되어있는 대표 언어
        /// </summary>
        public static Locale ProjectLocale
        {
            get
            {
                // 설정된 초기 값이 있는 경우 해당 언어를 리턴
                if (LocalizationSettings.ProjectLocale != null)
                {
                    return LocalizationSettings.ProjectLocale;
                }

                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null) return null;

                // 없는 경우 가장 첫번째에 있는 언어 리턴
                return settings.GetAvailableLocales().Locales.FirstOrDefault();
            }
        }
#endif

        public static event Action OnSettingChanged;
        public static readonly string LastOpenedFileKey = "VisualScripting.LastOpenedFilePath";

        [Space(10)]
        [Header("Visual Scripting Setting")]
        [SerializeField]
        private string _styleSheetDirectory = "Packages/Unity-Dialogue-Visual-Scripting/Styles";
        public static string StyleSheetDirectory => Instance._styleSheetDirectory;

        [SerializeField]
        private bool _useLocalization;
        private bool _prevLocalization;
        public static bool UseLocalization
        {
            get => Instance._useLocalization;
            set
            {
                if (Instance._useLocalization == value)
                {
                    return;
                }

                Instance._prevLocalization = Instance._useLocalization = value;
                NotifySettingChanged();
            }
        }

        [Header("Text Node Setting")]
        [SerializeField]
        private string _dialogueKeyPrefix = "Dialogue";
        public static string DialogueKeyPrefix => Instance._dialogueKeyPrefix;

        [Header("Select Node Setting")]
        [SerializeField]
        private int _maxChoice = 3;
        public static int MaxChoice => Instance._maxChoice;

        [SerializeField]
        private string _selectOptionKeyPrefix = "Option";
        public static string SelectOptionKeyPrefix => Instance._selectOptionKeyPrefix;

        private void OnValidate()
        {
            if (_prevLocalization == _useLocalization)
            {
                return;
            }

            _prevLocalization = _useLocalization;
            NotifySettingChanged();
        }

        private static void NotifySettingChanged()
        {
            OnSettingChanged?.Invoke();
        }
    }
}