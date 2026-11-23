using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] ToggleGroup mainGroup;
    [SerializeField] ToggleGroup subGroup;
    [SerializeField] Selectable fallbackFocus; // どうしても見つからなかったとき用

    bool _hooked;

    void OnEnable()
    {
        // 1. まず現在のLoadoutをUIに反映
        var mainToggle = ApplySelectionToGroup(mainGroup, Loadout.Main);
        var subToggle = ApplySelectionToGroup(subGroup, Loadout.Sub);

        // 2. フォーカスは「いまのメインのトグル」に当てる
        var target = (Selectable)mainToggle ?? (Selectable)subToggle ?? fallbackFocus;
        if (target && EventSystem.current)
            EventSystem.current.SetSelectedGameObject(target.gameObject);

        // 3. リスナーは一度だけ張る
        if (!_hooked)
        {
            Hook(mainGroup, true);
            Hook(subGroup, false);
            _hooked = true;
        }
    }

    void Hook(ToggleGroup group, bool isMain)
    {
        foreach (var t in group.GetComponentsInChildren<Toggle>(true))
        {
            t.onValueChanged.AddListener(_ =>
            {
                if (!t.isOn) return;
                var now = GetSelected(group);
                if (isMain) Loadout.SetMain(now);
                else Loadout.SetSub(now);
            });
        }
    }

    // 指定された武器タイプのトグルをONにして返す
    Toggle ApplySelectionToGroup(ToggleGroup group, WeaponType wanted)
    {
        foreach (var t in group.GetComponentsInChildren<Toggle>(true))
        {
            var tag = t.GetComponent<WeaponOptionTag>();
            if (tag && tag.type == wanted)
            {
                t.SetIsOnWithoutNotify(true);
                return t;
            }
        }
        return null;
    }

    WeaponType GetSelected(ToggleGroup group)
    {
        foreach (var t in group.ActiveToggles())
        {
            var tag = t.GetComponent<WeaponOptionTag>();
            if (tag) return tag.type;

            var label = t.transform.Find("Label")?.GetComponent<TMP_Text>()?.text;
            if (label == "Gun" || label == "銃") return WeaponType.Gun;
            if (label == "Sword" || label == "剣") return WeaponType.Sword;
            if (label == "Bit" || label == "ビット") return WeaponType.Bit;
            if (label == "Punch" || label == "パンチ") return WeaponType.Punch;
        }
        return WeaponType.Gun;
    }
}
