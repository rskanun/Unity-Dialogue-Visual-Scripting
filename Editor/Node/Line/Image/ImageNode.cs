using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [NodeMenu("Image/Image", Order = 3)]
    public class ImageNode : LineNode, ILineProvider
    {
        private Sprite selectedSprite;
        private ObjectField spriteField;
        private ColorField colorField;
        private Vector2Field posField;

        public ImageNode() : base() { }
        public ImageNode(string guid) : base(guid) { }
        public ImageNode(NodeData data) : base(data)
        {
            // 다운케스팅이 불가능한 경우
            if (data is not ImageNodeData imageNodeData)
            {
                // 타이틀과 위치만 설정
                return;
            }

            // 이미지 스프라이트 등록
            spriteField.SetValueWithoutNotify(imageNodeData.sprite);
            selectedSprite = imageNodeData.sprite;

            // 이미지 색 등록
            colorField.SetValueWithoutNotify(imageNodeData.color);

            // 스프라이트 위치 등록
            posField.SetValueWithoutNotify(imageNodeData.spritePos);
        }

        public Line ToLine()
        {
            var data = ToData() as ImageNodeData;

            return new ImageLine(data.guid, data.sprite, data.spritePos, data.color);
        }

        public override NodeData ToData()
        {
            var data = new ImageNodeData();

            data.guid = guid;
            data.name = nodeName;
            data.pos = position;
            data.sprite = selectedSprite;
            data.color = colorField.value;
            data.spritePos = posField.value;

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

            // 스프라이트 에셋 선택
            spriteField = new ObjectField("Sprite");
            spriteField.objectType = typeof(Sprite);
            spriteField.RegisterValueChangedCallback(evt =>
            {
                selectedSprite = evt.newValue as Sprite;
                NotifyModified();
            });
            extensionContainer.Add(spriteField);

            // 색 영역
            colorField = new ColorField("Color");
            colorField.value = Color.white;
            colorField.RegisterValueChangedCallback(evt =>
            {
                OnColorFieldChanged(evt);
                NotifyModified();
            });
            extensionContainer.Add(colorField);

            // 이미지 위치
            posField = new Vector2Field("Position");
            posField.AddToClassList("line-node__image-vectorfield");
            posField.RegisterValueChangedCallback(evt =>
            {
                OnPositionFieldChanged(evt);
                NotifyModified();
            });
            extensionContainer.Add(posField);

            // 이미지 위치 설정 버튼
            var sizePreviewButton = new Button(() => OnClickPreviewButton(selectedSprite));
            sizePreviewButton.text = "Image Preview";
            extensionContainer.Add(sizePreviewButton);

            RefreshExpandedState();
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

        private void OnClickPreviewButton(Sprite previewSprite)
        {
            // 선택된 이미지가 없으면 무시
            if (previewSprite == null)
            {
                EditorGUILayout.LabelField("Sprite를 먼저 선택해주세요.");
                return;
            }

            Action<Vector2> onMovePreviewerSprite = newPos =>
            {
                posField.SetValueWithoutNotify(newPos);
                NotifyModified();
            };

            // 이미지 프리뷰어 띄우기
            ImagePreviewer.ShowWindow(previewSprite, posField.value, colorField.value, onMovePreviewerSprite);
        }
    }
}