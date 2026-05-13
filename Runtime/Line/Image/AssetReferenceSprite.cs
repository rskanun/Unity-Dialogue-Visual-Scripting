using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Rskanun.DialogueVisualScripting
{
    [System.Serializable]
    public class AssetReferenceSprite : AssetReferenceT<Sprite>
    {
        public AssetReferenceSprite(string guid) : base(guid) { }
    }
}