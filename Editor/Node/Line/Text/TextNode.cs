using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [NodeMenu("Text", Order = 10)]
    public class TextNode : LineNode, ILineProvider
    {
        private DropdownField nameDropdownField;
        private IMGUI_TextField nameField;
        private IMGUI_TextField dialogueField;

        private string noneLabel = "None";
        private string speakerKey; // 마지막으로 사용 가능했던 이름 키
        private bool hasDialogueKeyError;

        public string dialogue => dialogueField.value;
        public string speaker
        {
            get
            {
                var useLocalization = VisualScriptingSettings.UseLocalization;

                return useLocalization ? nameDropdownField.value : nameField.value;
            }
        }

        public TextNode() : base() { }
        public TextNode(string guid) : base(guid) { }
        public TextNode(NodeData data) : base(data)
        {
            // 다운케스팅이 불가능한 경우
            if (data is not TextNodeData textNodeData)
            {
                // 타이틀과 위치만 설정
                return;
            }

            // 대화 대상의 이름 등록
            SetSpeakerName(textNodeData.speakerKey, textNodeData.speaker);

            // 대사 등록
            SetDialogue(textNodeData.dialogueKey, textNodeData.dialogue);
        }

        private void SetSpeakerName(string key, string value)
        {
            speakerKey = key;

            // 이름이 없는 경우
            if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value))
            {
                // 드롭다운의 값에만 null 표시
                nameDropdownField.value = noneLabel;
                return;
            }

            var useLocalization = VisualScriptingSettings.UseLocalization;
            string entryValue = GetSpeakerEntryValue(key); // 키 값을 통해 찾아온 이름
            string textValue = (useLocalization && entryValue != null) ? entryValue : value; // 키 값으로 가져올 이름이 있는 경우에만 해당 이름 사용(그 외엔 저장 당시의 이름 사용)
            string dropdownValue = (entryValue != null) ? entryValue : $"Error: key({key}) is not found"; // 키 값에 맞는 값이 없다면 오류를 이름으로 설정

            // 이름 값 설정
            nameDropdownField.SetValueWithoutNotify(dropdownValue);
            nameField.SetValueWithoutNotify(textValue);

            // 오류 여부 수정
            hasDialogueKeyError = (entryValue == null);
        }

        private string GetSpeakerEntryValue(string key)
        {
            var useLocalization = VisualScriptingSettings.UseLocalization;
            if (!useLocalization)
            {
                return null;
            }

#if USE_LOCALIZATION
            // 현재 설정된 테이블 가져오기
            var state = VisualScriptingGraphState.instance;
            var entry = state.nameTable?.GetEntry(key);

            return (entry != null) ? entry.Value : null;
#else
            // 로컬라이제이션을 사용한다고 변수가 선언되었으나, 에셋이 없는 경우 경고
            UnityEngine.Debug.LogWarning("Warning: Localization Setting is enabled, but no asset is assigned");
            return null;
#endif

        }

        private void SetDialogue(string key, string value)
        {
            // 키 값을 통해 대사 받아오기
            var useLocalization = VisualScriptingSettings.UseLocalization;
            var entryValue = GetDialogueEntryValue(key);
            var text = (entryValue != null) ? entryValue : $"Error: key({key}) is not found";

            // 대사 값 설정(로컬라이제이션을 사용하지 않는 경우 저장 당시의 값 사용)
            dialogueField.SetValueWithoutNotify((useLocalization && !string.IsNullOrEmpty(value)) ? text : value);
        }

        private string GetDialogueEntryValue(string key)
        {
            var useLocalization = VisualScriptingSettings.UseLocalization;
            if (!useLocalization)
            {
                return null;
            }

#if USE_LOCALIZATION
            // 현재 설정된 테이블 가져오기
            var state = VisualScriptingGraphState.instance;
            var entry = state.dialogueTable?.GetEntry(key);

            return (entry != null) ? entry.Value : null;
#else
            return null;
#endif
        }

        public Line ToLine()
        {
            var data = ToData() as TextNodeData;

            var useLocalization = VisualScriptingSettings.UseLocalization;
            var name = useLocalization ? data.speakerKey : data.speaker;
            var dialogue = useLocalization ? data.dialogueKey : data.dialogue;

            return new TextLine(data.guid, name, dialogue);
        }

        public override NodeData ToData()
        {
            var data = new TextNodeData();

            data.guid = guid;
            data.name = nodeName;
            data.pos = position;
            data.speakerKey = GetSpeakerKey();
            data.speaker = speaker == noneLabel ? "" : speaker;
            data.dialogueKey = GetDialogueKey();
            data.dialogue = dialogue;

            return data;
        }

        private string GetSpeakerKey()
        {
#if USE_LOCALIZATION
            var nameTable = VisualScriptingGraphState.instance.nameTable;

            // 테이블이 설정되어 있지 않는 경우 키 값 X
            if (nameTable == null) return null;

            return nameTable.Values
                        .Where(e => e.Value == speaker)
                        .Select(e => e.Key)
                        .FirstOrDefault();
#else
            // 로컬라이제이션 에셋이 없는 경우 빈 값 리턴
            return null;
#endif
        }

        private string GetDialogueKey()
        {
            return $"{VisualScriptingSettings.DialogueKeyPrefix}_{guid}";
        }

        protected override void OnEnable()
        {
            // 전체 설정값이 바뀌는 경우의 이벤트 설정
            VisualScriptingSettings.OnSettingChanged += UpdateNameFieldType;

            // 현제 파일의 설정값이 바뀌는 경우의 이벤트 설정
            VisualScriptingGraphState.OnSettingChanged += UpdateNameField;
            VisualScriptingGraphState.OnSettingChanged += UpdateDialogueField;
        }

        protected override void OnDisable()
        {
            // 이벤트 해제
            VisualScriptingSettings.OnSettingChanged -= UpdateNameFieldType;
            VisualScriptingGraphState.OnSettingChanged -= UpdateNameField;
            VisualScriptingGraphState.OnSettingChanged -= UpdateDialogueField;
        }

        public override void Draw()
        {
            base.Draw();

            var setting = VisualScriptingSettings.instance;

            // Input 연결 추가
            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "Prev";
            inputContainer.Add(inputPort);

            // Output 연결 추가
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outputPort.portName = "Next";
            outputContainer.Add(outputPort);

            // 이름 선택 드롭다운 추가(아래의 업데이트에서 목록 설정)
            nameDropdownField = new DropdownField("Name", new List<string>(), 0);
            nameDropdownField.value = noneLabel;
            nameDropdownField.RegisterValueChangedCallback(evt => speakerKey = GetSpeakerKey());
            nameDropdownField.AddToClassList("line-node__name-dropdown");
            extensionContainer.Add(nameDropdownField);

            // 로컬라이제이션을 사용하지 않는 경우 이름을 적을 필드 추가
            nameField = new IMGUI_TextField("Name");
            nameField.RegisterValueChangedCallback(evt => speakerKey = GetSpeakerKey());
            extensionContainer.Add(nameField);

            // 대사 선택 요소 추가(guid를 통해 Localization를 삽입 및 수정하는 방식)
            dialogueField = new IMGUI_TextField("Dialogue Text");
            dialogueField.multiline = true;
            dialogueField.AddToClassList("line-node__dialogue-field");
            extensionContainer.Add(dialogueField);

            // 이름 요소 display 업데이트
            UpdateNameField();

            RefreshExpandedState();
        }

        private void UpdateNameField()
        {
            // 현재 이름 타입을 업데이트
            UpdateNameFieldType();

            // 선택된 이름 업데이트(드롭다운 필드 값에 오류 문구가 있을 수 있으니 텍스트 필드 값으로 설정)
            SetSpeakerName(speakerKey, nameField.value);
        }

        private void UpdateNameFieldType()
        {
            var useLocalization = VisualScriptingSettings.UseLocalization;

            // 로컬라이제이션 사용 여부에 따라 이름 필드 바꾸기
            nameDropdownField.style.display = useLocalization ? DisplayStyle.Flex : DisplayStyle.None;
            nameField.style.display = useLocalization ? DisplayStyle.None : DisplayStyle.Flex;

#if USE_LOCALIZATION
            // 이름 선택 드롭다운 목록 재설정(Localization을 통해 이름 목록 불러오기)
            var nameTable = VisualScriptingGraphState.instance.nameTable;
            var nameList = new[] { noneLabel } // 이름 목록 + 이름 없음 포함
                .Concat(nameTable?.Values.Select(ste => ste.Value) ?? Enumerable.Empty<string>())
                .ToList();
            nameDropdownField.choices = nameList;
#else
            // 로컬라이제이션 에셋이 없는 경우에도 만약을 위해 이름 없음만 넣기
            nameDropdownField.choices = new List<string>() { noneLabel };
#endif
        }

        private void UpdateDialogueField()
        {
            // 오류로 인해 기존 대사를 가져오지 못한 경우에만 다시 불러오기
            if (!hasDialogueKeyError) return;

            // 키 값을 통해 대사 다시 불러오기
            SetDialogue(GetDialogueKey(), "");
        }
    }
}