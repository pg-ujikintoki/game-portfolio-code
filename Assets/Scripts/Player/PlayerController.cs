using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの移動・被ダメージ・落下救済をまとめる中核。壁処理は PlayerWallMotor に委譲する。
/// </summary>
[RequireComponent(typeof(PlayerWallMotor))]
[RequireComponent(typeof(PlayerInputRouter))]
public partial class PlayerController : MonoBehaviour
{
    private const string AnimWalking = "isWalking";
    private const string AnimJumpUping = "isJumpUping";
    private const string AnimJumpDowning = "isJumpDowning";
    private const string AnimDashing = "isDashing";
    private const string AnimWallSliding = "isWallSliding";
    private const string AnimKnockback = "isKnockback";
    private const string EnemyBulletTag = "EnemyBullet";

    public bool IsWallSliding => wallMotor != null && wallMotor.IsSliding;

    private bool isInvincible = false;
    private float invincibleTimer = 0f;
    private const float InvincibleDuration = 1.0f;
    private const int DefaultEnemyContactDamage = 1;
    [SerializeField] private float invincibleBlinkInterval = 0.1f;

    private SpriteRenderer[] spriteRenderers;
    private Coroutine invincibilityBlinkCoroutine;
    private int playerLayer;
    private int enemyLayer;
    private int enemyPhysicsLayer;
    private bool isEnemyCollisionIgnored = false;

    private readonly HashSet<EnemyContactDamager> activeContactDamagers = new();
    private static readonly List<EnemyContactDamager> contactDamagerBuffer = new();
    private Collider2D[] playerColliders;

    public float walkSpeed = 5f;
    public float dashSpeed = 10f;
    public float jumpPower = 5f;
    public float maxJumpTime = 0.5f;
    public float fallSpeed = 5f;
    public float maxDashDuration = 0.5f;

    public bool isGrounded = false;
    public bool isJumping = false;
    private float jumpTimer = 0f;

    public bool isDashing = false;
    private float dashTimer = 0f;
    private int facingDirection = 1;
    private bool isChangedFacingDirection;

    [SerializeField] public Rigidbody2D rb;

    Animator animator;
    public bool isAnimatorWalking;
    public bool isAnimatorJumpUping;
    public bool isAnimatorJumpDowning;
    public bool isAnimatorDashing;
    public bool isAnimatorWallSliding;
    public bool isAnimatorKnockback;

    private Vector2 velocity = Vector2.zero;

    [Header("Life")]
    [SerializeField] private PlayerLife life;

    public float knockbackDistance = 0.1f;
    public float knockbackDuration = 0.5f;
    private bool isControlLocked = false;
    public bool isControlLockedStage = false;
    private bool scriptedWalkActive;
    private bool scriptedFallActive;

