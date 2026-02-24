using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class VisualScriptingProvider : SettingsProvider
    {
        private SerializedObject serializedSettings;
        private ReorderableList resolutionList;

        public VisualScriptingProvider(string path, SettingsScope scopes = SettingsScope.Project) : base(path, scopes) { }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new VisualScriptingProvider("Project/Dialogue Visual Scripting", SettingsScope.Project)
            {
                label = "Dialogue Visual Scripting",
                keywords = new HashSet<string>() { "Dialogue", "Visual Scripting", "DVS" }
            };
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            var settings = VisualScriptingSettings.instance;
            settings.hideFlags &= ~HideFlags.NotEditable; // 수정 불가 삭제

            serializedSettings = new SerializedObject(settings);
            resolutionList = null;
        }

        public override void OnGUI(string searchContext)
        {
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
                VisualScriptingSettings.instance.Save();

                // 변경사항 알림
                VisualScriptingGraphState.NotifySettingChanged();
            }
        }

        private void DrawGeneralSettings(SerializedObject serializedSettings)
        {
            DrawHeader("General Settings");
            DrawTextField(serializedSettings, "_styleSheetDirectory", "Style Sheet Directory");
            DrawLocalizationSettings(serializedSettings);
            DrawResolutionSettings(serializedSettings);
        }

        private void DrawLocalizationSettings(SerializedObject serializedSettings)
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

        private void DrawResolutionSettings(SerializedObject serializedSettings)
        {
            if (resolutionList == null)
            {
                var prop = serializedSettings.FindProperty("_previewerResolutions");
                if (prop == null) return;

                resolutionList = new ReorderableList(serializedSettings, prop, true, true, true, true);

                // 헤더
                resolutionList.drawHeaderCallback = (rect) =>
                {
                    EditorGUI.LabelField(rect, "Previewer Resolutions", EditorStyles.boldLabel);
                };

                // 요소 내부
                resolutionList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var currentProp = resolutionList.serializedProperty;
                    var element = currentProp.GetArrayElementAtIndex(index);

                    var nameProp = element.FindPropertyRelative("label");
                    var sizeProp = element.FindPropertyRelative("resolution");

                    rect.y += 2; // 상단 여백 아주 살짝 추가
                    float height = EditorGUIUtility.singleLineHeight;
                    float padding = 5f; // 위아래 간격

                    var nameRect = new Rect(rect.x, rect.y, rect.width, height);
                    var sizeRect = new Rect(rect.x, rect.y + height + padding, rect.width, height);

                    // 쪼갠 영역에 각각의 필드를 그림 (GUIContent.none으로 앞쪽 라벨 제거)
                    nameProp.stringValue = EditorGUI.TextField(nameRect, new GUIContent("Label"), nameProp.stringValue);
                    sizeProp.vector2Value = EditorGUI.Vector2Field(sizeRect, new GUIContent("Resolution"), sizeProp.vector2Value);
                };

                // 각 항목 높이 설정
                resolutionList.elementHeightCallback = (index) =>
                {
                    return (EditorGUIUtility.singleLineHeight * 2) + 8f;
                };
            }

            // 위쪽 요소로부터 약간 띄우기
            EditorGUILayout.Space(7f);

            // 넓이 조정
            using (new GUILayout.HorizontalScope())
            {
                // 오른쪽 채우기
                GUILayout.FlexibleSpace();

                using (new GUILayout.VerticalScope(GUILayout.Width(680f)))
                {
                    // 넓이에 맞춰 그리기
                    resolutionList.DoLayoutList();
                }

                // 왼쪽 채우기
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawTextNodeSettings(SerializedObject serializedSettings)
        {
            DrawHeader("Text Node Settings");
            DrawTextField(serializedSettings, "_dialogueKeyPrefix", "Dialogue Key Prefix");
        }

        private void DrawSelectNodeSettings(SerializedObject serializedSettings)
        {
            DrawHeader("Select Node Settings");
            DrawIntField(serializedSettings, "_maxChoice", "Max Choice");
            DrawTextField(serializedSettings, "_selectOptionKeyPrefix", "Select Option Key Prefix");
        }

        private void DrawLocalizationWarnings()
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

        private void DrawHeader(string title)
        {
            EditorGUILayout.Space(7);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private void DrawTextField(SerializedObject serializedSettings, string propName, string label)
        {
            var prop = serializedSettings.FindProperty(propName);
            if (prop == null) return;

            prop.stringValue = EditorGUILayout.TextField(new GUIContent(label), prop.stringValue);
        }

        private void DrawIntField(SerializedObject serializedSettings, string propName, string label)
        {
            var prop = serializedSettings.FindProperty(propName);
            if (prop == null) return;

            prop.intValue = EditorGUILayout.IntField(new GUIContent(label), prop.intValue);
        }

        private void DrawBoolField(SerializedObject serializedSettings, string propName, string label)
        {
            var prop = serializedSettings.FindProperty(propName);
            if (prop == null) return;

            prop.boolValue = EditorGUILayout.Toggle(new GUIContent(label), prop.boolValue);
        }
    }
}