using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class IMGUI_TextField : VisualElement, INotifyValueChanged<string>
    {
        private readonly IMGUIContainer _container;
        private readonly Label _labelElement;

        private string _lastComposition = ""; // 조합 중인 글자 비교용
        private string _focusValue = ""; // 포커싱 이전 이후 비교용

        private bool _multiline;
        public bool multiline
        {
            get => _multiline;
            set
            {
                // 값이 바뀔 때에만 작동
                if (_multiline == value) return;

                // 줄바꿈 여부 갱신
                _multiline = value;

                // IMGUIContainer UI 갱신
                _container.MarkDirtyRepaint();
            }
        }
        private string _value;
        public string value
        {
            get => _value;
            set
            {
                if (_value == value) return;

                // 값 변경 시 ChangeEvent 수신 (UI Toolkit 표준 방식 처리)
                using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(_value, value))
                {
                    evt.target = this;
                    SetValueWithoutNotify(value);
                    SendEvent(evt);
                }
            }
        }

        private bool _isReadOnly;
        public bool isReadOnly
        {
            get => _isReadOnly;
            set
            {
                // 값이 바뀔 때에만 작동
                if (_isReadOnly == value) return;

                _isReadOnly = value;

                // 상태 변경에 따른 UI 갱신
                _container.MarkDirtyRepaint();
            }
        }

        public IMGUI_TextField()
        {
            _value = "";

            // TextField의 기존 스타일 적용
            AddToClassList("unity-base-field");
            AddToClassList("unity-base-text-field");
            AddToClassList("line-node__IMGUI-textfield");

            // IMGUIContainer 생성 및 내부에 IMGUI TextField 그리기
            _container = new IMGUIContainer(() =>
            {
                // 읽기 전용 설정
                GUI.enabled = !isReadOnly;

                // multiline 여부에 따른 줄바꿈 설정
                string value = multiline ? EditorGUILayout.TextArea(_value, GUILayout.Height(80)) : EditorGUILayout.TextField(_value);

                // 타이핑에 따른 내용 변화를 매순간 보이기(한글 전용)
                SetValueWithoutNotify(value);

                // 모음 또는 자음 삭제 시 변화 내용 캐치
                SetComposition(Input.compositionString);
            });

            // container 스타일 지정
            _container.AddToClassList("line-node__IMGUI-container");

            Add(_container);

            // 문장 완성 이벤트 추가
            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        public IMGUI_TextField(string labelText) : this()
        {
            // 라벨이 있는 경우 라벨 객체 추가
            _labelElement = new Label(labelText);
            _labelElement.AddToClassList(BaseField<string>.labelUssClassName);

            Insert(0, _labelElement);
        }

        public void SetValueWithoutNotify(string newValue)
        {
            if (_value == newValue) return;

            _value = newValue;

            // 다음 프레임에 그려지도록 알림
            _container.MarkDirtyRepaint();
        }

        private void SetComposition(string newValue)
        {
            if (_lastComposition == newValue) return;

            _lastComposition = newValue;

            // 다음 프레임에 그려지도록 알림
            _container.MarkDirtyRepaint();
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            _focusValue = value;
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            // 완성된 문장이 포커싱 이전과 같은 경우 이벤트 X
            if (value == _focusValue) return;

            using (ChangeEvent<string> cevt = ChangeEvent<string>.GetPooled(_focusValue, value))
            {
                cevt.target = this;
                _focusValue = value;
                SendEvent(cevt);
            }
        }
    }
}