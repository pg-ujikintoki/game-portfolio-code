using UnityEngine;

public class Boss01 : BossBase
{
    private enum BossActionType
    {
        BurstShot,
        WalkToSnapshotX,
        JumpToSnapshotX
    }

    private enum BossActionState
    {
        Idle,
        Acting,
        Cooldown
    }

    [Header("Attack")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float burstShotInterval = 0.3f;

    [Header("Ground Movement")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float fallSpeed = 5.0f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float arrivalToleranceX = 0.1f;
    [SerializeField] private float initialActionDelaySeconds = 0.5f;
    [SerializeField] private float actionCooldownSeconds = 1.0f;
    [SerializeField] private float jumpPower = 5.0f;
    [SerializeField] private float maxJumpTime = 0.5f;
    [SerializeField] private float jumpHorizontalSpeedCap = 12.0f;

    private Vector2 velocity = Vector2.zero;
    private int facingDirection = 1;
    private float lastTurnTime = float.NegativeInfinity;
    private BossActionState actionState = BossActionState.Idle;
    private BossActionType currentAction = BossActionType.BurstShot;
    private BossActionType? lastAction;
    private float initialActionDelayTimer = 0f;
    private float actionCooldownTimer = 0f;
    private float actionStartPlayerX = 0f;
    private int burstShotsFired = 0;
    private float burstShotTimer = 0f;
    private bool jumpStarted = false;
    private bool jumpAscending = false;
    private bool jumpHasLeftGround = false;
    private float jumpTimer = 0f;
    private float jumpHorizontalSpeed = 0f;

    protected override void OnBossResetState()
    {
        velocity = Vector2.zero;
        facingDirection = 1;
        lastTurnTime = float.NegativeInfinity;
        isGrounded = false;
        actionState = BossActionState.Idle;
        initialActionDelayTimer = Mathf.Max(0f, initialActionDelaySeconds);
        actionCooldownTimer = 0f;
        lastAction = null;
        actionStartPlayerX = 0f;
        burstShotsFired = 0;
        burstShotTimer = 0f;
        jumpStarted = false;
        jumpAscending = false;
        jumpHasLeftGround = false;
        jumpTimer = 0f;
        jumpHorizontalSpeed = 0f;
    }

    protected override void OnBossUpdate()
    {
        if (initialActionDelayTimer > 0f)
        {
            UpdateInitialActionDelay();
            if (initialActionDelayTimer > 0f)
            {
                return;
            }
        }

        switch (actionState)
        {
            case BossActionState.Idle:
                BeginNextAction();
                break;
            case BossActionState.Acting:
                UpdateActingState();
                break;
            case BossActionState.Cooldown:
                UpdateCooldownState();
                break;
        }
    }

    protected override void OnBossFixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }

    private void UpdateInitialActionDelay()
    {
        UpdateStandardMovement(
            ref velocity,
            ref facingDirection,
            ref lastTurnTime,
            moveSpeed,
            fallSpeed,
            groundCheck,
            groundCheckSize,
            groundLayer,
            false,
            false);

        initialActionDelayTimer -= Time.deltaTime;
    }

    private void BeginNextAction()
    {
        currentAction = SelectNextAction();
        actionStartPlayerX = player != null ? player.position.x : transform.position.x;
        InitializeCurrentAction();
        actionState = BossActionState.Acting;
    }

    private void InitializeCurrentAction()
    {
        switch (currentAction)
        {
            case BossActionType.BurstShot:
                burstShotsFired = 0;
                burstShotTimer = 0f;
                if (player != null)
                {
                    UpdateFacingDirectionTowardsPlayer(ref facingDirection, ref lastTurnTime);
                }
                break;
            case BossActionType.WalkToSnapshotX:
                SetFacingDirectionTowardsX(actionStartPlayerX);
                break;
            case BossActionType.JumpToSnapshotX:
                PrepareJumpAction();
                break;
        }
    }

    private BossActionType SelectNextAction()
    {
        if (!lastAction.HasValue)
        {
            return (BossActionType)Random.Range(0, 3);
        }

        int previous = (int)lastAction.Value;
        int roll = Random.Range(0, 2);
        int next = (previous + 1 + roll) % 3;
        return (BossActionType)next;
    }

    private void UpdateActingState()
    {
        bool isCompleted;
        switch (currentAction)
        {
            case BossActionType.BurstShot:
                isCompleted = UpdateBurstShotAction();
                break;
            case BossActionType.WalkToSnapshotX:
                isCompleted = UpdateWalkToSnapshotXAction();
                break;
            case BossActionType.JumpToSnapshotX:
                isCompleted = UpdateJumpToSnapshotXAction();
                break;
            default:
                isCompleted = true;
                break;
        }

        if (!isCompleted)
        {
            return;
        }

        velocity.x = 0f;
        lastAction = currentAction;
        actionCooldownTimer = Mathf.Max(0f, actionCooldownSeconds);
        actionState = BossActionState.Cooldown;
    }

    private void UpdateCooldownState()
    {
        UpdateStandardMovement(
            ref velocity,
            ref facingDirection,
            ref lastTurnTime,
            moveSpeed,
            fallSpeed,
            groundCheck,
            groundCheckSize,
            groundLayer,
            false,
            false);

        if (actionCooldownTimer > 0f)
        {
            actionCooldownTimer -= Time.deltaTime;
        }

        if (actionCooldownTimer > 0f)
        {
            return;
        }

        actionState = BossActionState.Idle;
    }

    private bool UpdateBurstShotAction()
    {
        UpdateStandardMovement(
            ref velocity,
            ref facingDirection,
            ref lastTurnTime,
            moveSpeed,
            fallSpeed,
            groundCheck,
            groundCheckSize,
            groundLayer,
            false,
            false);

        burstShotTimer -= Time.deltaTime;
        bool canShootNow = burstShotsFired == 0 || burstShotTimer <= 0f;
        if (!canShootNow)
        {
            return false;
        }

        if (!TryFireBullet())
        {
            return true;
        }

        burstShotsFired++;
        burstShotTimer = Mathf.Max(0.01f, burstShotInterval);
        return burstShotsFired >= 3;
    }

    private bool UpdateWalkToSnapshotXAction()
    {
        SetFacingDirectionTowardsX(actionStartPlayerX);

        float distanceToTargetX = Mathf.Abs(actionStartPlayerX - transform.position.x);
        float tolerance = Mathf.Max(0.01f, arrivalToleranceX);
        if (distanceToTargetX <= tolerance)
        {
            UpdateStandardMovement(
                ref velocity,
                ref facingDirection,
                ref lastTurnTime,
                moveSpeed,
                fallSpeed,
                groundCheck,
                groundCheckSize,
                groundLayer,
                false,
                false);
            return true;
        }

        UpdateStandardMovement(
            ref velocity,
            ref facingDirection,
            ref lastTurnTime,
            moveSpeed,
            fallSpeed,
            groundCheck,
            groundCheckSize,
            groundLayer,
            true,
            false);
        return false;
    }

    private bool UpdateJumpToSnapshotXAction()
    {
        if (!jumpStarted)
        {
            PrepareJumpAction();
        }

        UpdateStandardMovement(
            ref velocity,
            ref facingDirection,
            ref lastTurnTime,
            moveSpeed,
            fallSpeed,
            groundCheck,
            groundCheckSize,
            groundLayer,
            false,
            false);

        velocity.x = jumpHorizontalSpeed * facingDirection;

        if (jumpAscending)
        {
            jumpTimer += Time.deltaTime;
            if (jumpTimer < maxJumpTime)
            {
                velocity.y = jumpPower;
            }
            else
            {
                jumpAscending = false;
            }
        }

        if (!jumpHasLeftGround && !isGrounded)
        {
            jumpHasLeftGround = true;
        }

        if (jumpHasLeftGround && isGrounded)
        {
            velocity.x = 0f;
            return true;
        }

        return false;
    }

    private bool TryFireBullet()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            return false;
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript == null)
        {
            return false;
        }

