using System.Collections;
using UnityEngine;

public class GunChargeAttack : MonoBehaviour, IChargeableWeapon
{
    [Header("References")]
    [Tooltip("チャージショットを管理する PlayerAttackController への参照です。")]
    [SerializeField] private PlayerAttackController attackController;
    [Tooltip("チャージ弾の発射位置。未指定の場合は PlayerAttackController の弾発射位置を使用します。")]
    [SerializeField] private Transform projectileSpawnPoint;
    [Tooltip("チャージ/チャージガトリングの能力解放判定を行うプロバイダー。未設定の場合は同一 GameObject 上から自動取得します。")]
    [SerializeField] private MonoBehaviour abilityProviderBehaviour;

    [Header("Charge Settings")]
    [Tooltip("チャージ完了に必要な時間（秒）。")]
    [SerializeField, Min(0f)] private float chargeDuration = 2f;
    [Tooltip("チャージショットで使用する Projectile Prefab。PlayerProjectileBase 派生コンポーネントを含める必要があります。")]
    [SerializeField] private GameObject chargeProjectilePrefab;

    [Header("Charge Gatling (W103)")]
    [Tooltip("チャージガトリングで発射する弾数。")]
    [SerializeField, Min(1)] private int gatlingBulletCount = 16;
    [Tooltip("チャージガトリングの発射間隔（秒）。")]
    [SerializeField, Min(0f)] private float gatlingFireInterval = 0.05f;
    [Tooltip("チャージガトリングで使用する弾 Prefab。未設定の場合は通常弾（PlayerAttackController 側）を使用します。")]
    [SerializeField] private GameObject gatlingBulletPrefab;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("チャージショット発射時に再生する効果音。")]
    [SerializeField] private AudioClip chargeShotSfx;
    [Tooltip("チャージショット効果音のボリューム係数。")]
    [SerializeField, Range(0f, 1f)] private float chargeShotVolume = 1f;

    public WeaponType WeaponType => WeaponType.Gun;
    public float ChargeDuration => Mathf.Max(0f, chargeDuration);

    private bool hasLoggedInvalidProjectilePrefab;
    private IPlayerAbilityProvider abilityProvider;
    private readonly Coroutine[] gatlingRoutines = new Coroutine[2];

    private void Awake()
    {
        if (!attackController)
        {
            attackController = GetComponent<PlayerAttackController>();
        }

        if (!projectileSpawnPoint && attackController)
        {
            projectileSpawnPoint = attackController.BulletSpawnPoint;
        }

        if (!sfxSource)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        abilityProvider = ResolveAbilityProvider();
    }

    private void OnDisable()
    {
        StopAllGatlingRoutines();
    }

    private IPlayerAbilityProvider ResolveAbilityProvider()
    {
        if (abilityProviderBehaviour is IPlayerAbilityProvider configured)
        {
            return configured;
        }

        if (TryGetComponent<IPlayerAbilityProvider>(out var provider))
        {
            return provider;
        }

        return null;
    }

    public bool CanStartCharge(WeaponSlot slot)
    {
        if (!attackController || !attackController.isActiveAndEnabled)
        {
            return false;
        }

        if (IsGatlingActive())
        {
            return HasValidGatlingBulletPrefab();
        }

        return HasValidProjectilePrefab();
    }

    public void OnChargeStarted(WeaponSlot slot)
    {
        // ここでチャージ開始演出を追加する余地あり
    }

    public void OnChargeCanceled(WeaponSlot slot)
    {
        // チャージ解除演出が必要になった際に追記
    }

    public void OnChargeCompleted(WeaponSlot slot)
    {
        // 完了演出をここに追加可能
    }

    public bool CanFireChargedAttack(WeaponSlot slot)
    {
        if (!attackController || !attackController.isActiveAndEnabled)
        {
            return false;
        }

        if (IsGatlingActive())
        {
            return HasValidGatlingBulletPrefab();
        }

        if (!HasValidProjectilePrefab())
        {
            return false;
        }

        return attackController.CanSpawnProjectile();
    }

    private bool HasValidProjectilePrefab()
    {
        if (!chargeProjectilePrefab)
        {
            return false;
        }

        var component = chargeProjectilePrefab.GetComponent<PlayerProjectileBase>();
        if (component)
        {
            hasLoggedInvalidProjectilePrefab = false;
            return true;
        }

        if (!hasLoggedInvalidProjectilePrefab)
        {
            hasLoggedInvalidProjectilePrefab = true;
            Debug.LogWarning("[GunChargeAttack] チャージ弾Prefabに PlayerProjectileBase 派生コンポーネントが設定されていません。", this);
        }

        return false;
    }

    public bool TryFireChargedAttack(WeaponSlot slot)
    {
        if (!CanFireChargedAttack(slot))
        {
            return false;
        }

        if (IsGatlingActive())
        {
            StartGatlingBurst(slot);
            return true;
        }

        var spawnPoint = projectileSpawnPoint ? projectileSpawnPoint : attackController.transform;
        var instance = Instantiate(chargeProjectilePrefab, spawnPoint.position, Quaternion.identity);
        var projectile = instance.GetComponent<PlayerProjectileBase>();
        if (!projectile)
        {
            Debug.LogWarning("[GunChargeAttack] チャージ弾Prefabに PlayerProjectileBase 派生コンポーネントがありません。", this);
            Destroy(instance);
            return false;
        }

        attackController.MirrorWorldAttackInstance(instance.transform);

        if (!attackController.CanSpawnProjectile())
        {
            Destroy(instance);
            return false;
        }

        projectile.Initialize(attackController.FacingDirection, attackController.gameObject, OnProjectileDestroyed);
        attackController.RegisterProjectile(projectile);

        if (chargeShotSfx && sfxSource)
        {
            sfxSource.PlayOneShot(chargeShotSfx, chargeShotVolume);
        }

        return true;
    }

    private void OnProjectileDestroyed(PlayerProjectileBase projectile)
    {
        if (!attackController)
        {
            return;
        }

        attackController.UnregisterProjectile(projectile);
    }

    private bool HasChargeAbility()
    {
        return abilityProvider?.HasAbility(PlayerAbilityType.ChargeShot) ?? false;
    }

    private bool IsGatlingActive()
    {
        return abilityProvider?.HasAbility(PlayerAbilityType.ChargeGatling) ?? false;
    }

    private bool HasValidGatlingBulletPrefab()
    {
        return attackController && attackController.HasUsableBulletPrefab(gatlingBulletPrefab);
    }

    private static WeaponType GetLoadoutWeaponType(WeaponSlot slot)
    {
        return slot == WeaponSlot.Main ? Loadout.Main : Loadout.Sub;
    }

    private void StartGatlingBurst(WeaponSlot slot)
    {
        int index = (int)slot;
        if (gatlingRoutines[index] != null)
        {
            StopCoroutine(gatlingRoutines[index]);
        }

        gatlingRoutines[index] = StartCoroutine(GatlingRoutine(slot, index));
    }

    private void StopAllGatlingRoutines()
    {
        for (int i = 0; i < gatlingRoutines.Length; i++)
        {
            if (gatlingRoutines[i] != null)
            {
                StopCoroutine(gatlingRoutines[i]);
                gatlingRoutines[i] = null;
            }
        }
    }

    private IEnumerator GatlingRoutine(WeaponSlot slot, int index)
    {
        for (int i = 0; i < gatlingBulletCount; i++)
        {
            if (!IsGatlingBurstValid(slot))
            {
                break;
            }

            attackController.FireUncountedBullet(gatlingBulletPrefab);

            if (i < gatlingBulletCount - 1)
            {
                yield return new WaitForSeconds(gatlingFireInterval);
            }
        }

        gatlingRoutines[index] = null;
    }

    private bool IsGatlingBurstValid(WeaponSlot slot)
    {
        if (!attackController || !attackController.isActiveAndEnabled)
        {
            return false;
        }

        if (!HasChargeAbility())
        {
            return false;
        }

        if (!IsGatlingActive())
        {
            return false;
        }

        return GetLoadoutWeaponType(slot) == WeaponType.Gun;
    }
}
