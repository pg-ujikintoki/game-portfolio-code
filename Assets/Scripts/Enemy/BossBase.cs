using System.Collections;
using UnityEngine;

public abstract class BossBase : EnemyBase
{
    [Header("Boss Flags")]
    [SerializeField] private bool notifyStageAsBoss = true;

    [Header("Boss SFX")]
    [Tooltip("撃破時。未設定なら EnemyBase の撃破 SE を使う。被ダメージ SE は鳴らさない。")]
    [SerializeField] private AudioClip bossDefeatSfx;
    [SerializeField, Range(0f, 1f)] private float bossDefeatSfxVolume = 0.9f;

    [Header("Boss Damage Rules")]
    [SerializeField] private float invincibilityDuration = 1.0f;
    [SerializeField] private float invincibilityBlinkInterval = 0.1f;
    [SerializeField] private Transform spriteHolder;

    private float lastDamageTime = float.NegativeInfinity;
    private int lastDamageComboPoint = int.MinValue;
    private SpriteRenderer[] spriteRenderers;
    private Coroutine invincibilityBlinkCoroutine;

    protected override void Awake()
    {
        ApplyBossFlagSetting();
        base.Awake();
        ApplyBossFlagSetting();
    }

    protected override void CommonStart()
    {
        base.CommonStart();
        CacheSpriteRenderers();
        StopInvincibilityBlink();
        OnBossStart();
    }

    protected sealed override void CommonUpdate()
    {
        UpdateInvincibilityBlinkState();
        OnBossUpdate();
    }

    protected sealed override void CommonFixedUpdate()
    {
        OnBossFixedUpdate();
    }

    protected override void OnResetState()
    {
        base.OnResetState();
        lastDamageTime = float.NegativeInfinity;
        lastDamageComboPoint = int.MinValue;
        StopInvincibilityBlink();
        OnBossResetState();
    }

    /// <summary>ボス撃破 SE。未設定のときだけ通常敵の撃破 SE に戻す。</summary>
    protected override void PlayDefeatSfx()
    {
        if (bossDefeatSfx == null)
        {
            base.PlayDefeatSfx();
            return;
        }

        SfxOneShot.Play(bossDefeatSfx, bossDefeatSfxVolume);
    }

    protected virtual void OnBossStart()
    {
    }

    protected virtual void OnBossResetState()
    {
    }

    protected virtual void OnBossUpdate()
    {
    }

    protected virtual void OnBossFixedUpdate()
    {
    }

    private void OnDisable()
    {
        StopInvincibilityBlink();
    }

    protected override bool CanReceiveDamage(DamagePayload payload)
    {
        if (!base.CanReceiveDamage(payload))
        {
            return false;
        }

        bool isInvincibleWindow = IsCurrentlyInvincible();
        if (isInvincibleWindow && payload.ComboPoint <= lastDamageComboPoint)
        {
            return false;
        }

        return true;
    }

    protected override void OnDamageApplied(DamagePayload payload)
    {
        base.OnDamageApplied(payload);
        lastDamageTime = Time.time;
        lastDamageComboPoint = payload.ComboPoint;
        StartInvincibilityBlink();
    }

    private void ApplyBossFlagSetting()
    {
        SetBossFlag(notifyStageAsBoss);
    }

    private bool IsCurrentlyInvincible()
    {
        return invincibilityDuration > 0f && Time.time - lastDamageTime < invincibilityDuration;
    }

    private void UpdateInvincibilityBlinkState()
    {
        if (!IsCurrentlyInvincible() && invincibilityBlinkCoroutine != null)
        {
            StopInvincibilityBlink();
        }
    }

    private void StartInvincibilityBlink()
    {
        if (invincibilityBlinkCoroutine != null || invincibilityDuration <= 0f)
        {
            return;
        }

        EnsureSpriteRenderers();
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return;
        }

        invincibilityBlinkCoroutine = StartCoroutine(InvincibilityBlinkCoroutine());
    }

    private void StopInvincibilityBlink()
    {
        if (invincibilityBlinkCoroutine != null)
        {
            StopCoroutine(invincibilityBlinkCoroutine);
            invincibilityBlinkCoroutine = null;
        }

        SetRenderersVisible(true);
    }

    private void CacheSpriteRenderers()
    {
        if (spriteHolder != null)
        {
            spriteRenderers = spriteHolder.GetComponentsInChildren<SpriteRenderer>(true);
        }
        else
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }

    private void EnsureSpriteRenderers()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            CacheSpriteRenderers();
        }
    }

    private void SetRenderersVisible(bool isVisible)
    {
        EnsureSpriteRenderers();
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            spriteRenderers[i].enabled = isVisible;
        }
    }

    private IEnumerator InvincibilityBlinkCoroutine()
    {
        float interval = Mathf.Max(0.02f, invincibilityBlinkInterval);
        WaitForSeconds wait = new WaitForSeconds(interval);
        bool isVisible = false;

        while (IsCurrentlyInvincible())
        {
            isVisible = !isVisible;
            SetRenderersVisible(isVisible);
            yield return wait;
        }

        SetRenderersVisible(true);
        invincibilityBlinkCoroutine = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyBossFlagSetting();
    }
#endif
}
