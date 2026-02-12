using System;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [Serializable]
    public class EventNodeData : NodeData
    {
        public virtual string EventName => "None";
        public virtual Type ContentType => typeof(NoneEventContent);

        public override Type NodeType => typeof(EventNode);
    }
}