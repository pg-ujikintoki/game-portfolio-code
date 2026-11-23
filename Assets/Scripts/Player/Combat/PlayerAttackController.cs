using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackController : MonoBehaviour, IChargeWeaponProvider
{

    [Header("Commons")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform spriteHolder; // 反転に使うなら
    [SerializeField] private int facingDirection = 1;
    public int FacingDirection => spawnPointsMirrored ? -facingDirection : facingDirection;


    [Header("Gun")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    public Transform BulletSpawnPoint => bulletSpawnPoint;
    [SerializeField] private float bulletCooldown = 0.2f;
    [SerializeField] private int bulletSimulLimit = 3;

    [SerializeField] private GunChargeAttack gunChargeAttack;

    [SerializeField] private float punchCooldown = 0.5f;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip shootSfx;
    [SerializeField, Range(0, 1)] private float shootSfxVolume = .9f;

    private readonly List<PlayerProjectileBase> activeProjectiles = new();
    private float lastShotTime;
    private Coroutine shootingResetCoroutine;


    [Header("Slash (Combo)")]
    [SerializeField] private GameObject attackHitbox1Prefab;
    [SerializeField] private GameObject attackHitbox2Prefab;
    [SerializeField] private GameObject attackHitbox3Prefab;
    [SerializeField] private Transform slashSpawnPoint;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float[] attackDurations = new float[] { 0.3f, 0.3f, 0.4f };

    // ビット制御
    [SerializeField] private BitManager bit;     // ← インスペクタでプレイヤーの BitManager を割り当て
    [SerializeField] private float targetSearchRadius = 12f;
    [SerializeField] private LayerMask enemyLayer; // EnemyHurtBox と Enemy。EnemyPhysics は弾まで拾うので使わない

    // パンチ
    [Header("Punch")]
    private float lastPunchTime;
    [SerializeField] private PunchHitbox punchHitbox;

    [SerializeField] private UnityEngine.InputSystem.InputActionReference moveAction; // Player/Move を割当

    private bool isAttacking;
    private bool attackQueued;
    private int comboStep; // 0=なし, 1..3
    private bool spawnPointsMirrored;
    private Vector3 bulletSpawnRestLocalPos;
    private Vector3 slashSpawnRestLocalPos;

    void Awake()
    {
        if (!gunChargeAttack)
        {
            gunChargeAttack = GetComponent<GunChargeAttack>();
        }

        if (!slashSpawnPoint)
        {
            slashSpawnPoint = attackOrigin;
        }

        if (bulletSpawnPoint)
        {
            bulletSpawnRestLocalPos = bulletSpawnPoint.localPosition;
        }

        if (slashSpawnPoint)
        {
            slashSpawnRestLocalPos = slashSpawnPoint.localPosition;
        }

        EnsureEnemySearchMask();
    }

    /// <summary>索敵が EnemyPhysics のままだと敵本体を拾えないので、HurtBox を含める。</summary>
    private void EnsureEnemySearchMask()
    {
        int hurtBox = LayerMask.GetMask("EnemyHurtBox");
        if (hurtBox == 0 || (enemyLayer.value & hurtBox) != 0)
        {
            return;
        }

        enemyLayer = hurtBox | LayerMask.GetMask("Enemy");
    }

    void OnEnable()
    {

        Loadout.OnChanged += OnLoadoutChanged;  // ポーズ中の選択変更を捕まえる
    }

    void OnDisable()
    {
        Loadout.OnChanged -= OnLoadoutChanged;
        SetSpawnPointsMirrored(false);
    }

    void OnLoadoutChanged()
    {
        // 「Bit がメインにもサブにも選ばれていない」→ 解除時に消す
        bool hasBitSelected = (Loadout.Main == WeaponType.Bit) || (Loadout.Sub == WeaponType.Bit);
        if (bit) bit.SetRemovalPending(!hasBitSelected);
    }

    public void SetFacingByMove(float moveX)
    {
        if (Mathf.Abs(moveX) <= 0.01f) return;
        facingDirection = (int)Mathf.Sign(moveX);
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        transform.localScale = scale;
    }

    /// <summary>壁ずり落ち中は弾／斬撃の出現位置を左右反転する。</summary>
    public void SetSpawnPointsMirrored(bool mirrored)
    {
        spawnPointsMirrored = mirrored;
        float sign = mirrored ? -1f : 1f;
        ApplySpawnLocalX(bulletSpawnPoint, bulletSpawnRestLocalPos, sign);
        if (slashSpawnPoint != null && slashSpawnPoint != bulletSpawnPoint)
        {
            ApplySpawnLocalX(slashSpawnPoint, slashSpawnRestLocalPos, sign);
        }
    }

    private static void ApplySpawnLocalX(Transform spawn, Vector3 restLocalPos, float sign)
    {
        if (!spawn)
        {
            return;
        }

        Vector3 localPos = restLocalPos;
        localPos.x = restLocalPos.x * sign;
        spawn.localPosition = localPos;
    }

    // ==== Public entry points ====

    public void FireGun()
    {
        if (Time.time - lastShotTime < bulletCooldown) return;
        if (!CanSpawnProjectile()) return;

        var instance = GameObject.Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);

        var bullet = instance.GetComponent<Bullet>();
        if (!bullet)
        {
            Debug.LogWarning("[PlayerAttackController] Bullet prefabに Bullet コンポーネントがありません。発射を中止します。", this);
            Destroy(instance);
            return;
        }

        MirrorWorldAttackInstance(instance.transform);
        bullet.Initialize(FacingDirection, gameObject, OnBulletDestroyed);
        RegisterProjectile(bullet);
        lastShotTime = Time.time;

        if (shootSfx && sfxSource) sfxSource.PlayOneShot(shootSfx, shootSfxVolume);

        if (animator)

        {
            animator.SetBool("isShooting", true);
            if (shootingResetCoroutine != null) StopCoroutine(shootingResetCoroutine);
            shootingResetCoroutine = StartCoroutine(ResetShootingFlagDelayed());
        }
    }

    /// <summary>
    /// 同時発射数制限の対象外で通常弾を発射します。チャージガトリングなど、
    /// 通常のクールダウン/同時発射上限を無視して弾を撃ちたい場合に使用します。
    /// </summary>
    public void FireUncountedBullet(GameObject bulletPrefabOverride)
    {
        var prefab = bulletPrefabOverride ? bulletPrefabOverride : bulletPrefab;
        if (!prefab || !bulletSpawnPoint)
        {
            return;
        }

        var instance = GameObject.Instantiate(prefab, bulletSpawnPoint.position, Quaternion.identity);

        var bullet = instance.GetComponent<Bullet>();
        if (!bullet)
        {
            Debug.LogWarning("[PlayerAttackController] Bullet prefabに Bullet コンポーネントがありません。発射を中止します。", this);
            Destroy(instance);
            return;
        }

        MirrorWorldAttackInstance(instance.transform);
        bullet.Initialize(FacingDirection, gameObject, null);

        if (shootSfx && sfxSource) sfxSource.PlayOneShot(shootSfx, shootSfxVolume);

        if (animator)
        {
            animator.SetBool("isShooting", true);
            if (shootingResetCoroutine != null) StopCoroutine(shootingResetCoroutine);
            shootingResetCoroutine = StartCoroutine(ResetShootingFlagDelayed());
        }
    }

    /// <summary>
    /// 指定した Prefab（未指定時は通常弾 Prefab）が Bullet コンポーネントを持つ有効な弾かどうかを判定します。
    /// </summary>
    public bool HasUsableBulletPrefab(GameObject bulletPrefabOverride)
    {
        var prefab = bulletPrefabOverride ? bulletPrefabOverride : bulletPrefab;
        return prefab != null && prefab.GetComponent<Bullet>() != null;
    }

    public void FireSlash()
    {
        if (!isAttacking)
            StartCoroutine(SlashRoutine());
        else
            attackQueued = true;
    }

    // ==== Internals (ほぼ既存コードの移植) ====

    private IEnumerator ResetShootingFlagDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        if (animator) animator.SetBool("isShooting", false);
        shootingResetCoroutine = null;
    }

    private void OnBulletDestroyed(Bullet bullet)
    {
        if (!bullet)
        {
            return;
        }

        UnregisterProjectile(bullet);
    }


    private IEnumerator SlashRoutine()
    {
        while (true)
        {
            isAttacking = true;
            attackQueued = false;
            comboStep = (comboStep % 3) + 1; // 1→2→3→1…

            string triggerName = $"slash{comboStep}";
            if (animator)
            {
                animator.ResetTrigger("slash1");
                animator.ResetTrigger("slash2");
                animator.ResetTrigger("slash3");
                animator.SetTrigger(triggerName);
            }

            SpawnHitboxForComboStep(comboStep);

            float duration = attackDurations[Mathf.Clamp(comboStep - 1, 0, attackDurations.Length - 1)];
            yield return new WaitForSeconds(duration);

            isAttacking = false;

            if (attackQueued && comboStep < 3) continue;
            comboStep = 0;
            yield break;
        }
    }

    private void SpawnHitboxForComboStep(int step)
    {
        GameObject prefab = step switch
        {
            1 => attackHitbox1Prefab,
            2 => attackHitbox2Prefab,
            3 => attackHitbox3Prefab,
            _ => attackHitbox1Prefab,
        };

        if (step == 3) StartCoroutine(SpawnDelayed(prefab, 0.1f));
        else SpawnNow(prefab);
    }

    private IEnumerator SpawnDelayed(GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnNow(prefab);
    }

    public void FirePunch()
    {
        if (Time.time - lastPunchTime < punchCooldown) return;//Time.timeこれでいいの？
        var instance = GameObject.Instantiate(punchHitbox, bulletSpawnPoint.position, Quaternion.identity);
        lastPunchTime = Time.time;
        instance.hitDirection = new Vector2(FacingDirection, 0f);
        MirrorWorldAttackInstance(instance.transform);

        if (animator)
        {
            animator.SetBool("punch", true);
            //if (shootingResetCoroutine != null) StopCoroutine(shootingResetCoroutine);
            //shootingResetCoroutine = StartCoroutine(ResetShootingFlagDelayed());
        }
    }

    public void FireBy(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Gun:
                FireGun();
                break;
            case WeaponType.Sword:
                FireSlash();
                break;
            case WeaponType.Punch:
                FirePunch();
                break;
            case WeaponType.Bit:
                {
                    if (!bit) break;

                    // 1) 必要数のビットを用意（何度呼んでもOK）
                    bit.EnsurePopulation(gameObject);

                    bit.CommandNext(FindTarget(), DecideShotMode(), FacingDirection);
                    break;
                }
        }
    }

    Transform FindTarget()
    {
        var center = transform.position;
        var hits = Physics2D.OverlapCircleAll(center, targetSearchRadius, enemyLayer);
        if (hits == null || hits.Length == 0) return null;

        float best = float.PositiveInfinity;
        Transform bestTf = null;
        foreach (var h in hits)
        {
            if (!h) continue;
            var enemy = h.GetComponentInParent<EnemyBase>();
            if (!enemy) continue;

            var tf = enemy.transform;
            float d2 = (tf.position - center).sqrMagnitude;
            if (d2 < best) { best = d2; bestTf = tf; }
        }
        return bestTf;
    }

    BitAgent.ShotMode DecideShotMode()
    {
        var v = moveAction ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        // 優先度：上 > 向いている左右 > なし。左右はワールドの右固定ではなく向き基準。
        if (v.y > 0.5f) return BitAgent.ShotMode.TopDown;
        float alongFacing = v.x * FacingDirection;
        if (alongFacing > 0.5f) return BitAgent.ShotMode.Front;
        if (alongFacing < -0.5f) return BitAgent.ShotMode.Back;
        return BitAgent.ShotMode.Auto45;
    }

    public IChargeableWeapon GetChargeableWeapon(WeaponSlot slot)
    {
        var weaponType = slot == WeaponSlot.Main ? Loadout.Main : Loadout.Sub;
        if (weaponType == WeaponType.Gun)
        {
            return gunChargeAttack && gunChargeAttack.isActiveAndEnabled ? gunChargeAttack : null;
        }


        return null;
    }

    private int GetActiveProjectileCount()
    {

        activeProjectiles.RemoveAll(p => p == null);
        return activeProjectiles.Count;
    }

    public bool CanSpawnProjectile()
    {
        return GetActiveProjectileCount() < bulletSimulLimit;
    }

    public void RegisterProjectile(PlayerProjectileBase projectile)
    {
        if (!projectile)
        {
            return;
        }

        if (activeProjectiles.Contains(projectile))
        {
            return;
        }

        activeProjectiles.Add(projectile);
    }


    public void UnregisterProjectile(PlayerProjectileBase projectile)
    {
        if (!projectile)
        {
            return;
        }

        activeProjectiles.Remove(projectile);
    }


    /// <summary>壁ずり落ち中にワールド空間へ出した攻撃見た目を左右反転する。</summary>
    public void MirrorWorldAttackInstance(Transform instance)
    {
        if (!spawnPointsMirrored || instance == null)
        {
            return;
        }

        Vector3 scale = instance.localScale;
        scale.x = Mathf.Abs(scale.x) * FacingDirection;
        instance.localScale = scale;
    }

    private void SpawnNow(GameObject prefab)
    {
        Transform origin = slashSpawnPoint != null ? slashSpawnPoint : attackOrigin;
        var go = Instantiate(prefab, origin.position, Quaternion.identity, origin);
        Vector3 localPos = prefab.transform.localPosition;
        Vector3 localScale = prefab.transform.localScale;
        if (spawnPointsMirrored)
        {
            localPos.x = -localPos.x;
            localScale.x = -localScale.x;
        }

        go.transform.localPosition = localPos;
        go.transform.localRotation = prefab.transform.localRotation;
        go.transform.localScale = localScale;
    }
}
