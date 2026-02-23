using System;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [Serializable]
    public class DestroyNodeData : NodeData
    {
        public override Type NodeType => typeof(DestroyNode);
        public string targetGuid;
    }
}