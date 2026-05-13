using UnityEngine;

namespace Rskanun.DialogueVisualScripting
{
    [System.Serializable]
    public class ImageLine : Line
    {
        [SerializeField]
        private AssetReferenceSprite _spriteRef;
        public AssetReferenceSprite spriteRef => _spriteRef;

        [SerializeField]
        private Vector2 _pos;
        public Vector2 pos => _pos;

        [SerializeField]
        private Color _color;
        public Color color => _color;

        public ImageLine(string guid, AssetReferenceSprite spriteRef, Vector2 pos, Color color) : base(guid)
        {
            _spriteRef = spriteRef;
            _pos = pos;
            _color = color;
        }
    }
}