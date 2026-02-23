using UnityEngine;

namespace Rskanun.DialogueVisualScripting
{
    public class EventLine : Line
    {
        [SerializeReference]
        private IDialogueEvent _dialogueEvent;
        public IDialogueEvent dialogueEvent => _dialogueEvent;

#if UNITY_EDITOR
        public EventLine(string guid, IDialogueEvent dialogueEvent) : base(guid)
        {
            _dialogueEvent = dialogueEvent;
        }
#endif
    }
}