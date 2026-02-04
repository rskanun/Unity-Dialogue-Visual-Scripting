using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class LineNodeField : BaseField<string>
    {
        private Label displayName;
        private Button pickerButton;

        // 현재 선택된 타겟 노드
        private LineNode currentTarget;

        public LineNodeField(string label = null) : base(label, null)
        {
            AddToClassList(ObjectField.ussClassName);

            // input 컨테이너 가져오기
            var inputContainer = this.Q(className: inputUssClassName);
            inputContainer.AddToClassList(className: ObjectField.inputUssClassName);
            inputContainer.AddToClassList("line-node__nodefield");

            // 선택된 노드 이름 표시 라벨
            displayName = new Label("None");
            displayName.RegisterCallback<MouseDownEvent>(OnLabelClicked);
            displayName.ClearClassList();
            displayName.AddToClassList(ObjectField.objectUssClassName);
            inputContainer.Add(displayName);

            // 노드 선택창 띄우기 버튼
            pickerButton = new Button();
            pickerButton.ClearClassList();
            pickerButton.AddToClassList(ObjectField.selectorUssClassName);
            inputContainer.Add(pickerButton);

            // 버튼 클릭 이벤트 설정
            pickerButton.clicked += OnButtonCkick;
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var graphView = VisualScriptingGraphState.instance.graphView;
            var targetNode = graphView.nodes
                                        .OfType<LineNode>()
                                        .Where(node => node.guid == value)
                                        .FirstOrDefault();

            UpdateTargetNode(targetNode);
        }

        private void OnLabelClicked(MouseDownEvent evt)
        {
            // 좌클릭이 아니거나 선택된 노드가 없는 경우 무시
            if (evt.button != (int)MouseButton.LeftMouse || currentTarget == null)
            {
                return;
            }

            var graphView = VisualScriptingGraphState.instance.graphView;

            graphView.ClearSelection(); // 선택 해제
            graphView.AddToSelection(currentTarget); // 목표 노드 선택
            graphView.FrameSelection(); // 선택된 노드로 옮기기

            // 이벤트 전파 막기
            evt.StopPropagation();
        }

        private void OnButtonCkick()
        {
            // 버튼 패널 좌표를 스크린 좌표로 변환
            var screenPoint = GUIUtility.GUIToScreenPoint(pickerButton.worldBound.center);
            screenPoint.x += 120;

            // 해당 좌표에 노드 선택창 열기
            NodeSearchWindow.Open(screenPoint, (node) => value = node?.guid);
        }

        private void UpdateTargetNode(LineNode node)
        {
            // 값이 변경된 경우에만 업데이트 호출
            if (currentTarget == node) return;

            // 이전 노드는 이벤트를 취소하고, 새 노드엔 이벤트 등록
            if (currentTarget != null) currentTarget.OnNodeModified -= UpdateDisplayLabel;
            if (node != null) node.OnNodeModified += UpdateDisplayLabel;

            // 현재 선택된 노드 변경
            currentTarget = node;

            // 라벨값 업데이트
            UpdateDisplayLabel();
        }

        public void UpdateDisplayLabel()
        {
            // 현재 값이 있음에도 선택된 노드가 없는 경우
            if (currentTarget == null && !string.IsNullOrEmpty(value))
            {
                // 그래프에서 찾아오기
                var graphView = VisualScriptingGraphState.instance.graphView;
                var targetNode = graphView.nodes
                                            .OfType<LineNode>()
                                            .Where(node => node.guid == value)
                                            .FirstOrDefault();

                // 이벤트 등록
                targetNode.OnNodeModified += UpdateDisplayLabel;

                // 현재 선택된 노드로 설정
                currentTarget = targetNode;
            }

            // 선택된 노드가 없는 경우
            if (currentTarget == null)
            {
                displayName.text = "None (Line Node)";
                return;
            }

            // 선택된 노드가 삭제된(부모 여부로 확인) 경우
            if (currentTarget.parent == null)
            {
                displayName.text = "Missing Node";
                return;
            }

            displayName.text = currentTarget.nodeName;
        }
    }
}