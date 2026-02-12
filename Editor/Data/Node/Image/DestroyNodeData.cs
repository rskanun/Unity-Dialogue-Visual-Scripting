using System;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [System.Serializable]
    public class DestroyNodeData : NodeData
    {
        public override Type NodeType => typeof(DestroyNode);
        public string targetGuid;
    }
}