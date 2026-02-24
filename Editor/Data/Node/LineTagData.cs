using System;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [Serializable]
    public class LineTagData : NodeData
    {
        public override Type NodeType => typeof(LineTag);
        public int dialogueID;
    }
}