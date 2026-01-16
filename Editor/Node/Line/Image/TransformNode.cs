using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class TransformNode : LineNode, ILineProvider
{
    private LineNodeField targetField;
    private ColorField colorField;
    private Vector2Field posField;

    private ImageNode targetNode;

    public TransformNode() : base() { }
    public TransformNode(string guid) : base(guid) { }
    public TransformNode(NodeData data) : base(data)
    {
        // 다운케스팅이 불가능한 경우
        if (data is not TransformNodeData transformNodeData)
        {
            // 타이틀과 위치만 설정
            return;
        }

        // 대상 guid 등록
        targetField.value = transformNodeData.targetGuid;

        // 변형될 색과 위치값 등록
        colorField.SetValueWithoutNotify(transformNodeData.transColor);
        posField.SetValueWithoutNotify(transformNodeData.transPos);
    }

    public Line ToLine()
    {
        return new TransformLine((TransformNodeData)ToData());
    }

    public override NodeData ToData()
    {
        var data = new TransformNodeData();

        data.guid = guid;
        data.name = nodeName;
        data.pos = position;
        data.targetGuid = targetField.value;
        data.transColor = colorField.value;
        data.transPos = posField.value;

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

        // 변경할 오브젝트 선택
        targetField = new LineNodeField("Transform Object");
        targetField.RegisterValueChangedCallback(evt =>
        {
            OnTargetChanged(evt);
            NotifyModified();
        });
        extensionContainer.Add(targetField);

        // 변경될 색
        colorField = new ColorField("Transform Color");
        colorField.value = Color.white;
        colorField.RegisterValueChangedCallback(evt =>
        {
            OnColorFieldChanged(evt);
            NotifyModified();
        });
        extensionContainer.Add(colorField);

        // 이동될 위치
        posField = new Vector2Field("Transform Position");
        posField.AddToClassList("line-node__image-vectorfield");
        posField.RegisterValueChangedCallback(evt =>
        {
            OnPositionFieldChanged(evt);
            NotifyModified();
        });
        extensionContainer.Add(posField);

        // 이미지 위치 설정 버튼
        var sizePreviewButton = new Button(OnClickPreviewButton);
        sizePreviewButton.text = "Image Preview";
        extensionContainer.Add(sizePreviewButton);

        RefreshExpandedState();
    }

    private void OnTargetChanged(ChangeEvent<string> evt)
    {
        // 타겟 노드 찾아오기
        UpdateTargetNode();
    }

    private void UpdateTargetNode()
    {
        // 그래프 뷰에서 guid로 바뀐 타겟 찾아오기
        var graphView = VisualScriptingGraphState.Instance.graphView;

        // 대상 노드 설정
        targetNode = graphView.nodes
                            .OfType<ImageNode>()
                            .Where(node => node.guid == targetField.value)
                            .FirstOrDefault();
    }

    private void OnColorFieldChanged(ChangeEvent<Color> evt)
    {
        // 현재 열려있는 프리뷰어 창 가져오기
        var window = Resources.FindObjectsOfTypeAll<ImagePreviewer>().FirstOrDefault();

        // 없는 경우 무시
        if (window == null) return;

        // 해당 창에 띄워진 스프라이트 위치 업데이트
        window.SetColor(evt.newValue);

    }

    private void OnPositionFieldChanged(ChangeEvent<Vector2> evt)
    {
        // 현재 열려있는 프리뷰어 창 가져오기
        var window = Resources.FindObjectsOfTypeAll<ImagePreviewer>().FirstOrDefault();

        // 없는 경우 무시
        if (window == null) return;

        // 해당 창에 띄워진 스프라이트 위치 업데이트
        window.SetPosition(evt.newValue);
    }

    private void OnClickPreviewButton()
    {
        // 선택한 타겟이 없는 경우 다시 불러오기
        if (targetNode == null)
        {
            UpdateTargetNode();
        }

        // 불러올 타겟이 없는 경우엔 무시 
        if (targetNode == null)
        {
            return;
        }

        // 스프라이트 값 가져오기
        var data = targetNode.ToData() as ImageNodeData;
        var sprite = data?.sprite;

        Action<Vector2> onMovePreviewerSprite = newPos =>
        {
            posField.SetValueWithoutNotify(newPos);
            NotifyModified();
        };

        // 이미지 프리뷰어 띄우기
        ImagePreviewer.ShowWindow(sprite, posField.value, colorField.value, onMovePreviewerSprite);
    }
}