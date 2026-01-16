using UnityEngine;

namespace Rskanun.DialogueVisualScripting
{
    [System.Serializable]
    public class TextLine : Line
    {
        [SerializeField]
        private string _name;
        public string name => _name;

        [SerializeField]
        private string _dialogue;
        public string dialogue => _dialogue;

#if UNITY_EDITOR
        public TextLine(string guid, string name, string text) : base(guid)
        {
            _name = name;
            _dialogue = text;
        }
    }
#endif
}