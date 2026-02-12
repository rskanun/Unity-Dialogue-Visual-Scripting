using System;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [System.Serializable]
    public class TransformNodeData : NodeData
    {
        public override Type NodeType => typeof(TransformNode);
        public string targetGuid;
        public Color transColor;
        public Vector2 transPos;
    }
}