using System;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [System.Serializable]
    public class EventNodeData : NodeData
    {
        public DialogueEventType type;
        public override Type NodeType => typeof(EventNodeData);
    }
}