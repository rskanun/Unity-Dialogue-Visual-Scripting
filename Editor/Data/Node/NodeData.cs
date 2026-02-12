using System;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [Serializable]
    public abstract class NodeData : IEquatable<NodeData>
    {
        public string guid;
        public string name;
        public Vector2 pos;
        public abstract Type NodeType { get; }

        public override int GetHashCode()
        {
            return guid.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is NodeData other && Equals(other);
        }

        public bool Equals(NodeData other)
        {
            return other != null && guid == other.guid;
        }
    }
}