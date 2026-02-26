using UnityEngine;

namespace Rskanun.DialogueVisualScripting
{
    [System.Serializable]
    public class ImageLine : Line
    {
        [SerializeField]
        private Sprite _sprite;
        public Sprite sprite => _sprite;

        [SerializeField]
        private Vector2 _pos;
        public Vector2 pos => _pos;

        [SerializeField]
        private Color _color;
        public Color color => _color;

        public ImageLine(string guid, Sprite sprite, Vector2 pos, Color color) : base(guid)
        {
            _sprite = sprite;
            _pos = pos;
            _color = color;
        }
    }
}