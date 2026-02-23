using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public abstract class LineNode : Node
    {
        public readonly string guid;
        public event Action OnNodeModified;

        private TextField nameField;
        public string nodeName
        {
            get => nameField?.value;
            private set => nameField?.SetValueWithoutNotify(value);
        }

        public Vector2 position
        {
            get
            {
                bool hasStyleLeft = style.left.keyword == StyleKeyword.Undefined;
                bool hasStyleTop = style.top.keyword == StyleKeyword.Undefined;

                if (hasStyleLeft && hasStyleTop)
                {
                    return new Vector2(style.left.value.value, style.top.value.value);
                }

                return GetPosition().position;
            }
            private set
            {
                SetPosition(new Rect(value, GetPosition().size));
            }
        }

        public LineNode() : this(Guid.NewGuid().ToString()) { }
        public LineNode(string guid)
        {
            // 각 노드마다 고유한 GUID 할당
            this.guid = guid;

            // 생성 시, 요소 그리기
            Draw();

            // 생성 호출 함수
            OnEnable();

            // 파괴 호출 함수
            RegisterCallback<DetachFromPanelEvent>(evt => OnDisable());

            // 노드 내 요소 변경 감지 등록
            RegisterModifiCallbacks();
        }
        public LineNode(NodeData data) : this(data.guid)
        {
            nodeName = data.name;
            position = data.pos;
        }

        public virtual void Draw()
        {
            titleButtonContainer.Clear();

            // 이름 필드
            nameField = new TextField() { value = "New Line" };
            nameField.RegisterValueChangedCallback(evt => nodeName = evt.newValue);
            titleContainer.Insert(0, nameField);

            // 스타일 적용
            nameField.AddToClassList("line-node__textfield");
            nameField.AddToClassList("line-node__textfield__hidden");

            extensionContainer.AddToClassList("line-node__extension-container");
        }

        private void RegisterModifiCallbacks()
        {
            // 모든 요소에 변경 이벤트 등록
            RegisterCallback<ChangeEvent<string>>(OnFieldChanged);
            RegisterCallback<ChangeEvent<int>>(OnFieldChanged);
            RegisterCallback<ChangeEvent<float>>(OnFieldChanged);
            RegisterCallback<ChangeEvent<double>>(OnFieldChanged);
            RegisterCallback<ChangeEvent<bool>>(OnFieldChanged);
            RegisterCallback<ChangeEvent<Enum>>(OnFieldChanged);
            RegisterCallback<ChangeEvent<Vector2>>(OnFieldChanged);
            RegisterCallback<ChangeEvent<Vector3>>(OnFieldChanged);
            RegisterCallback<ChangeEvent<Color>>(OnFieldChanged);
            RegisterCallback<ChangeEvent<UnityEngine.Object>>(OnFieldChanged);
        }

        private void OnFieldChanged<T>(ChangeEvent<T> evt)
        {
            NotifyModified();
        }

        protected void NotifyModified()
        {
            OnNodeModified?.Invoke();
        }

        /// <summary>
        /// 그래프 뷰의 모든 로드가 끝났을 때 실행될 함수
        /// </summary>
        public virtual void OnLoadCompleted() { }

        /// <summary>
        /// Node 객체가 생성될 때 실행되는 함수
        /// </summary>
        protected virtual void OnEnable() { }

        /// <summary>
        /// Node 객체가 파괴될 때 실행되는 함수
        /// </summary>
        protected virtual void OnDisable() { }

        public abstract NodeData ToData();
    }
}