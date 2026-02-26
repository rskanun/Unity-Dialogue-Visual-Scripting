using UnityEngine;

namespace Rskanun.DialogueVisualScripting
{
    [System.Serializable]
    public class DestroyLine : Line
    {
        [SerializeField]
        private string _target;
        public string target => _target;

        public DestroyLine(string guid, string targetGuid) : base(guid)
        {
            _target = targetGuid;
        }
    }
}