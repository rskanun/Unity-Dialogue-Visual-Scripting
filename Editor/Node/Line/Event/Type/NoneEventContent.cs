using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class NoneEventContent : IEventContent
    {
        public EventNodeData ToData() { return new EventNodeData(); }
        public IDialogueEvent ToEvent() { return null; }
        public void Draw(VisualElement container) { }
        public void RestoreData(EventNodeData data) { }
    }
}