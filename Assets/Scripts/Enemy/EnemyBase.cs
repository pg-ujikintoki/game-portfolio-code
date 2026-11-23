using System;
using UnityEngine;
using UnityEngine.Serialization;

public readonly struct DamagePayload
{
    public DamagePayload(int damage, Vector2 hitDirection, bool isPunch, bool isPlayerAttack, int comboPoint)
    {
        Damage = damage;
        HitDirection = hitDirection;
        IsPunch = isPunch;
        IsPlayerAttack = isPlayerAttack;
        ComboPoint = comboPoint;
    }

    public int Damage { get; }
    public Vector2 HitDirection { get; }
    public bool IsPunch { get; }
    public bool IsPlayerAttack { get; }
    public int ComboPoint { get; }
}

public readonly struct DamageResult
{
    public DamageResult(bool damageApplied, bool wasDestroyed, int previousHp, int currentHp)
    {
        DamageApplied = damageApplied;
        WasDestroyed = wasDestroyed;
        PreviousHp = previousHp;
        CurrentHp = currentHp;
    }

    public bool DamageApplied { get; }
    public bool WasDestroyed { get; }
    public int PreviousHp { get; }
    public int CurrentHp { get; }
}

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [FormerlySerializedAs("maxHP")]
    [SerializeField] protected int maxHp = 3;
    [SerializeField] protected int currentHp;
    [SerializeField, Min(0)] private int contactDamage = 1;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public int ContactDamage => Mathf.Max(0, contactDamage);
    public bool IsBoss => isBoss;

    public event Action<EnemyBase, int, int, bool> HpChanged;
    public static event Action<EnemyBase, int, int, bool> GlobalHpChanged;
    public static event Action<EnemyBase> BossAppeared;

    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;

    [Header("Compatibility")]
    public bool isGrounded;
    [SerializeField] private bool isBoss;

    [Header("SFX")]
    [Tooltip("被ダメージ時。ボスもこれを使う。撃破時は鳴らない。")]
    [SerializeField] private AudioClip damageSfx;
    [SerializeField, Range(0f, 1f)] private float damageSfxVolume = 0.9f;
    [Tooltip("撃破時。被ダメージ SE より優先する。ボスは BossBase の撃破 SE が設定されていればそちらを使う。")]
    [SerializeField] private AudioClip defeatSfx;
    [SerializeField, Range(0f, 1f)] private float defeatSfxVolume = 0.9f;

    [Header("Item Drops")]
    [Tooltip("排他抽選です。上から確率を加算し、0〜100の乱数が入った最初の1件だけドロップします。合計が100未満なら何も出ません。Item Prefab は PickupItem をルートまたは子に持つプレハブを指定します。")]
    [SerializeField] private EnemyDropTable dropTable = new EnemyDropTable();

    [Header("Modules")]
    [SerializeField] private MonoBehaviour movementModuleBehaviour;
    [SerializeField] private MonoBehaviour attackModuleBehaviour;

    protected bool isDead;
    protected EnemySpawner spawner;

    private IEnemyMovement movementModule;
    private IEnemyAttack attackModule;
    private Animator cachedAnimator;
    private EnemyShield shield;

    protected MonoBehaviour MovementModuleSource => movementModuleBehaviour;
    protected MonoBehaviour AttackModuleSource => attackModuleBehaviour;

    protected void RegisterMovementModule(MonoBehaviour module)
    {
        movementModuleBehaviour = module;
    }

    protected void RegisterAttackModule(MonoBehaviour module)
    {
        attackModuleBehaviour = module;
    }

    /// <summary>
    /// 盾が砕けたフレーム。同一斬撃が DamageTrigger でも拾われ本体に当たるのを防ぐ。
    /// </summary>
    private int _shieldBrokenFrame = -1;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedAnimator = GetComponentInChildren<Animator>();
        shield = GetComponentInChildren<EnemyShield>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        InitializeModules();

        ResetState();
    }

    protected virtual void Start()
    {
        CommonStart();
    }

    protected virtual void Update()
    {
        float deltaTime = Time.deltaTime;
        movementModule?.Tick(deltaTime);
        attackModule?.Tick(deltaTime);

        CommonUpdate();
    }

    protected virtual void CommonStart()
    {
    }

    protected virtual void CommonUpdate()
    {
    }

    public void InitializeFromSpawner(EnemySpawner owner)
    {
        spawner = owner;
        ResetState();
        OnInitializedFromSpawner();
    }

    protected virtual void OnInitializedFromSpawner()
    {
    }

    public virtual void ResetState()
    {
        currentHp = maxHp;
        isDead = false;
        isGrounded = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        StopAllCoroutines();

        if (cachedAnimator != null)
        {
            cachedAnimator.Rebind();
            cachedAnimator.Update(0f);
        }

        _shieldBrokenFrame = -1;

        movementModule?.ResetState();
        attackModule?.ResetState();

        OnResetState();
        NotifyHpChanged(false);
    }

    protected virtual void OnResetState()
    {
    }

    public void SetBossFlag(bool value)
    {
        isBoss = value;
    }

    public void NotifyBossAppeared()
    {
        if (!isBoss)
        {
            return;
        }

        BossAppeared?.Invoke(this);
    }

    public virtual void TakeDamage(int damage)
    {
        ApplyDamageInternal(new DamagePayload(damage, Vector2.zero, false, false, 0));
    }

    public virtual void TakeDamage(int damage, Vector2 hitDirection, bool isPunch)
    {
        ApplyDamageInternal(new DamagePayload(damage, hitDirection, isPunch, false, 0));
    }

    protected DamageResult ApplyDamage(DamagePayload payload)
    {
        return ApplyDamageInternal(payload);
    }

    protected virtual DamageResult ApplyDamage(int damage, Vector2 hitDirection, bool isPunch, bool isPlayerAttack)
    {
        return ApplyDamageInternal(new DamagePayload(damage, hitDirection, isPunch, isPlayerAttack, 0));
    }

    protected virtual bool CanReceiveDamage(DamagePayload payload)
    {
        return true;
    }

    protected virtual void OnDamageApplied(DamagePayload payload)
    {
    }

    private DamageResult ApplyDamageInternal(DamagePayload payload)
    {
        if (isDead || payload.Damage <= 0)
        {
            return new DamageResult(false, false, currentHp, currentHp);
        }

        if (!CanReceiveDamage(payload))
        {
            return new DamageResult(false, false, currentHp, currentHp);
        }

        int previousHp = currentHp;
        currentHp = Mathf.Max(previousHp - payload.Damage, 0);

        bool damageApplied = currentHp != previousHp;

        if (damageApplied)
        {
            NotifyHpChanged(payload.IsPlayerAttack);
            OnDamageApplied(payload);
        }

        bool wasDestroyed = currentHp <= 0 && damageApplied;
        if (wasDestroyed)
        {
            Die();
        }
        else if (damageApplied)
        {
            PlayDamageSfx();
        }

        return new DamageResult(damageApplied, wasDestroyed, previousHp, currentHp);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead)
        {
            return;
        }

        if (shield != null)
        {
            if (other.CompareTag("Slash"))
            {
                shield.TakeShieldDamage();
            }
            return;
        }

        if (!TryCreateDamagePayload(other, out var payload))
        {
            return;
        }

        var result = ApplyDamage(payload);

        if (other.TryGetComponent<IProjectileDamageListener>(out var listener))
        {
            listener.OnDamageApplied(this, result, other);
            return;
        }

        var parentListener = other.GetComponentInParent<IProjectileDamageListener>();
        if (parentListener != null)
        {
            parentListener.OnDamageApplied(this, result, other);
        }
    }

    private bool TryCreateDamagePayload(Collider2D other, out DamagePayload payload)
    {
        payload = default;

        bool isPlayerAttack = false;
        bool isPunch = false;
        Vector2 hitDirection = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;

        int defaultDamage = 0;
        int defaultComboPoint = 0;

        if (other.CompareTag("Bullet"))
        {
            Bullet bullet = other.GetComponent<Bullet>() ?? other.GetComponentInParent<Bullet>();
            if (bullet != null && bullet.isReflected)
            {
                return false;
            }

            isPlayerAttack = true;
            defaultDamage = 1;
            defaultComboPoint = 1;
        }
        else if (other.CompareTag("Slash"))
        {
            if (Time.frameCount == _shieldBrokenFrame)
            {
                return false;
            }

            isPlayerAttack = true;
            defaultDamage = 3;
            defaultComboPoint = 2;
        }
        else if (other.CompareTag("PunchedEnemy"))
        {
            isPlayerAttack = true;
            defaultDamage = 5;
            defaultComboPoint = 3;
        }
        else if (other.CompareTag("Punch"))
        {
            PunchHitbox punch = other.GetComponent<PunchHitbox>() ?? other.GetComponentInParent<PunchHitbox>();
            if (punch != null)
            {
                hitDirection = punch.hitDirection;
                defaultDamage = punch.Damage;
                defaultComboPoint = punch.ComboPoint;
            }
            else
            {
                defaultDamage = 1;
                defaultComboPoint = 3;
            }

            isPunch = true;
            isPlayerAttack = true;
        }
        else
        {
            return false;
        }

        bool hasAttackStats = TryResolveAttackStats(other, out var attackStats);
        int damage = hasAttackStats && attackStats.Damage > 0 ? attackStats.Damage : defaultDamage;
        int comboPoint = hasAttackStats ? attackStats.ComboPoint : defaultComboPoint;

        if (damage <= 0)
        {
            return false;
        }

        payload = new DamagePayload(damage, hitDirection, isPunch, isPlayerAttack, comboPoint);
        return true;
    }

    private static bool TryResolveAttackStats(Collider2D collider, out AttackStats attackStats)
    {
        if (collider.TryGetComponent<IAttackStatsProvider>(out var directProvider))
        {
            attackStats = directProvider.Stats;
            return true;
        }

        var parentProvider = collider.GetComponentInParent<IAttackStatsProvider>();
        if (parentProvider != null)
        {
            attackStats = parentProvider.Stats;
            return true;
        }

        attackStats = default;
        return false;
    }

    protected virtual void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        PlayDefeatSfx();

        if (dropTable != null)
        {
            dropTable.TrySpawnAt(transform.position);
        }

        if (spawner != null)
        {
            spawner.NotifyEnemyDefeated(this);
        }

        if (isBoss)
        {
            StageManager stageManager = FindObjectOfType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnBossDefeated();
            }
        }

        Destroy(gameObject);
    }

    /// <summary>被ダメージ SE。撃破と同じヒットでは呼ばない。</summary>
    protected virtual void PlayDamageSfx()
    {
        SfxOneShot.Play(damageSfx, damageSfxVolume);
    }

    /// <summary>撃破 SE。ボスは必要なら差し替える。</summary>
    protected virtual void PlayDefeatSfx()
    {
        SfxOneShot.Play(defeatSfx, defeatSfxVolume);
    }

    public virtual void OnShieldBroken()
    {
        shield = null;
        _shieldBrokenFrame = Time.frameCount;
    }

    protected virtual void CommonFixedUpdate()
    {
    }

    private void FixedUpdate()
    {
        float fixedDeltaTime = Time.fixedDeltaTime;
        movementModule?.FixedTick(fixedDeltaTime);
        attackModule?.FixedTick(fixedDeltaTime);

        CommonFixedUpdate();
    }

    public enum AerialVerticalMovementMode
    {
        None,
        PingPong,
        Sine
    }

    protected bool UpdateFacingDirectionTowardsPlayer(
        ref int facingDirection,
        ref float lastTurnTime,
        float turnCooldown = 0f,
        bool forceApply = false)
    {
        if (player == null)
        {
            return false;
        }

        int desiredDirection = player.position.x > transform.position.x ? 1 : -1;
        if (!forceApply && desiredDirection == facingDirection)
        {
            return false;
        }

        if (!forceApply && turnCooldown > 0f && Time.time - lastTurnTime < turnCooldown)
        {
            return false;
        }

        facingDirection = desiredDirection;
        lastTurnTime = Time.time;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * -facingDirection;
        transform.localScale = scale;

        return true;
    }

    protected void UpdateAerialMovement(
        Transform targetTransform,
        float baseY,
        float moveRange,
        float moveSpeed,
        AerialVerticalMovementMode verticalMode,
        bool followPlayerHorizontally = false,
        float horizontalSpeed = 0f,
        float horizontalOffset = 0f)
    {
        if (targetTransform == null)
        {
            return;
        }

        Vector3 position = targetTransform.position;
        float nextY = baseY;

        switch (verticalMode)
        {
            case AerialVerticalMovementMode.None:
                nextY = baseY;
                break;
            case AerialVerticalMovementMode.PingPong:
                nextY = baseY + Mathf.PingPong(Time.time * Mathf.Max(moveSpeed, 0f), moveRange * 2f) - moveRange;
                break;
            case AerialVerticalMovementMode.Sine:
                nextY = baseY + Mathf.Sin(Time.time * moveSpeed) * moveRange;
                break;
        }

        position.y = nextY;

        if (followPlayerHorizontally && player != null)
        {
            float targetX = player.position.x + horizontalOffset;
            float speed = Mathf.Max(horizontalSpeed, 0f);
            if (speed > 0f)
            {
                position.x = Mathf.MoveTowards(position.x, targetX, speed * Time.deltaTime);
            }
            else
            {
                position.x = targetX;
            }
        }

        targetTransform.position = position;
    }

    protected void NotifyHpChanged(bool isPlayerAttack)
    {
        HpChanged?.Invoke(this, currentHp, maxHp, isPlayerAttack);
        GlobalHpChanged?.Invoke(this, currentHp, maxHp, isPlayerAttack);
    }

    protected void UpdateStandardMovement(
        ref Vector2 velocity,
        ref int facingDirection,
        ref float lastTurnTime,
        float horizontalSpeed,
        float fallSpeedValue,
        Transform groundCheckTransform,
        Vector2 groundCheckDimensions,
        LayerMask groundLayerMask,
        bool enableHorizontalMovement,
        bool enableAutoTurn,
        float turnCooldown = 0f)
    {
        if (groundCheckTransform != null)
        {
            isGrounded = Physics2D.OverlapBox(
                groundCheckTransform.position,
                groundCheckDimensions,
                0f,
                groundLayerMask);
        }

        if (enableAutoTurn)
        {
            UpdateFacingDirectionTowardsPlayer(ref facingDirection, ref lastTurnTime, turnCooldown);
        }

        if (enableHorizontalMovement)
        {
            velocity.x = horizontalSpeed * facingDirection;
        }
        else
        {
            velocity.x = 0f;
        }

        if (isGrounded)
        {
            velocity.y = 0f;
        }
        else
        {
            velocity.y = -Mathf.Abs(fallSpeedValue);
        }
    }

    private void InitializeModules()
    {
        movementModule = ResolveMovementModule(movementModuleBehaviour);
        attackModule = ResolveAttackModule(attackModuleBehaviour);
    }

    private IEnemyMovement ResolveMovementModule(MonoBehaviour candidate)
    {
        IEnemyMovement module = null;

        if (candidate != null)
        {
            module = candidate as IEnemyMovement;
            if (module == null)
            {
                Debug.LogWarning($"Movement module assigned to {name} does not implement IEnemyMovement.", this);
            }
        }
        else
        {
            module = GetComponent<IEnemyMovement>();
        }

        if (module != null)
        {
            module.Initialize(this);
        }

        return module;
    }

    private IEnemyAttack ResolveAttackModule(MonoBehaviour candidate)
    {
        IEnemyAttack module = null;

        if (candidate != null)
        {
            module = candidate as IEnemyAttack;
            if (module == null)
            {
                Debug.LogWarning($"Attack module assigned to {name} does not implement IEnemyAttack.", this);
            }
        }
        else
        {
            module = GetComponent<IEnemyAttack>();
        }

        if (module != null)
        {
            module.Initialize(this);
        }

        return module;
    }
}