        bulletScript.Initialize(facingDirection, null);
        return true;
    }

    private void SetFacingDirectionTowardsX(float targetX)
    {
        float deltaX = targetX - transform.position.x;
        if (Mathf.Abs(deltaX) <= 0.001f)
        {
            return;
        }

        facingDirection = deltaX >= 0f ? 1 : -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * -facingDirection;
        transform.localScale = scale;
    }

    private void PrepareJumpAction()
    {
        SetFacingDirectionTowardsX(actionStartPlayerX);

        jumpStarted = true;
        jumpAscending = true;
        jumpHasLeftGround = false;
        jumpTimer = 0f;

        float targetDistance = Mathf.Abs(actionStartPlayerX - transform.position.x);
        float estimatedAirTime = EstimateJumpAirTime();
        float calculatedSpeed = targetDistance / estimatedAirTime;
        jumpHorizontalSpeed = Mathf.Min(calculatedSpeed, Mathf.Max(0f, jumpHorizontalSpeedCap));
    }

    private float EstimateJumpAirTime()
    {
        float safeMaxJumpTime = Mathf.Max(0.01f, maxJumpTime);
        float safeFallSpeed = Mathf.Max(0.01f, fallSpeed);
        float safeJumpPower = Mathf.Max(0f, jumpPower);

        // 現在のジャンプ実装は「一定上昇速度で maxJumpTime 上昇」なので、頂点高は speed*time で近似できる。
        float apexHeight = safeJumpPower * safeMaxJumpTime;
        float fallDuration = apexHeight / safeFallSpeed;
        float totalAirTime = safeMaxJumpTime + fallDuration;
        return Mathf.Max(0.01f, totalAirTime);
    }
}
