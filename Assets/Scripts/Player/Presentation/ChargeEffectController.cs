using UnityEngine;

/// <summary>
/// 1 つの武器スロット（Main または Sub）に紐づくチャージ演出。
/// 表示するエフェクトはスロット単位で、装備中の武器種類では切り替わらない。
/// </summary>
public class ChargeEffectController : MonoBehaviour
{
    /// <summary>チャージ段階1。現状の「チャージ中」に対応する。</summary>
    public const int Stage1Index = 0;

    /// <summary>チャージ段階2。現状の「チャージ完了」に対応する。</summary>
    public const int Stage2Index = 1;

    [Header("Stages")]
    [Tooltip("チャージ段階ごとのエフェクト。要素 0 が段階1（チャージ中）、要素 1 が段階2（チャージ完了）。未設定の要素は子の Stage1 / Stage2 を使います。")]
    [SerializeField] private GameObject[] stageEffects = new GameObject[2];

    private void Awake()
    {
        ResolveStageEffects();
    }

    private void ResolveStageEffects()
    {
        int count = stageEffects != null ? stageEffects.Length : 0;
        int needed = Mathf.Max(count, 2);
        if (stageEffects == null || stageEffects.Length < needed)
        {
            var resized = new GameObject[needed];
            if (stageEffects != null)
            {
                for (int i = 0; i < stageEffects.Length; i++)
                {
                    resized[i] = stageEffects[i];
                }
            }

            stageEffects = resized;
        }

        for (int i = 0; i < stageEffects.Length; i++)
        {
            if (IsOwnChild(stageEffects[i]))
            {
                continue;
            }

            var child = transform.Find("Stage" + (i + 1));
            stageEffects[i] = child ? child.gameObject : null;
        }
    }

    private bool IsOwnChild(GameObject effect)
    {
        return effect && effect.transform.IsChildOf(transform);
    }

    public void ShowStage(int stageIndex)
    {
        if (stageEffects == null)
        {
            return;
        }

        for (int i = 0; i < stageEffects.Length; i++)
        {
            ApplyStageActive(stageEffects[i], i == stageIndex);
        }
    }

    public void Clear()
    {
        if (stageEffects == null)
        {
            return;
        }

        for (int i = 0; i < stageEffects.Length; i++)
        {
            ApplyStageActive(stageEffects[i], false);
        }
    }

    private static void ApplyStageActive(GameObject target, bool active)
    {
        if (!target)
        {
            return;
        }

        bool wasActive = target.activeSelf;
        if (wasActive == active)
        {
            return;
        }

        target.SetActive(active);
        if (!active)
        {
            return;
        }

        var animator = target.GetComponent<Animator>();
        if (!animator || !animator.runtimeAnimatorController)
        {
            return;
        }

        animator.Rebind();
        animator.Update(0f);
    }
}
