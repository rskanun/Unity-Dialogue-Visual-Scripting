using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [Serializable]
    public class PreviewerResolution
    {
        public string label;
        public Vector2 resolution;
    }

    public class ImagePreviewer : EditorWindow
    {
        private float scale;
        private Sprite sprite;
        private Vector2 currentPos;
        private Color currentColor;
        private Action<Vector2> onPosUpdateHandler;

        // 해상도 정보
        private int resolutionIndex;

        // 마우스 상태
        private bool isDragging;
        private Vector2 dragOffset;

        public static void ShowWindow(Sprite sprite, Vector2 initPos, Color initColor, Action<Vector2> handler)
        {
            // 이미지 설정용 창을 새로 생성
            var window = GetWindow<ImagePreviewer>("Image Previewer");

            // 노드로부터 데이터 받아오기
            window.sprite = sprite;
            window.currentColor = initColor;
            window.currentPos = initPos;
            window.onPosUpdateHandler = handler;

            // 창 설정 및 열기
            window.minSize = new Vector2(550, 400);
            window.Show();
        }

        private void OnGUI()
        {
            if (sprite == null)
            {
                Debug.LogError("Please assign a Sprite to the ImageNode first.");
                return;
            }

            var resolutionLabels = VisualScriptingSettings.PreviewerResolutions
                                    .Select(item => item.label)
                                    .ToArray();
            var resolutions = VisualScriptingSettings.PreviewerResolutions
                                    .Select(item => item.resolution)
                                    .ToArray();

            // 툴바 영역 설정
            var toolbarRect = new Rect(0, 0, position.width, EditorStyles.toolbar.fixedHeight);
            GUI.Box(toolbarRect, "", EditorStyles.toolbar);

            // 툴바 형태의 요소 그리기
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            resolutionIndex = EditorGUILayout.Popup(resolutionIndex, resolutionLabels, EditorStyles.toolbarPopup, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 툴바 제외 실 사용 가능한 영역
            var availableHeight = position.height - toolbarRect.height;
            var availableWidth = position.width;

            // 해상도에 따른 콘텐츠 영역 설정
            var radio = resolutions[resolutionIndex].y / resolutions[resolutionIndex].x;
            var contentWidth = (position.width * radio > availableHeight) ? availableHeight / radio : availableWidth;
            var contentHeight = contentWidth * radio;
            var contentX = (availableWidth - contentWidth) / 2;
            var contentY = (availableHeight - contentHeight) / 2 + toolbarRect.height;
            var contentRect = new Rect(contentX, contentY, contentWidth, contentHeight);
            GUI.BeginClip(contentRect);

            // 가이드 영역 설정
            var placementRect = new Rect(0, 0, contentRect.width, contentRect.height);
            EditorGUI.DrawRect(placementRect, new Color(0.3f, 0.3f, 0.3f));

            // 스프라이트 크기 및 위치 계산(화면 중앙이 pivot이 되도록)
            scale = contentWidth / resolutions[resolutionIndex].x;
            float width = sprite.rect.width * scale;
            float height = sprite.rect.height * scale;
            float x = placementRect.x + currentPos.x * scale - width / 2 + placementRect.width / 2;
            float y = placementRect.y - currentPos.y * scale - height / 2 + placementRect.height / 2;
            Rect rect = new Rect(x, y, width, height);

            // 스프라이트 색 지정
            var originColor = GUI.color;
            GUI.color = currentColor;

            // 스프라이트 그리기
            GUI.DrawTexture(rect, sprite.texture);

            // 이전 색으로 되돌리기
            GUI.color = originColor;

            // 마우스 이벤트 처리
            HandleMouseEvent(placementRect);

            // 클리핑 종료
            GUI.EndClip();
        }

        private void HandleMouseEvent(Rect bounds)
        {
            Event e = Event.current;

            // 마우스 클릭이 일어난 경우
            if (e.type == EventType.MouseDown && e.button == 0 && bounds.Contains(e.mousePosition))
            {
                isDragging = true;

                float mouseY = (bounds.height / 2 - e.mousePosition.y) / scale;
                float mouseX = (e.mousePosition.x - bounds.width / 2) / scale;

                dragOffset = new Vector2(mouseX, mouseY) - currentPos;

                // 다른 UI가 이 이벤트를 사용하지 못하도록 막기
                e.Use();
            }
            // 마우스 드래그를 하는 경우
            else if (e.type == EventType.MouseDrag && isDragging)
            {
                float mouseY = (bounds.height / 2 - e.mousePosition.y) / scale;
                float mouseX = (e.mousePosition.x - bounds.width / 2) / scale;

                // 처음 클릭한 지점을 기점으로 이동 위치 계산
                currentPos = new Vector2(mouseX, mouseY) - dragOffset;

                // 실시간으로 위치 값 업데이트
                onPosUpdateHandler?.Invoke(currentPos);

                // 창 다시 그리기
                Repaint();
                e.Use();
            }
            // 마우스 클릭에서 손을 뗀 경우
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
            }
        }

        public void SetColor(Color color)
        {
            // 현재 스프라이트의 색 업데이트
            currentColor = color;

            // 업데이트된 색에 맞춰 스프라이트 다시 그리기
            Repaint();
        }

        public void SetPosition(Vector2 pos)
        {
            // 현재 위치 업데이트
            currentPos = pos;

            // 업데이트된 위치에 맞춰 창 다시 그리기
            Repaint();
        }
    }
}