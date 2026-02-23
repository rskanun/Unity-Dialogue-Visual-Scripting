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
    [FilePath("ProjectSettings/VisualScriptingGraphState.asset", FilePathAttribute.Location.ProjectFolder)]
    public class VisualScriptingGraphState : ScriptableSingleton<VisualScriptingGraphState>
    {
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

        public static void NotifySettingChanged()
        {
            OnSettingChanged?.Invoke();
        }

#if USE_LOCALIZATION
        public StringTableCollection nameTableCollection => currentFile.nameTableCollection;
        public StringTable nameTable => GetStringTable(nameTableCollection);

        public StringTableCollection dialogueTableCollection => currentFile.dialogueTableCollection;
        public StringTable dialogueTable => GetStringTable(dialogueTableCollection);

        public StringTableCollection selectionTableCollection => currentFile.selectionTableCollection;
        public StringTable selectionTable => GetStringTable(selectionTableCollection);

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