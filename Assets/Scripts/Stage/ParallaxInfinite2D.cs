// ParallaxInfinite2D.cs（堅牢化差し替え）
using UnityEngine;
using Cinemachine;


[ExecuteAlways]
public class ParallaxInfinite2D : MonoBehaviour
{
    public Camera mainCamera;

    [Header("Parallax")]
    [Range(0f, 1f)] public float parallaxX = 0.3f;
    [Range(0f, 1f)] public float parallaxY = 0.0f;

    [Header("Tile Directions")]
    public bool tileX = true;
    public bool tileY = false;

    [Tooltip("わずかな隙間防止用の余白（World単位）")]
    public float buffer = 0.05f;

    private float spriteW, spriteH;
    private int cols = 1, rows = 1;
    private Vector3 prevCamPos;
    private bool valid;
    private float cachedCameraOrthoSize = -1f;
    private float cachedCameraAspect = -1f;
    private CinemachineBrain cinemachineBrain;





    void Awake()
    {
        if (!mainCamera) mainCamera = Camera.main;
        CacheCameraComponents();
        EnsurePrototypeChild();
        //RecalcSpriteSize();
        //EnsureGridSize();   // 必要枚数ぶん子オブジェクトを用意
    }

    void OnEnable()
    {
        if (!mainCamera) mainCamera = Camera.main;
        CacheCameraComponents();

        prevCamPos = mainCamera ? mainCamera.transform.position : Vector3.zero;
        if (mainCamera)
        {
            cachedCameraOrthoSize = GetEffectiveOrthoSize();
            cachedCameraAspect = GetEffectiveAspect();
        }
        else
        {
            cachedCameraOrthoSize = -1f;
            cachedCameraAspect = -1f;
        }
    }


    void Start()

    {
        LayoutGrid();
    }

    void LateUpdate()
    {
        if (!mainCamera) mainCamera = Camera.main;
        if (!mainCamera) return;

        if (cinemachineBrain == null)
        {
            CacheCameraComponents();
        }

        if (!valid)
        {
            if (!RecalcSpriteSize()) return;
            EnsureGridSize();
        }

        if (CameraFramingChanged())
        {
            EnsureGridSize();
        }

        // 親をパララックス移動
        Vector3 camDelta = mainCamera.transform.position - prevCamPos;
        transform.position += new Vector3(camDelta.x * parallaxX, camDelta.y * parallaxY, 0f);
        prevCamPos = mainCamera.transform.position;

        // 敷き詰め直し（子リストは都度取り直す）
        LayoutGrid();
    }



    // ---------- helpers ----------
    // ---- ここから“明示初期化”だけで組み立てる ----
    public void Initialize()  // ← Manager が Sprite を入れた後に呼ぶ
    {
        if (!mainCamera) mainCamera = Camera.main;
        CacheCameraComponents();
        EnsurePrototypeChild();
        if (!RecalcSpriteSize()) return;
        EnsureGridSize();
        prevCamPos = mainCamera ? mainCamera.transform.position : Vector3.zero;
        LayoutGrid();
    }


    void EnsurePrototypeChild()
    {
        if (transform.childCount == 0)
        {
            var g = new GameObject("tile");
            g.transform.SetParent(transform, false);
            g.AddComponent<SpriteRenderer>();
        }
    }

    bool RecalcSpriteSize()
    {
        valid = false;

        var sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        if (!sr || !sr.sprite) return false; // ★エラーにしない：まだ未設定なだけ

        spriteW = sr.bounds.size.x;
        spriteH = sr.bounds.size.y;

        if (spriteW <= 0f || spriteH <= 0f)
        {
            Debug.LogError("[ParallaxInfinite2D] スプライトの幅/高さが 0 です。Sprite/PPU/Scale を確認してください。", this);
            return false;
        }

        valid = true;
        return true;
    }

