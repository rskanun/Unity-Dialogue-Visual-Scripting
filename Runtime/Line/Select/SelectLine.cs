using UnityEngine;
using System.Collections.Generic;

namespace Rskanun.DialogueVisualScripting
{
    [System.Serializable]
    public class SelectLine : Line
    {
        [SerializeField]
        private List<string> _options = new();
        public List<string> options => _options;

#if UNITY_EDITOR
        public SelectLine(string guid, List<string> options) : base(guid)
        {
            _options = options;
        }
    }
#endif
}