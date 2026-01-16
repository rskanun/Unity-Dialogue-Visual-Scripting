using UnityEngine;

namespace Rskanun.DialogueVisualScripting
{
    [System.Serializable]
    public class TransformLine : Line
    {
        [SerializeField]
        private string _target;
        public string target
        {
            get => _target;

            set => _target = value;
        }

        [SerializeField]
        private Vector2 _pos;
        public Vector2 pos => _pos;

        [SerializeField]
        private Color _color;
        public Color color => _color;

#if UNITY_EDITOR
        public TransformLine(string guid, string targetGuid, Vector2 pos, Color color) : base(guid)
        {
            _target = targetGuid;
            _pos = pos;
            _color = color;
        }
#endif
    }
}