    [SerializeField] private Transform spriteHolder;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    [SerializeField] private LayerMask groundLayer;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];

    private float coyoteTime = 0.1f;
    public float groundedIgnoreTimer = 0f;

    [Header("Fall Rescue Settings")]
    [SerializeField] float fallRescueTotalDuration = 5f;
    [SerializeField] float fallRescueRiseDuration = 0.3f;
    [SerializeField] float fallRescueRiseSpeed = 18f;
    [SerializeField] float fallRescueFloatFallSpeed = 2f;
    [SerializeField] float fallRescueHorizontalSpeedMul = 0.7f;
    [SerializeField] float fallRescueJumpPower = 5f;
    bool isFallRescueActive;
    bool isFallRescueFloatingPhase;
    float fallRescueTimer;
    public bool IsFallRescueActive => isFallRescueActive;

    [Header("SFX: Dash")]
    [SerializeField] private AudioClip dashSfx;
    [SerializeField, Range(0f, 1f)] private float dashSfxVolume = 0.9f;
    [SerializeField] private Vector2 dashSfxPitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private float dashSfxCooldown = 0.05f;

    private AudioSource sfxSource;
    private float lastDashSfxTime = -999f;

    private PlayerInputRouter inputRouter;
    private PlayerAttackController attack;
    private PlayerWallMotor wallMotor;

    private Vector2 MoveInput => inputRouter != null ? inputRouter.Move : Vector2.zero;
    private bool JumpPressed => inputRouter != null && inputRouter.JumpPressed;
    private bool JumpHeld => inputRouter != null && inputRouter.JumpHeld;
    private bool JumpReleased => inputRouter != null && inputRouter.JumpReleased;
    private bool DashPressed => inputRouter != null && inputRouter.DashPressed;
    private bool DashHeld => inputRouter != null && inputRouter.DashHeld;
    private bool DashReleased => inputRouter != null && inputRouter.DashReleased;
    private bool IsHorizontalLocked => wallMotor != null && wallMotor.IsLockingHorizontal;

    /// <summary>参照の取得と、被ダメージ・SE 用の初期化を行う。</summary>
    void Awake()
    {
        EnsureSfxSource();

        inputRouter = GetComponent<PlayerInputRouter>();
        attack = GetComponent<PlayerAttackController>();
        wallMotor = GetComponent<PlayerWallMotor>();

        CacheSpriteRenderers();
        InitializeCollisionLayers();
        RefreshPlayerColliders();
    }

    /// <summary>攻撃入力の購読を解除する。</summary>
    void OnDestroy()
    {
        UnsubscribeAttackInput();
    }

    /// <summary>メイン武器を発射する。</summary>
    void OnFireMain()
    {
        FireFromLoadout(Loadout.Main);
    }

    /// <summary>サブ武器を発射する。</summary>
    void OnFireSub()
    {
        FireFromLoadout(Loadout.Sub);
    }

    /// <summary>コライダー再取得と攻撃入力の購読を行う。</summary>
    void OnEnable()
    {
        RefreshPlayerColliders();
        SubscribeAttackInput();
    }

    /// <summary>攻撃入力の購読解除と、接触ダメージ状態のリセットを行う。</summary>
    void OnDisable()
    {
        UnsubscribeAttackInput();

        activeContactDamagers.Clear();
        SetEnemyCollisionEnabled(true);
    }

    /// <summary>Animator と体力コンポーネントを取得し、開始時は空中扱いする。</summary>
    void Start()
    {
        animator = GetComponent<Animator>();

        if (life == null)
        {
            life = GetComponent<PlayerLife>();
        }

        isGrounded = false;
    }

    /// <summary>入力・壁・移動・被ダメージを1フレーム分更新する。</summary>
    void Update()
    {
        if (PauseState.IsPaused)
        {
            return;
        }

        if (scriptedWalkActive)
        {
            UpdateScriptedWalk();
            return;
        }

        if (scriptedFallActive)
        {
            UpdateScriptedFall();
            return;
        }

        if (isControlLockedStage)
        {
            velocity = Vector2.zero;
            SetLocomotionAnimatorBools(false, false, false, false, false);
            SyncAttackSpawnMirror();
            return;
        }

        if (!isControlLocked)
        {
            CheckGrounded();
        }

        UpdateWallState();

        float moveX = MoveInput.x;
        UpdateVerticalMovement(moveX);
        UpdateInvincibility();

        if (isControlLocked)
        {
            ApplyWallSlidingAnimator(false);
            SyncAttackSpawnMirror();
            return;
        }

        if (isGrounded)
        {
            SetJumpAnimator(false, false);
        }

        isAnimatorWalking = isGrounded && Mathf.Abs(moveX) > 0.01f;
        UpdateFacing(moveX);
        attack?.SetFacingByMove(moveX);

        if (isFallRescueActive)
        {
            HandleFallRescue(moveX);
            ApplyWallSlidingAnimator(false);
        }
        else
        {
            HandleLocomotion(moveX);
        }

        SyncAttackSpawnMirror();
    }

    /// <summary>組み立てた速度を Rigidbody へ反映する。</summary>
    void FixedUpdate()
    {
        if (PauseState.IsPaused) return;
        rb.linearVelocity = velocity;
    }

    /// <summary>ボス部屋への侵入演出で、移動・攻撃・チャージを止める。</summary>
    public void BeginBossEntryLock()
    {
        scriptedWalkActive = false;
        scriptedFallActive = false;
        isControlLockedStage = true;
        isDashing = false;
        isJumping = false;
        groundedIgnoreTimer = 0f;
        wallMotor?.ResetAll();
        inputRouter?.SetGameplayInputBlocked(true);
        StopBody();
        SetLocomotionAnimatorBools(false, false, false, false, false);
        GetComponent<PlayerChargeController>()?.CancelChargesForStageLock();
    }

    /// <summary>侵入演出の操作ロックを解除する。</summary>
    public void EndBossEntryLock()
    {
        scriptedWalkActive = false;
        scriptedFallActive = false;
        isControlLockedStage = false;
        velocity.x = 0f;
        isAnimatorWalking = false;
        inputRouter?.SetGameplayInputBlocked(false);
    }

    /// <summary>着地するまで、その場で落下させる。</summary>
    public void BeginScriptedFall()
    {
        scriptedFallActive = true;
        scriptedWalkActive = false;
        isDashing = false;
        isJumping = false;
        groundedIgnoreTimer = 0f;
        isGrounded = false;
        velocity.x = 0f;
    }

    /// <summary>その場落下を止め、横速度を切る。</summary>
    public void EndScriptedFall()
    {
        scriptedFallActive = false;
        velocity.x = 0f;
        if (isGrounded)
        {
            velocity.y = 0f;
        }
    }

    /// <summary>右方向への演出歩行を始める。</summary>
    public void BeginScriptedWalk()
    {
        scriptedFallActive = false;
        scriptedWalkActive = true;
        isDashing = false;
        isJumping = false;
        groundedIgnoreTimer = 0f;
        UpdateFacing(1f);
    }

    /// <summary>演出歩行を止め、横速度を切る。</summary>
    public void EndScriptedWalk()
    {
        scriptedWalkActive = false;
        velocity.x = 0f;
        isAnimatorWalking = false;
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        SetLocomotionAnimatorBools(false, false, false, false, false);
    }

    /// <summary>操作を止め、移動・壁・入力状態を初期化する。</summary>
    public void DisableControl()
    {
        StopAllCoroutines();
        StopInvincibilityBlink();

        isControlLocked = true;
        isControlLockedStage = true;
        isFallRescueActive = false;
        isFallRescueFloatingPhase = false;
        isJumping = false;
        isDashing = false;
        wallMotor?.ResetAll();
        inputRouter?.SetGameplayInputBlocked(true);
        StopBody();
        SetLocomotionAnimatorBools(false, false, false, false, false);
    }

    /// <summary>操作ロックを解除し、押し続け中の入力を現在のボタン状態へ戻す。</summary>
    public void EnableControl()
    {
        isControlLocked = false;
        isControlLockedStage = false;
        inputRouter?.SetGameplayInputBlocked(false);
        StopBody();
    }

    /// <summary>接地判定ボックスを Scene ビューに描画する。</summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }

    /// <summary>ロードアウトの指定スロットを発射する。</summary>
    private void FireFromLoadout(WeaponType weaponType)
    {
        if (PauseState.IsPaused || isControlLockedStage)
        {
            return;
        }

        attack.FireBy(weaponType);
    }

    /// <summary>ダッシュ用 AudioSource が無ければ追加する。</summary>
    private void EnsureSfxSource()
    {
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource != null)
        {
            return;
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    /// <summary>速度と Rigidbody を停止する。</summary>
    private void StopBody()
    {
        velocity = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>移動系 Animator パラメータを書き込む。</summary>
    private void SetLocomotionAnimatorBools(bool walking, bool jumpUp, bool jumpDown, bool dashing, bool wallSliding)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(AnimWalking, walking);
        animator.SetBool(AnimJumpUping, jumpUp);
        animator.SetBool(AnimJumpDowning, jumpDown);
        animator.SetBool(AnimDashing, dashing);
        animator.SetBool(AnimWallSliding, wallSliding);
    }

    /// <summary>壁ずり落ちアニメ用フラグだけを Animator へ送る。</summary>
    private void ApplyWallSlidingAnimator(bool wallSliding)
    {
        isAnimatorWallSliding = wallSliding;
        if (animator == null)
        {
            return;
        }

        animator.SetBool(AnimWallSliding, wallSliding);
    }

    /// <summary>入力を使わず、接地するまで落下させる。</summary>
    private void UpdateScriptedFall()
    {
        CheckGrounded();
        isDashing = false;
        isJumping = false;
        velocity.x = 0f;
        isAnimatorWalking = false;
        isAnimatorDashing = false;
        isAnimatorKnockback = false;
        if (isGrounded)
        {
            velocity.y = 0f;
            isAnimatorJumpUping = false;
            isAnimatorJumpDowning = false;
        }
        else
        {
            UpdateVerticalMovement(0f);
            isAnimatorJumpUping = false;
            isAnimatorJumpDowning = true;
        }

        ApplyAnimatorParameters();
        SyncAttackSpawnMirror();
    }

    /// <summary>入力を使わず、歩行速度で右へ進む。</summary>
    private void UpdateScriptedWalk()
    {
        CheckGrounded();
        isDashing = false;
        isJumping = false;
        UpdateFacing(1f);
        UpdateVerticalMovement(0f);
        velocity.x = walkSpeed;
        isAnimatorWalking = isGrounded;
        isAnimatorDashing = false;
        isAnimatorKnockback = false;
        if (!isGrounded)
        {
            isAnimatorJumpUping = false;
            isAnimatorJumpDowning = true;
        }

        ApplyAnimatorParameters();
        SyncAttackSpawnMirror();
    }

    /// <summary>壁ずり落ち中だけ弾／斬撃出現位置を左右反転する。</summary>
    private void SyncAttackSpawnMirror()
    {
        bool mirror = !isControlLocked && !isControlLockedStage && !isFallRescueActive && IsWallSliding;
        attack?.SetSpawnPointsMirrored(mirror);
    }

    /// <summary>ジャンプ上昇／下降フラグをセットする。</summary>
    private void SetJumpAnimator(bool jumpUp, bool jumpDown)
    {
        isAnimatorJumpUping = jumpUp;
        isAnimatorJumpDowning = jumpDown;
    }

    /// <summary>通常ジャンプ開始時の共通フラグをセットする。</summary>
    private void BeginJump()
    {
        isJumping = true;
        jumpTimer = 0f;
        isAnimatorDashing = false;
        isAnimatorWalking = false;
        isGrounded = false;
        groundedIgnoreTimer = coyoteTime;
    }

    /// <summary>壁接触とキック後ロックを更新し、必要ならダッシュを解除する。</summary>
    private void UpdateWallState()
    {
        if (wallMotor == null)
        {
            return;
        }

        wallMotor.TickLock(Time.deltaTime);
        if (wallMotor.RefreshContact(isGrounded))
        {
            isDashing = false;
        }
    }

    /// <summary>壁滑り、または通常落下の縦速度を適用する。</summary>
    private void UpdateVerticalMovement(float moveX)
    {
        bool appliedSlide = wallMotor != null && wallMotor.TryApplyWallSlide(ref velocity, moveX, isGrounded, isJumping);
        if (!appliedSlide && !isGrounded && !isJumping)
        {
            velocity.y = -fallSpeed;
        }
    }

    /// <summary>メイン／サブ発射イベントを購読する。</summary>
    private void SubscribeAttackInput()
    {
        if (inputRouter == null)
        {
            return;
        }

        inputRouter.FireMain += OnFireMain;
        inputRouter.FireSub += OnFireSub;
    }

    /// <summary>メイン／サブ発射イベントの購読を解除する。</summary>
    private void UnsubscribeAttackInput()
    {
        if (inputRouter == null)
        {
            return;
        }

        inputRouter.FireMain -= OnFireMain;
        inputRouter.FireSub -= OnFireSub;
    }
}
