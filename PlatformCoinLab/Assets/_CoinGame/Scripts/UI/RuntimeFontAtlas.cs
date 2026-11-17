using TMPro;
using UnityEngine;

namespace CoinGame.UI
{
    public sealed class RuntimeFontAtlas : MonoBehaviour
    {
        [SerializeField]
        private TMP_FontAsset _source;
        [SerializeField]
        private TMP_Text[] _texts;
        private TMP_FontAsset _font;

        private void Awake()
        {
            int pointSize = Mathf.RoundToInt(_source.faceInfo.pointSize);
            _font = TMP_FontAsset.CreateFontAsset(_source.sourceFontFile, pointSize, _source.atlasPadding, _source.atlasRenderMode, _source.atlasWidth, _source.atlasHeight, AtlasPopulationMode.Dynamic, _source.isMultiAtlasTexturesEnabled);
            _font.name = _source.name + " Runtime";
            foreach (TMP_Text text in _texts)
            {
                text.font = _font;
            }
        }

        private void OnDestroy()
        {
            if (_font == null)
            {
                return;
            }

            // TMP_FontAsset releases its own atlas textures and material.
            Destroy(_font);
        }
    }
}
