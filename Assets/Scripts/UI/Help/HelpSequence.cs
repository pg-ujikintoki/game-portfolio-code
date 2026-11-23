using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HelpEntry
{
    [Tooltip("プレイヤーへ提示するメッセージ本文")]
    [TextArea(3, 5)]
    public string message;

    [Tooltip("メッセージと併せて表示するキャラクター画像。不要な場合は未設定で構いません。")]
    public Sprite image;
}

[CreateAssetMenu(menuName = "Help/Help Sequence", fileName = "HelpSequence")]
public class HelpSequence : ScriptableObject
{
    static readonly HashSet<HelpSequence> shownOnce = new();

    [SerializeField] List<HelpEntry> entries = new();

    [Tooltip("オンにすると、一度開いたあと同じ周回では再表示しない。タイトルからニューゲームすると忘れ、もう一度出す。")]
    [SerializeField] bool showOnlyOnce;

    public IReadOnlyList<HelpEntry> Entries => entries;

    public bool ShowOnlyOnce => showOnlyOnce;

    public bool IsValid => entries != null && entries.Count > 0;

    /// <summary>
    /// showOnlyOnce がオンで、このプレイ中にすでに開いたことがある。
    /// </summary>
    public bool IsAlreadyShown => showOnlyOnce && shownOnce.Contains(this);

    public void MarkShown()
    {
        if (!showOnlyOnce)
        {
            return;
        }

        shownOnce.Add(this);
    }

    /// <summary>表示済みを消す。タイトルのニューゲームで呼ぶ。</summary>
    public static void ClearShownHistory()
    {
        shownOnce.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetShownState()
    {
        ClearShownHistory();
    }
}
