// Assets/Scripts/UI/UINavRow.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("UI/UINavRow")]
public class UINavRow : MonoBehaviour
{
    [SerializeField] private List<Selectable> items = new();  // この行のトグル群
    [SerializeField] private UINavRow upRow;                  // 上の行
    [SerializeField] private UINavRow downRow;                // 下の行

    void Reset()
    {
        CollectItems();
        AutoLinkRowsIfNull();
    }

    void OnEnable()
    {
        if (items == null || items.Count == 0) CollectItems();
        AutoLinkRowsIfNull();
        Wire();
    }

    void OnValidate()  // エディタで並べ替えた時も反映
    {
        CollectItems();
    }

    void CollectItems()
    {
        // 直下の子にある Selectable を左→右の順で収集
        items = transform.Cast<Transform>()
            .Select(t => t.GetComponent<Selectable>())
            .Where(s => s != null).ToList();
    }

    void AutoLinkRowsIfNull()
    {
        if (transform.parent == null) return;
        var rows = transform.parent.GetComponentsInChildren<UINavRow>(true)
                     .OrderBy(r => r.transform.GetSiblingIndex()).ToList();
        int me = rows.IndexOf(this);
        if (!upRow   && me > 0)                 upRow   = rows[me - 1];
        if (!downRow && me >= 0 && me < rows.Count - 1) downRow = rows[me + 1];
    }

    [ContextMenu("Rewire Navigation")]
    public void Wire()
    {
        if (items == null || items.Count == 0) return;

        for (int i = 0; i < items.Count; i++)
        {
            var s = items[i];
            if (!s) continue;

            var nav = new Navigation { mode = Navigation.Mode.Explicit };

            // 左右（循環）
            nav.selectOnLeft  = items[(i - 1 + items.Count) % items.Count];
            nav.selectOnRight = items[(i + 1) % items.Count];

            // 上下（同じ列番号があれば接続）
            if (upRow   && i < upRow.items.Count)   nav.selectOnUp   = upRow.items[i];
            if (downRow && i < downRow.items.Count) nav.selectOnDown = downRow.items[i];

            s.navigation = nav;
        }
#if UNITY_EDITOR
        Debug.Log($"[UINavRow] Wired {name}: {items.Count} items"
            + (upRow ? " ↑OK" : " ↑-") + (downRow ? " ↓OK" : " ↓-"));
#endif
    }
}
