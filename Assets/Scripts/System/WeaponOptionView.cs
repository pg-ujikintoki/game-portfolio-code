using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class WeaponOptionView : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalLabelColor = Color.white;
    [SerializeField] private Color selectedLabelColor = new Color(1f, 0.95f, 0.35f, 1f);

    void Reset()
    {
        if (!toggle) toggle = GetComponent<Toggle>();
        if (!selectedFrame) selectedFrame = transform.Find("SelectedFrame")?.gameObject;
        if (!label) label = transform.Find("Label")?.GetComponent<TMP_Text>();
    }

    void Awake()
    {
        if (!toggle) toggle = GetComponent<Toggle>();
        if (!label) label = transform.Find("Label")?.GetComponent<TMP_Text>();
        toggle.onValueChanged.AddListener(OnChanged);
        OnChanged(toggle.isOn); // 初期反映
        RefreshFocusColor();
    }

    void OnDestroy()
    {
        if (toggle) toggle.onValueChanged.RemoveListener(OnChanged);
    }

    void OnChanged(bool on)
    {
        if (selectedFrame) selectedFrame.SetActive(on);
        RefreshFocusColor();
    }

    void Update()
    {
        RefreshFocusColor();
    }

    void RefreshFocusColor()
    {
        if (label == null)
        {
            return;
        }

        var current = EventSystem.current;
        bool isFocused = current != null
            && current.currentSelectedGameObject != null
            && (current.currentSelectedGameObject == gameObject
                || current.currentSelectedGameObject == toggle?.gameObject);

        label.color = isFocused ? selectedLabelColor : normalLabelColor;
    }
}
