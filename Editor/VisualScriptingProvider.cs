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
            return new SettingsProvider("Project/Dialogue Visual Scripting", SettingsScope.Project)
            {
                label = "Dialogue Visual Scripting",
                guiHandler = GUIHandler,
                keywords = new HashSet<string>() { "Dialogue", "Visual Scripting", "DVS" }
            };
        }

        private static void GUIHandler(string searchContext)
        {
            var settings = VisualScriptingSettings.instance;

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.Update();

            EditorGUI.BeginChangeCheck();

            // 레이아웃 시작
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(7);

                using (new GUILayout.VerticalScope())
                {
                    // 라벨 넓이 고정
                    float originWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 250f;

                    // 세션별 그리기 함수 호출
                    DrawGeneralSettings(serializedSettings);
                    DrawTextNodeSettings(serializedSettings);
                    DrawSelectNodeSettings(serializedSettings);

                    // 라벨 너비 복구
                    EditorGUIUtility.labelWidth = originWidth;
                }
            }

            // 저장 로직
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

        private static void DrawGeneralSettings(SerializedObject serializedSettings)
        {
            DrawHeader("General Settings");
            DrawTextField(serializedSettings, "_styleSheetDirectory", "Style Sheet Directory");
            DrawLocalizationSettings(serializedSettings);

        }

        private static void DrawLocalizationSettings(SerializedObject serializedSettings)
        {
            bool guiEnabled = true;

#if USE_LOCALIZATION
            // Locale 설정 여부에 따라 설정
            guiEnabled = VisualScriptingSettings.ProjectLocale != null;
#else
            // 로컬라이제이션 에셋이 없다면 사용 불가능하도록 설정
            guiEnabled = false;
#endif

            using (new EditorGUI.DisabledGroupScope(!guiEnabled))
            {
                DrawBoolField(serializedSettings, "_useLocalization", "Use Localization");
            }

            // 경고문 출력
            DrawLocalizationWarnings();
        }

        private static void DrawTextNodeSettings(SerializedObject serializedSettings)
        {
            DrawHeader("Text Node Settings");
            DrawTextField(serializedSettings, "_dialogueKeyPrefix", "Dialogue Key Prefix");
        }

        private static void DrawSelectNodeSettings(SerializedObject serializedSettings)
        {
            DrawHeader("Select Node Settings");
            DrawIntField(serializedSettings, "_maxChoice", "Max Choice");
            DrawTextField(serializedSettings, "_selectOptionKeyPrefix", "Select Option Key Prefix");
        }

        private static void DrawLocalizationWarnings()
        {
#if USE_LOCALIZATION
            // Locale 설정이 되어있지 않는 경우의 경고문
            if (VisualScriptingSettings.ProjectLocale == null)
            {
                EditorGUILayout.HelpBox(
                    "Project Locale is missing. Please ensure a valid Locale is selected in the Localization Settings.",
                    MessageType.Warning
                );
            }
#else
            // 해당 옵션 사용을 위해 에셋을 깔아야 한다는 경고문
            EditorGUILayout.HelpBox(
                "The 'Localization' package is required to use this feature.\n" +
                "Please install it via the Package Manager.",
                MessageType.Warning
            );
#endif
        }

        private static void DrawHeader(string title)
        {
            EditorGUILayout.Space(7);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawTextField(SerializedObject serializedSettings, string propName, string label)
        {
            var prop = serializedSettings.FindProperty(propName);
            if (prop == null) return;

            prop.stringValue = EditorGUILayout.TextField(new GUIContent(label), prop.stringValue);
        }

        private static void DrawIntField(SerializedObject serializedSettings, string propName, string label)
        {
            var prop = serializedSettings.FindProperty(propName);
            if (prop == null) return;

            prop.intValue = EditorGUILayout.IntField(new GUIContent(label), prop.intValue);
        }

        private static void DrawBoolField(SerializedObject serializedSettings, string propName, string label)
        {
            var prop = serializedSettings.FindProperty(propName);
            if (prop == null) return;

            prop.boolValue = EditorGUILayout.Toggle(new GUIContent(label), prop.boolValue);
        }
    }
}