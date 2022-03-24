using UnityEngine;

[System.Serializable]
public class ShadowSetting
{
    [Min(0f)]
    public float m_MaxShadowDistance = 100f;
    public enum ShadowType
    {
        _NO_SHADOW,
        _HARD_SHADOW,
        _SOFT_SHADOW
    }
    public ShadowType m_ShadowType;

    public enum TextureSizeType
    {
        _TEXTURESIZE_256 = 256,
        _TEXTURESIZE_512 = 512,
        _TEXTURESIZE_1024 = 1024,
        _TEXTURESIZE_2048 = 2048,
        _TEXTURESIZE_4096 = 4096
    }
    public TextureSizeType m_TextureSize = TextureSizeType._TEXTURESIZE_2048;
}
