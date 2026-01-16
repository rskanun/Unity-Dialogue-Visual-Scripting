using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public enum DialogueEventType
    {
        None,
        Teleport,   // 플레이어의 씬과 위치 이동
    }

    public class EventNode : LineNode, ILineProvider
    {
        private VisualElement eventInfoContainer;
        private EnumField eventTypeField;
        private IEventContent currentContent;

        public EventNode() : base() { }
        public EventNode(string guid) : base(guid) { }
        public EventNode(NodeData data) : base(data)
        {
            // 다운케스팅이 불가능한 경우
            if (data is not EventNodeData eventNodeData)
            {
                // 타이틀과 위치만 설정
                return;
            }

            eventTypeField.value = eventNodeData.type;

            // 새 이벤트 객체 할당
            currentContent = EventContentFactory.Create(eventNodeData.type);

            // Register 작동하지 않으므로 직접 그려주고 데이터 삽입
            currentContent.Draw(eventInfoContainer, NotifyModified);
            currentContent.RestoreData(eventNodeData);
        }

        public Line ToLine()
        {
            var data = (EventNodeData)ToData();
            var runtimeEvent = currentContent.ToEvent();

            return new EventLine(data.guid, runtimeEvent);
        }

        public override NodeData ToData()
        {
            // 이벤트 타입 별 데이터 객체 생성
            var data = currentContent.ToData();

            // 공통 데이터 담기 
            data.guid = guid;
            data.name = nodeName;
            data.pos = position;
            data.type = (DialogueEventType)eventTypeField.value;

            return data;
        }

        public override void Draw()
        {
            base.Draw();

            // Input 연결 추가
            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "Prev";
            inputContainer.Add(inputPort);

            // Output 연결 추가
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outputPort.portName = "Next";
            outputContainer.Add(outputPort);

            // 이벤트 타입
            eventTypeField = new EnumField("Event Type", DialogueEventType.None);
            eventTypeField.RegisterValueChangedCallback(evt =>
            {
                OnEventTypeChanged((DialogueEventType)evt.newValue);
                NotifyModified();
            });
            extensionContainer.Add(eventTypeField);

            eventInfoContainer = new VisualElement();
            extensionContainer.Add(eventInfoContainer);

            RefreshExpandedState();
        }

        private void OnEventTypeChanged(DialogueEventType newType)
        {
            eventInfoContainer.Clear();

            // 새 이벤트 내용으로 변경 
            currentContent = EventContentFactory.Create(newType);

            // 변경된 내용의 이벤트 정보 노드 그리기
            currentContent.Draw(eventInfoContainer, NotifyModified);
        }
    }
}