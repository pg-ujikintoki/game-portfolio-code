// BgLayerConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "BgLayer", menuName = "Background/BgLayerConfig")]
public class BgLayerConfig : ScriptableObject
{
    public string layerName = "L1";
    public Sprite sprite;
    public Color tint = Color.white;

    [Range(0f,1f)] public float parallaxX = 0.3f;
    [Range(0f,1f)] public float parallaxY = 0f;

    public bool tileX = true;   // 横に敷き詰め
    public bool tileY = false;  // 縦にも敷き詰め（雲など）

    public string sortingLayer = "Background";
    public int    orderInLayer = 0;

    public Vector2 startOffset; // 初期位置(ワールド座標)
    public float   z = 0f;

    public bool active = true;  // 使う/使わない
}
