using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePreviewer : EditorWindow
{
    private SceneAsset targetScene;
    private PreviewRenderUtility previewUtility;
    private List<GameObject> previewObjs = new List<GameObject>();
    private Bounds cameraBounds;
    private Action<Vector2> onPosUpdate;

    // 이동 위치
    private Vector2 teleportPos;

    // 상태 함수
    private float zoom = 1.0f;
    private bool isDragging;
    private Vector2 cameraOffset;
    private Vector2 dragOffset;

    public static void ShowWindow(SceneAsset previewScene, Vector2 currentPos, Action<Vector2> onPosUpdate)
    {
        var window = GetWindow<ScenePreviewer>("Scene Previewer");

        window.targetScene = previewScene;
        window.teleportPos = currentPos;
        window.onPosUpdate = onPosUpdate;
        window.RefreshPreview();
    }

    private void OnEnable()
    {
        // PreviewRenderUtility 초기화
        if (previewUtility != null) return;

        previewUtility = new PreviewRenderUtility();
    }

    private void OnDisable()
    {
        // 메모리 해제 작업
        ClearPreviewObjects();
        if (previewUtility != null)
        {
            previewUtility.Cleanup();
            previewUtility = null;
        }
    }

    private void OnGUI()
    {
        if (targetScene == null) return;

        Rect previewRect = GUILayoutUtility.GetRect(200, 500, 200, 500, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        // 이벤트 처리
        HandleInput(previewRect);

        // 카메라 영역 설정
        if (Event.current.type == EventType.Repaint)
        {
            RenderPreview(previewRect);
        }
    }

    private void RefreshPreview()
    {
        // 이전 작업 초기화
        ClearPreviewObjects();

        if (targetScene == null) return;

        var loadedScene = SceneManager.GetSceneByName(targetScene.name);
        bool inHierarchy = loadedScene.IsValid();
        bool isLoaded = loadedScene.isLoaded;

        // 씬이 로드 되어있는 지 확인
        if (!isLoaded)
        {
            // 언로드 상태라면 씬 임시로 열기
            var path = AssetDatabase.GetAssetPath(targetScene);
            loadedScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            if (!loadedScene.IsValid()) return;
        }

        // 해당 씬의 오브젝트 가져오기
        var rootObjs = loadedScene.GetRootGameObjects();
        cameraBounds = new Bounds(Vector3.zero, Vector3.zero);
        var isFindObj = false;

        foreach (var obj in rootObjs)
        {
            // 비활성화된 오브젝트인 경우 넘어가기
            if (!obj.activeInHierarchy) continue;

            // 프리뷰 월드에 복제 생성
            var clone = Instantiate(obj);

            // 프리뷰 유틸리티에 등록
            previewUtility.AddSingleGO(clone);
            previewObjs.Add(clone);

            // 카메라 영역 계산
            var renderers = clone.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                // 처음 발견한 오브젝트인 경우
                if (isFindObj)
                {
                    // 해당 오브젝트를 기준으로 첫 영역 잡기
                    cameraBounds = r.bounds;
                    isFindObj = true;
                    continue;
                }

                // 이후 발견된 오브젝트는 기존 영역에서 넓혀서 잡기
                cameraBounds.Encapsulate(r.bounds);
            }
        }

        // 기존에 로드되어 있지 않은 씬이었던 경우
        if (!isLoaded)
        {
            // 임시 씬 닫기
            // 하이어리키 창에 있던 씬어라면 언로드만 진행
            EditorSceneManager.CloseScene(loadedScene, !inHierarchy);
        }
    }

    private void RenderPreview(Rect rect)
    {
        if (previewUtility == null || previewObjs.Count == 0) return;

        // 임시 뷰어 생성
        previewUtility.BeginPreview(rect, GUIStyle.none);

        // 2D 모드로 설정
        previewUtility.camera.orthographic = true;

        // 카메라 위치 설정
        var center = cameraBounds.center - new Vector3(cameraOffset.x, cameraOffset.y, 10f);
        previewUtility.camera.transform.position = center;
        previewUtility.camera.transform.rotation = Quaternion.identity;

        // 화면 비율 설정
        var aspect = rect.width / rect.height;
        var height = cameraBounds.size.y;
        var width = cameraBounds.size.x;
        var padding = 1.1f;

        // orthographic 사이즈 설정
        var isLongWidth = (width / height > aspect);
        var size = (isLongWidth ? (width / aspect / 2.0f) : (height / 2.0f)) * padding * zoom;
        previewUtility.camera.orthographicSize = size;

        // 렌더링
        previewUtility.camera.Render();

        // 임시 뷰어 종료 및 텍스처 그리기
        var texture = previewUtility.EndPreview();
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);

        // 텔레포트 위치 마킹
        DrawMark(rect, teleportPos);
    }

    private void DrawMark(Rect rect, Vector2 pos)
    {
        var cam = previewUtility.camera;

        // 카메라에 맞춘 스크린 좌표 변경
        var screenPos = cam.WorldToScreenPoint(pos);

        // 0~1 비율로 전환
        var normalizedX = screenPos.x / cam.pixelWidth;
        var normalizedY = screenPos.y / cam.pixelHeight;

        // 그려질 좌표로 변환
        var x = rect.x + (normalizedX * rect.width);
        var y = rect.y + ((1.0f - normalizedY) * rect.height);
        var markingPos = new Vector2(x, y);

        // 해당 지점에 마킹
        Handles.BeginGUI();

        Handles.color = Color.red;
        Handles.DrawSolidDisc(markingPos, Vector3.forward, 1f / zoom);

        Handles.EndGUI();
    }

    private void ClearPreviewObjects()
    {
        foreach (var obj in previewObjs)
        {
            DestroyImmediate(obj);
        }
        previewObjs.Clear();
    }

    /// <summary>
    /// 마우스 이벤트 처리
    /// </summary>
    private void HandleInput(Rect rect)
    {
        Event e = Event.current;

        // 마우스가 작업 영역 안에 있을 때만 작동
        if (!rect.Contains(e.mousePosition)) return;

        ContentZoomHandler();
        ContentMoveHandler(rect);
        UpdatePosHandler(rect);
    }

    private void ContentZoomHandler()
    {
        Event e = Event.current;

        // 마우스 휠에만 반응
        if (e.type != EventType.ScrollWheel) return;

        zoom += e.delta.y * 0.05f;
        zoom = Mathf.Clamp(zoom, 0.1f, 5.0f); // 확대 제한

        e.Use();
        Repaint();
    }

    private void ContentMoveHandler(Rect rect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            isDragging = true;

            float ortho = previewUtility.camera.orthographicSize;
            float scale = rect.height / (ortho * 2.0f); // 작업 영역 높이 / 월드 높이
            float mouseY = (rect.height / 2 - e.mousePosition.y) / scale;
            float mouseX = (e.mousePosition.x - rect.width / 2) / scale;

            dragOffset = new Vector2(mouseX, mouseY) - cameraOffset;

            e.Use();
        }
        else if (e.type == EventType.MouseDrag && isDragging)
        {
            float ortho = previewUtility.camera.orthographicSize;
            float scale = rect.height / (ortho * 2.0f); // 작업 영역 높이 / 월드 높이
            float mouseY = (rect.height / 2 - e.mousePosition.y) / scale;
            float mouseX = (e.mousePosition.x - rect.width / 2) / scale;

            // 처음 클릭한 지점을 기점으로 이동 위치 계산
            cameraOffset = new Vector2(mouseX, mouseY) - dragOffset;

            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseUp)
        {
            isDragging = false;
        }
    }

    private void UpdatePosHandler(Rect rect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 1)
        {
            // 0~1 비율로 전환
            float normalizedX = (e.mousePosition.x - rect.x) / rect.width;
            float normalizedY = 1.0f - ((e.mousePosition.y - rect.y) / rect.height);

            teleportPos = previewUtility.camera.ViewportToWorldPoint(new Vector3(normalizedX, normalizedY));

            // 업데이트 핸들러 실행
            onPosUpdate?.Invoke(teleportPos);

            // 선택한 위치에 다시 그리기
            Repaint();
        }
    }

    public void SetPosition(Vector2 newPos)
    {
        teleportPos = newPos;

        // 바뀐 위치로 다시 그리기
        Repaint();
    }
}