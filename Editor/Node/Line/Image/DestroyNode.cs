using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [NodeMenu("Image/Destroy", Order = 14)]
    public class DestroyNode : LineNode, ILineProvider
    {
        private LineNodeField targetField;

        public DestroyNode() : base() { }
        public DestroyNode(string guid) : base(guid) { }
        public DestroyNode(NodeData data) : base(data)
        {
            // 다운케스팅이 불가능한 경우
            if (data is not DestroyNodeData destroyNodeData)
            {
                // 타이틀과 위치만 설정
                return;
            }

            targetField.value = destroyNodeData.targetGuid;
        }

        public Line ToLine()
        {
            var data = ToData() as DestroyNodeData;

            return new DestroyLine(data.guid, data.targetGuid);
        }

        public override NodeData ToData()
        {
            var data = new DestroyNodeData();

            data.guid = guid;
            data.name = nodeName;
            data.pos = position;
            data.targetGuid = targetField.value;

            return data;
        }

        public override void OnLoadCompleted()
        {
            // 그래프 로드가 끝난 시점에서 타겟 설정 필드 다시 불러오기
            // (타겟보다 해당 노드가 먼저 로드된 시점에서 못 찾는 오류가 발생할 수 있음)
            targetField.UpdateDisplayLabel();
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

            // 파괴할 오브젝트 선택
            targetField = new LineNodeField("Destroy Object");
            extensionContainer.Add(targetField);

            RefreshExpandedState();
        }
    }
}