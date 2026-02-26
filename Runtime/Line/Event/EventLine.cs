using UnityEngine;

namespace Rskanun.DialogueVisualScripting
{
    public class EventLine : Line
    {
        [SerializeReference]
        private IDialogueEvent _dialogueEvent;
        public IDialogueEvent dialogueEvent => _dialogueEvent;

        public EventLine(string guid, IDialogueEvent dialogueEvent) : base(guid)
        {
            _dialogueEvent = dialogueEvent;
        }
    }
}