    void EnsureGridSize()
    {
        if (!valid || !mainCamera) return;

        float ortho = GetEffectiveOrthoSize();
        if (ortho <= 0f) return;

        float aspect = GetEffectiveAspect();

        cachedCameraOrthoSize = ortho;
        cachedCameraAspect = aspect;

        float camW = 2f * cachedCameraOrthoSize * cachedCameraAspect;
        float camH = 2f * cachedCameraOrthoSize;


        cols = tileX ? Mathf.Max(3, Mathf.CeilToInt(camW / spriteW) + 2) : 1;

        rows = tileY ? Mathf.Max(3, Mathf.CeilToInt(camH / spriteH) + 2) : 1;

        int need = cols * rows;

        var proto = transform.GetChild(0).gameObject;

        // 不足分を複製（親は必ず transform、ローカルを維持）
        while (transform.childCount < need)
        {
#if UNITY_EDITOR
            GameObject dup = Application.isPlaying
                ? Instantiate(proto)
                : UnityEditor.PrefabUtility.InstantiatePrefab(proto) as GameObject;
#else
            GameObject dup = Instantiate(proto);
#endif

            dup.name = $"tile_{transform.childCount}";
            dup.transform.SetParent(transform, false);   // ← これでルート直下に
            dup.transform.localPosition = Vector3.zero;  // 念のため初期化
            dup.transform.localRotation = Quaternion.identity;
            dup.transform.localScale = Vector3.one;
        }

        // 余分は削除
        while (transform.childCount > need)
        {
            var t = transform.GetChild(transform.childCount - 1).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(t);
            else Destroy(t);
#else
            Destroy(t);
#endif
        }
    }

    void LayoutGrid()
    {
        if (!valid || !mainCamera) return;

        float ortho = GetEffectiveOrthoSize();
        if (ortho <= 0f) return;

        float aspect = GetEffectiveAspect();

        float camLeft = mainCamera.transform.position.x - ortho * aspect;
        float camBottom = mainCamera.transform.position.y - ortho;

        float w = spriteW;
        float h = spriteH;


        float startX = tileX ? Mathf.Floor((camLeft - buffer) / w - 1f) * w + w * 0.5f
                             : transform.position.x;
        float startY = tileY ? Mathf.Floor((camBottom - buffer) / h - 1f) * h + h * 0.5f
                             : transform.position.y;

        int k = 0;
        for (int iy = 0; iy < rows; iy++)
        {
            for (int ix = 0; ix < cols; ix++)
            {
                if (k >= transform.childCount) return;
                var t = transform.GetChild(k++);
                if (!t) continue; // 念のため
                t.position = new Vector3(startX + ix * w, startY + iy * h, transform.position.z);
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // エディタで値を変えたときは遅延で再構築（OnValidate中にInstantiateしない）
        if (!isActiveAndEnabled) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || !gameObject) return;
            if (!gameObject.activeInHierarchy) return;

            EnsurePrototypeChild();
            RecalcSpriteSize();
            EnsureGridSize();
            LayoutGrid();
        };
    }
#endif

    // 外部から差し替えたとき呼ぶ
    public void RebuildNow()
    {
        EnsurePrototypeChild();
        RecalcSpriteSize();
        EnsureGridSize();
        LayoutGrid();
    }

    bool CameraFramingChanged()
    {
        if (!mainCamera) return false;

        float currentOrtho = GetEffectiveOrthoSize();
        float currentAspect = GetEffectiveAspect();

        if (!Mathf.Approximately(currentOrtho, cachedCameraOrthoSize) ||
            !Mathf.Approximately(currentAspect, cachedCameraAspect))
        {
            cachedCameraOrthoSize = currentOrtho;
            cachedCameraAspect = currentAspect;
            return true;
        }

        return false;
    }

    void CacheCameraComponents()
    {
        if (mainCamera)
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
        }
        else
        {
            cinemachineBrain = null;
        }
    }

    float GetEffectiveOrthoSize()
    {
        if (!mainCamera) return 0f;

        if (cinemachineBrain == null)
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
        }

        float ortho = mainCamera.orthographicSize;

        if (cinemachineBrain != null)
        {
            var vcam = cinemachineBrain.ActiveVirtualCamera;
            if (vcam != null)
            {
                var lens = vcam.State.Lens;
                if (lens.Orthographic && lens.OrthographicSize > 0f)
                {
                    ortho = lens.OrthographicSize;
                }
            }
        }

        return ortho;
    }

    float GetEffectiveAspect()
    {
        if (!mainCamera) return 1f;

        return mainCamera.aspect;
    }
}


