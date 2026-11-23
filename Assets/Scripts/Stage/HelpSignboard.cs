using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HelpSequence と 1 対 1 で紐づくステージ上の看板。
/// 対応する HelpTrigger の範囲に入っているあいだ、Animator の Readable をオンにする。
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class HelpSignboard : MonoBehaviour
{
    public const string ReadableParameter = "Readable";

    [Tooltip("この看板が連動する HelpSequence。未設定なら状態は変えない。")]
    [SerializeField] HelpSequence sequence;

    [Tooltip("Readable パラメータで Idle と Readable を切り替える。")]
    [SerializeField] Animator animator;

    static readonly Dictionary<HelpSequence, List<HelpSignboard>> signboardsBySequence = new();
    static readonly Dictionary<HelpSequence, HashSet<HelpTrigger>> insideTriggersBySequence = new();

    bool registered;

    void Reset()
    {
        animator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (!animator)
        {
            animator = GetComponent<Animator>();
        }
    }

    void OnEnable()
    {
        Register();
        if (sequence)
        {
            Refresh(sequence);
        }
        else
        {
            SetReadable(false);
        }
    }

    void OnDisable()
    {
        Unregister();
    }

    /// <summary>
    /// トリガーの範囲にプレイヤーがいるかを知らせる。同じ HelpSequence のトリガーが残っているあいだは読める状態を維持する。
    /// </summary>
    public static void SetTriggerInside(HelpTrigger trigger, HelpSequence sequence, bool inside)
    {
        if (!trigger || !sequence)
        {
            return;
        }

        if (!insideTriggersBySequence.TryGetValue(sequence, out HashSet<HelpTrigger> triggers))
        {
            if (!inside)
            {
                return;
            }

            triggers = new HashSet<HelpTrigger>();
            insideTriggersBySequence[sequence] = triggers;
        }

        triggers.RemoveWhere(candidate => !candidate);

        bool changed = inside ? triggers.Add(trigger) : triggers.Remove(trigger);
        if (triggers.Count == 0)
        {
            insideTriggersBySequence.Remove(sequence);
        }

        if (!changed)
        {
            return;
        }

        Refresh(sequence);
    }

    void Register()
    {
        if (!sequence)
        {
            return;
        }

        if (!signboardsBySequence.TryGetValue(sequence, out List<HelpSignboard> signboards))
        {
            signboards = new List<HelpSignboard>();
            signboardsBySequence[sequence] = signboards;
        }

        signboards.RemoveAll(board => !board);
        if (signboards.Contains(this))
        {
            registered = true;
            return;
        }

        if (signboards.Count > 0)
        {
            Debug.LogWarning($"[HelpSignboard] HelpSequence '{sequence.name}' は既に '{signboards[0].name}' に紐づいています。先に有効な看板だけを動かします: {name}");
        }

        signboards.Add(this);
        registered = true;
    }

    void Unregister()
    {
        if (!registered)
        {
            return;
        }

        registered = false;
        SetReadable(false);

        if (!sequence || !signboardsBySequence.TryGetValue(sequence, out List<HelpSignboard> signboards))
        {
            return;
        }

        signboards.Remove(this);
        if (signboards.Count == 0)
        {
            signboardsBySequence.Remove(sequence);
            return;
        }

        Refresh(sequence);
    }

    static void Refresh(HelpSequence sequence)
    {
        if (!sequence || !signboardsBySequence.TryGetValue(sequence, out List<HelpSignboard> signboards))
        {
            return;
        }

        signboards.RemoveAll(board => !board);
        if (signboards.Count == 0)
        {
            signboardsBySequence.Remove(sequence);
            return;
        }

        bool occupied = insideTriggersBySequence.TryGetValue(sequence, out HashSet<HelpTrigger> triggers) && triggers.Count > 0;
        HelpSignboard driver = null;
        for (int i = 0; i < signboards.Count; i++)
        {
            HelpSignboard board = signboards[i];
            if (board.isActiveAndEnabled)
            {
                driver = board;
                break;
            }
        }

        for (int i = 0; i < signboards.Count; i++)
        {
            HelpSignboard board = signboards[i];
            if (!board.isActiveAndEnabled)
            {
                continue;
            }

            board.SetReadable(occupied && board == driver);
        }
    }

    void SetReadable(bool readable)
    {
        if (!animator || !animator.isActiveAndEnabled)
        {
            return;
        }

        animator.SetBool(ReadableParameter, readable);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry()
    {
        signboardsBySequence.Clear();
        insideTriggersBySequence.Clear();
    }
}
