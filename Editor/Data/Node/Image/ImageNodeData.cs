using System;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [Serializable]
    public class ImageNodeData : NodeData
    {
        public override Type NodeType => typeof(ImageNode);
        public Sprite sprite;
        public Color color;
        public Vector2 spritePos;
    }
}