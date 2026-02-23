using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [NodeMenu("Event", Order = 15)]
    public class EventNode : LineNode, ILineProvider
    {
        private VisualElement eventInfoContainer;
        private DropdownField eventTypeField;
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

            eventTypeField.value = eventNodeData.EventName;

            // 새 이벤트 객체 할당
            currentContent = EventContentFactory.Create(eventNodeData.EventName);

            // Register 작동하지 않으므로 직접 그려주고 데이터 삽입
            currentContent.Draw(eventInfoContainer);
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
            eventTypeField = new DropdownField("Event Type", GetEventOptions(), 0);
            eventTypeField.RegisterValueChangedCallback(evt => OnEventTypeChanged(evt.newValue));
            extensionContainer.Add(eventTypeField);

            eventInfoContainer = new VisualElement();
            extensionContainer.Add(eventInfoContainer);

            RefreshExpandedState();
        }

        private List<string> GetEventOptions()
        {
            var options = new List<string>();

            var baseType = typeof(EventNodeData);
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t == baseType || (t.IsSubclassOf(baseType) && !t.IsAbstract));

            foreach (var type in types)
            {
                // 타입에 맞는 이벤트 데이터 객체 임시 생성
                if (Activator.CreateInstance(type) is EventNodeData data)
                {
                    // 이벤트 이름을 옵션으로 등록
                    options.Add(data.EventName);
                }
            }

            return options;
        }

        private void OnEventTypeChanged(string newType)
        {
            eventInfoContainer.Clear();

            // 새 이벤트 내용으로 변경 
            currentContent = EventContentFactory.Create(newType);

            // 변경된 내용의 이벤트 정보 노드 그리기
            currentContent.Draw(eventInfoContainer);
        }
    }
}