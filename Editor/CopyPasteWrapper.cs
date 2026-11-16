using System.Collections.Generic;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [System.Serializable]
    public class CopyPasteWrapper
    {
        [SerializeReference] public List<NodeData> nodes = new();
        public List<EdgeData> edges = new();
    }
}