using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LastAttackedEnemyHpDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private GameObject gaugeRoot;
    [SerializeField] private RectTransform gaugeFill;
    [SerializeField] private Image frameImage;

    private int bossInstanceId;

    private void Awake()
    {
        ApplyFrameSprite();
        SetGaugeVisible(false);
        HideNumericText();
    }

    private void OnEnable()
    {
        EnemyBase.GlobalHpChanged += OnEnemyHpChanged;
        EnemyBase.BossAppeared += OnBossAppeared;
        HideNumericText();
    }

    private void OnDisable()
    {
        EnemyBase.GlobalHpChanged -= OnEnemyHpChanged;
        EnemyBase.BossAppeared -= OnBossAppeared;
    }

    private void OnBossAppeared(EnemyBase boss)
    {
        if (boss == null || !boss.IsBoss)
        {
            return;
        }

        bossInstanceId = boss.GetInstanceID();
        SetGaugeVisible(true);
        ApplyGauge(boss.CurrentHp, boss.MaxHp);
    }

    private void OnEnemyHpChanged(EnemyBase enemy, int currentHp, int maxHp, bool isPlayerAttack)
    {
        if (enemy == null || !enemy.IsBoss)
        {
            return;
        }

        if (bossInstanceId != 0 && enemy.GetInstanceID() == bossInstanceId)
        {
            ApplyGauge(currentHp, maxHp);
        }
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

    private void SetGaugeVisible(bool visible)
    {
        if (gaugeRoot == null || gaugeRoot == gameObject)
        {
            return;
        }

        if (gaugeRoot.activeSelf != visible)
        {
            gaugeRoot.SetActive(visible);
        }
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

    private void ApplyFrameSprite()
    {
        if (frameImage == null)
        {
            return;
        }

        frameImage.enabled = frameImage.sprite != null;
    }
}
