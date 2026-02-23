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
    [FilePath("ProjectSettings/VisualScriptingSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class VisualScriptingSettings : ScriptableSingleton<VisualScriptingSettings>
    {
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

        [Header("General Settings")]
        [SerializeField]
        private string _styleSheetDirectory;
        public static string StyleSheetDirectory => instance._styleSheetDirectory;

        [SerializeField]
        private bool _useLocalization;
        public static bool UseLocalization => instance._useLocalization;

        [Header("Text Node Setting")]
        [SerializeField]
        private string _dialogueKeyPrefix = "Dialogue";
        public static string DialogueKeyPrefix => instance._dialogueKeyPrefix;

        [Header("Select Node Setting")]
        [SerializeField]
        private int _maxChoice = 3;
        public static int MaxChoice => instance._maxChoice;

        [SerializeField]
        private string _selectOptionKeyPrefix = "Option";
        public static string SelectOptionKeyPrefix => instance._selectOptionKeyPrefix;

        public static void NotifySettingChanged()
        {
            OnSettingChanged?.Invoke();
        }

        // 변경사항 저장
        public void Save()
        {
            Save(true);
        }
    }
}