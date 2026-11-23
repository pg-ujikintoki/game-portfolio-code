// BackgroundManager.cs
using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    public Camera mainCamera;
    public ParallaxInfinite2D layerPrefab;     // 子に SpriteRenderer を1枚持つPrefab
    public List<BgLayerConfig> layers = new(); // 並び順＝描画順の想定でOK

    void Awake()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
        #if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(transform.GetChild(i).gameObject);
            else Destroy(transform.GetChild(i).gameObject);
        #else
            Destroy(transform.GetChild(i).gameObject);
        #endif
        }

        if (!mainCamera) mainCamera = Camera.main;
        if (!layerPrefab) { Debug.LogError("layerPrefab を割り当ててください"); return; }

        foreach (var cfg in layers)
        {
            if (cfg == null || !cfg.active) continue;

            /*var layer = Instantiate(layerPrefab, transform);
            layer.name        = cfg.layerName;
            layer.mainCamera  = mainCamera;
            layer.parallaxX   = cfg.parallaxX;
            layer.parallaxY   = cfg.parallaxY;
            layer.tileX       = cfg.tileX;
            layer.tileY       = cfg.tileY;*/

            // 生成ループの核心部だけ
            var layer = Instantiate(layerPrefab, transform);
            var comp  = layer.GetComponent<ParallaxInfinite2D>();

            // まず各種パラメータをセット
            comp.mainCamera = mainCamera;
            comp.parallaxX  = cfg.parallaxX;
            comp.parallaxY  = cfg.parallaxY;
            comp.tileX      = cfg.tileX;
            comp.tileY      = cfg.tileY;


            // 見本SpriteRenderer（Prefabの子0想定）
            var sr = layer.transform.GetChild(0).GetComponent<SpriteRenderer>();
            sr.sprite            = cfg.sprite;
            sr.color             = cfg.tint;
            sr.sortingLayerName  = cfg.sortingLayer;
            sr.sortingOrder      = cfg.orderInLayer;

            // 位置など
            layer.name = string.IsNullOrWhiteSpace(cfg.layerName) ? cfg.name : cfg.layerName;
            layer.transform.position = new Vector3(cfg.startOffset.x, cfg.startOffset.y, cfg.z);

            // ★最後に明示初期化（ここでグリッド生成・配置）
            comp.Initialize();

            // スプライト差し替え後にタイル枚数/配置を再計算
            layer.RebuildNow();
        }
    }
}
