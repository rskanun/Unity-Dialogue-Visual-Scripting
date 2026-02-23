using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public interface IEventContent
    {
        public EventNodeData ToData();
        public IDialogueEvent ToEvent();
        public void Draw(VisualElement container);
        public void RestoreData(EventNodeData data);
    }
}