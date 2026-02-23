using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private static NodeSearchWindow instance;

        private VisualScriptingGraphView graphView; // 현재 열린 그래프 창
        private Action<LineNode> onNodeSelectedCallback; // 노드 선택 시 실행될 콜백 함수
        private Texture2D icon;

        public static void Initialize(VisualScriptingGraphView graphView)
        {
            // 할당된 창이 없는 경루 새로운 창 생성
            if (instance == null)
                instance = CreateInstance<NodeSearchWindow>();

            // 그래프 설정
            instance.graphView = graphView;

            instance.icon = new Texture2D(1, 1);
            instance.icon.SetPixel(0, 0, Color.clear);
            instance.icon.Apply();
        }

        public static void Open(Vector2 position, Action<LineNode> onNodeSelectedCallback)
        {
            // 할당된 창이 없는 경우 경고문을 주고 돌아가기
            if (instance == null)
            {
                Debug.LogError("Failed to open window: Initialize settings must be configured first.");
                return;
            }

            // 결과값을 받기 원하는 노드 설정
            instance.onNodeSelectedCallback = onNodeSelectedCallback;

            // 선택창 열기
            SearchWindow.Open(new SearchWindowContext(position), instance);
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>()
        {
            new SearchTreeGroupEntry(new GUIContent("Select Target"))
        };

            // 타겟으로 삼는 노드가 현재 이미지 노드 하나 뿐이므로,
            // 이미지 노드만 불러오기
            var nodes = graphView.nodes.OfType<ImageNode>();
            foreach (var node in nodes)
            {
                entries.Add(new SearchTreeEntry(new GUIContent($"{node.nodeName} ({node.GetType().Name})", icon))
                {
                    level = 1,
                    userData = node
                });
            }

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            // 선택한 엔트리가 ImageNode인 경우에만 리턴
            if (entry.userData is not ImageNode node)
            {
                return false;
            }

            // 콜백 함수 실행
            onNodeSelectedCallback?.Invoke(node);

            // 콜백 함수 초기화
            onNodeSelectedCallback = null;

            return true;
        }
    }
}