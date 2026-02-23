using System.Collections.Generic;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [System.Serializable]
    public class GraphData
    {
        // 그래프 뷰 설정
        public Vector3 viewScale = Vector3.one;
        public Vector3 viewPosition = Vector3.zero;

        // 내용 데이터
        [SerializeReference]
        public List<NodeData> nodes = new();
        public List<EdgeData> edges = new();
    }
}