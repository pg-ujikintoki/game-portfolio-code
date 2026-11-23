using TMPro;
using UnityEngine;

public class PlayerHpDisplay : MonoBehaviour
{
    [SerializeField] private PlayerLife playerLife;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private RectTransform gaugeFill;

    private void Awake()
    {
        if (playerLife == null)
        {
            playerLife = FindObjectOfType<PlayerLife>();
        }

        HideNumericText();
    }

    private void OnEnable()
    {
        RegisterCallbacks();
        HideNumericText();
        Refresh();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        if (playerLife != null)
        {
            playerLife.HpChanged += OnHpChanged;
        }
    }

    private void UnregisterCallbacks()
    {
        if (playerLife != null)
        {
            playerLife.HpChanged -= OnHpChanged;
        }
    }

    private void OnHpChanged(int currentHp, int maxHp)
    {
        ApplyGauge(currentHp, maxHp);
    }

    private void Refresh()
    {
        if (playerLife == null)
        {
            ApplyGauge(0, 1);
            return;
        }

        ApplyGauge(playerLife.CurrentHp, playerLife.MaxHp);
    }

    private void ApplyGauge(int currentHp, int maxHp)
    {
        if (gaugeFill == null)
        {
            return;
        }

        float ratio = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
        gaugeFill.anchorMin = new Vector2(0f, 0f);
        gaugeFill.anchorMax = new Vector2(1f, ratio);
        gaugeFill.offsetMin = Vector2.zero;
        gaugeFill.offsetMax = Vector2.zero;
    }

    private void HideNumericText()
    {
        if (hpText == null || hpText.gameObject == gameObject)
        {
            return;
        }

        if (hpText.gameObject.activeSelf)
        {
            hpText.gameObject.SetActive(false);
        }
    }
}
