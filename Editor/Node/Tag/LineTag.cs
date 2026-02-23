using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [NodeMenu("Line Tag", Order = 0)]
    public class LineTag : LineNode
    {
        private IntegerField idField;
        public int ID
        {
            get => idField.value;
            private set => idField.value = value; // 범위 지정을 위해 설정과 동시에 알림
        }

        public LineTag() : base() { }
        public LineTag(string guid) : base(guid) { }
        public LineTag(NodeData data) : base(data)
        {
            // 다운케스팅이 불가능한 경우
            if (data is not LineTagData tagData)
            {
                // 타이틀과 위치만 설정
                return;
            }

            ID = tagData.npcID;
        }

        public override NodeData ToData()
        {
            var data = new LineTagData();

            data.guid = guid;
            data.name = nodeName;
            data.pos = position;
            data.npcID = ID;

            return data;
        }

        public override void Draw()
        {
            base.Draw();

            // Input 연결 더미로 추가
            var dummyElement = new VisualElement();
            inputContainer.Add(dummyElement);

            // Output 연결 추가
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outputPort.portName = "Next";
            outputContainer.Add(outputPort);

            // 대사 정보
            idField = new IntegerField("ID");
            idField.value = 1;
            idField.AddToClassList("line-node__integerfield");
            idField.RegisterValueChangedCallback(evt =>
            {
                // ID 값이 7자리가 넘지 않는 자연수가 되도록 조정
                idField.value = Mathf.Clamp(evt.newValue, 1, 9999999);
            });
            extensionContainer.Add(idField);

            RefreshExpandedState();
        }
    }
}