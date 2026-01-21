using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if USE_LOCALIZATION
using UnityEditor.Localization;
#endif

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class GraphSettingEditor : EditorWindow
    {
        private Button selectedButton;

        public static void ShowWindow()
        {
            var window = GetWindow<GraphSettingEditor>();

            // 유니티 내장 톱니바퀴 아이콘 가져오기
            Texture icon = EditorGUIUtility.IconContent("SettingsIcon").image;

            // 제목에 아이콘 넣기
            window.titleContent = new GUIContent("Settings", icon);

            window.minSize = new Vector2(550, 300);
            window.Show();
        }

        private void CreateGUI()
        {
            var style = StyleSheetManager.GetStyle("SettingStyle.uss");
            rootVisualElement.styleSheets.Add(style);

            // 화면을 2개로 분할하여 하나엔 메뉴를, 다른 하나엔 내용을 띄우기
            var splitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            DrawMenu(splitView);
            DrawContent(splitView);
        }

        private void DrawMenu(TwoPaneSplitView splitView)
        {
            var menuContainer = new IMGUIContainer();

            var localButton = DrawMenuButton("Localization");
            menuContainer.Add(localButton);

            splitView.Add(menuContainer);
        }

        private Button DrawMenuButton(string menuName)
        {
            var menuButton = new Button(null);
            menuButton.text = menuName;
            menuButton.AddToClassList("settings-editor__menu-item-button");
            menuButton.clicked += () =>
            {
                if (selectedButton != null)
                    selectedButton.RemoveFromClassList("selected");

                selectedButton = menuButton;

                selectedButton.AddToClassList("selected");
            };

            return menuButton;
        }

        private void DrawContent(TwoPaneSplitView splitView)
        {
            var contentContainer = new IMGUIContainer();
            contentContainer.AddToClassList("settings-editor__content");

            // 제목
            var title = new Label("Localization");
            title.AddToClassList("settings-editor__content-title");
            contentContainer.Add(title);

#if USE_LOCALIZATION
            // 로컬라이제이션 테이블 목록
            var tableMap = LocalizationEditorSettings.GetStringTableCollections().ToDictionary(c => c.TableCollectionName);

            // 현재 데이터 파일 가져오기
            var currentFile = VisualScriptingGraphState.Instance.currentFile;

            // 이름을 담을 로컬라이제이션 드롭박스
            var nameDropdown = CreateTableDropdown("Name", currentFile.nameTableCollection, tableMap, table => currentFile.nameTableCollection = table);
            contentContainer.Add(nameDropdown);

            // 대사를 담을 로컬라이제이션 드롭박스
            var textDropdown = CreateTableDropdown("Text", currentFile.dialogueTableCollection, tableMap, table => currentFile.dialogueTableCollection = table);
            contentContainer.Add(textDropdown);

            // 선택지 담을 로컬라이제이션 드롭박스
            var selectionDropdown = CreateTableDropdown("Selection", currentFile.selectionTableCollection, tableMap, table => currentFile.selectionTableCollection = table);
            contentContainer.Add(selectionDropdown);
#else
            // ###########여기 수정#############
            var disabledLabel = new Label("Localization 기능이 비활성화 상태입니다.");
            disabledLabel.style.color = UnityEngine.Color.gray;
            contentContainer.Add(disabledLabel);
#endif

            splitView.Add(contentContainer);
        }

#if USE_LOCALIZATION
        private DropdownField CreateTableDropdown(string label, StringTableCollection selectedTable, Dictionary<string, StringTableCollection> tableMap, Action<StringTableCollection> setter)
        {
            var errorText = "Error: Could not found Table";
            var noValueText = "No Localization";

            var useLocalization = VisualScriptingSettings.UseLocalization;
            var selectValue = useLocalization == false ? noValueText : selectedTable?.TableCollectionName; // 로컬라이제이션을 사용하지 않을 때의 문구 따로 잓성
            selectValue ??= errorText; // 테이블을 읽어올 수 없는 경우 에러 문구 넣기

            // 로컬라이제이션 테이블 목록을 map으로부터 가져오기
            var list = tableMap.Keys.ToList();

            // 드롭다운 생성
            var dropdown = new DropdownField(label, list, selectValue);

            // 드롭다운 이벤트 등록
            dropdown.RegisterValueChangedCallback(evt =>
            {
                // map에서 테이블 찾기
                tableMap.TryGetValue(evt.newValue, out var table);

                // 해당 테이블 등록
                setter(table);

                // 설정 업데이트 알림
                VisualScriptingGraphState.NotifySettingChanged();
            });

            // 로컬라이제이션을 사용하지 않는 경우 읽기 전용으로 설정
            dropdown.SetEnabled(useLocalization);

            return dropdown;
        }
#endif
    }
}