using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class VisualScriptingProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/Dialogue Visual Scripting", SettingsScope.Project);

            provider.label = "Dialogue Visual Scripting";
            provider.guiHandler = GUIHandler;
            provider.keywords = new HashSet<string>()
            {
                "Dialogue", "Visual Scripting", "DVS"
            };

            return provider;
        }

        private static void GUIHandler(string searchContext)
        {
            var settings = VisualScriptingSettings.instance;
            var serializedSettings = new SerializedObject(settings);

            // UI 그리기
            serializedSettings.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space(5);

            var propStylePath = serializedSettings.FindProperty("_styleSheetDirectory");
            EditorGUILayout.PropertyField(propStylePath, new GUIContent("Style Sheet Directory"));

            var propUseLoc = serializedSettings.FindProperty("_useLocalization");
            propUseLoc.boolValue = EditorGUILayout.Toggle(new GUIContent("Use Localization"), propUseLoc.boolValue);

            // 값 변경 확인
            if (EditorGUI.EndChangeCheck())
            {
                // 변경 값을 실제 인스턴스에 적용
                serializedSettings.ApplyModifiedProperties();

                // 즉시 저장
                settings.Save();

                // 변경사항 알림
                VisualScriptingGraphState.NotifySettingChanged();
            }
        }
    }
}