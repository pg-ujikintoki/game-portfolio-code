using UnityEngine;

/// <summary>
/// 壁接触・壁滑り・壁蹴りジャンプを担当する。地形は Ground と同じレイヤーを使い、側面かどうかは法線で判定する。
/// </summary>
public class PlayerWallMotor : MonoBehaviour
{
    [Header("Wall Check")]
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.05f, 0.8f);
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField, Range(0.5f, 1f)] private float wallNormalThreshold = 0.7f;
    [SerializeField] private float wallCastDistance = 0.12f;

    [Header("Wall Kick")]
    [SerializeField] private float wallKickLockDuration = 0.1f;
    [SerializeField] private float wallKickHorizontalMultiplier = 1.2f;
    [SerializeField] private float wallKickDashMultiplier = 1.5f;

    [Header("Wall Slide")]
    [SerializeField] private float wallSlideFallSpeed = 3f;

    private MovingFloor cachedMovingFloor;
    private int movingFloorCacheFrames;
    private const int MovingFloorCacheMaxFrames = 1;
    private float wallKickLockTimer;
    private RaycastHit2D leftWallHit;
    private RaycastHit2D rightWallHit;

    public bool IsSliding { get; private set; }
    public bool IsKickAvailable { get; private set; }
    public bool IsKicking { get; private set; }
    public bool IsLockingHorizontal { get; private set; }
    public bool IsTouchingLeft { get; private set; }
    public bool IsTouchingRight { get; private set; }
    public float DashKickMultiplier => wallKickDashMultiplier;

    private void Awake()
    {
        if (terrainLayer == 0)
        {
            terrainLayer = LayerMask.GetMask("Ground");
        }
    }

    /// <summary>壁蹴り直後の横速度ロック時間を進める。</summary>
    public void TickLock(float deltaTime)
    {
        if (!IsLockingHorizontal)
        {
            return;
        }

        wallKickLockTimer -= deltaTime;
        if (wallKickLockTimer <= 0f)
        {
            IsLockingHorizontal = false;
        }
    }

    /// <summary>左右の側面接触を更新する。ダッシュ解除すべきなら true。</summary>
    public bool RefreshContact(bool isGrounded)
    {
        leftWallHit = default;
        rightWallHit = default;

        GetWorldSideOrigins(out Vector2 leftOrigin, out Vector2 rightOrigin);
        IsTouchingLeft = TryDetectSide(leftOrigin, Vector2.left, ref leftWallHit);
        IsTouchingRight = TryDetectSide(rightOrigin, Vector2.right, ref rightWallHit);

        IsKickAvailable = !isGrounded && (IsTouchingLeft || IsTouchingRight);
        return !IsLockingHorizontal && IsKickAvailable;
    }

    /// <summary>壁に張り付いている間は落下を遅くする。適用したら true。</summary>
    public bool TryApplyWallSlide(ref Vector2 velocity, float moveX, bool isGrounded, bool isJumping)
    {
        bool isStickingWall = !isGrounded && !isJumping && (IsTouchingLeft || IsTouchingRight) && velocity.y <= 0f;
        bool isWallInput = (IsTouchingLeft && moveX < -0.01f) || (IsTouchingRight && moveX > 0.01f);
        if (isStickingWall && isWallInput)
        {
            float floorVelocityY = 0f;
            MovingFloor movingFloor = ResolveMovingFloor();

            if (movingFloor != null && movingFloor.moveDirection == MovingFloor.MoveDirection.Vertical)
            {
                floorVelocityY = movingFloor.GetFloorVelocityY();
            }

            velocity.y = -wallSlideFallSpeed + floorVelocityY;
            IsSliding = true;
            return true;
        }

        if (!isGrounded && !isJumping)
        {
            ClearMovingFloorCache();
        }

        IsSliding = false;
        return false;
    }

    /// <summary>壁と逆方向へ蹴る初速を与える。成功したら true。</summary>
    public bool TryBeginKick(ref Vector2 velocity, float walkSpeed, bool dashHeld, int currentFacing, out int kickFacing)
    {
        kickFacing = currentFacing;
        if (!IsKickAvailable)
        {
            return false;
        }

        IsKicking = true;
        IsLockingHorizontal = true;
        wallKickLockTimer = wallKickLockDuration;

        if (IsTouchingLeft)
        {
            kickFacing = 1;
        }
        else if (IsTouchingRight)
        {
            kickFacing = -1;
        }

        velocity.x = walkSpeed * wallKickHorizontalMultiplier * kickFacing;
        if (dashHeld)
        {
            velocity.x *= wallKickDashMultiplier;
        }

        return true;
    }

    /// <summary>壁蹴り中フラグだけを落とす。</summary>
    public void ClearKicking()
    {
        IsKicking = false;
    }

    /// <summary>壁蹴り・ロック・移動床キャッシュをすべて初期化する。</summary>
    public void ResetAll()
    {
        IsKicking = false;
        IsKickAvailable = false;
        IsLockingHorizontal = false;
        wallKickLockTimer = 0f;
        leftWallHit = default;
        rightWallHit = default;
        ClearMovingFloorCache();
    }

    /// <summary>ワールド左／右のセンサー位置を返す。スプライト反転の影響を打ち消す。</summary>
    private void GetWorldSideOrigins(out Vector2 leftOrigin, out Vector2 rightOrigin)
    {
        Vector2 a = wallCheckLeft != null
            ? (Vector2)wallCheckLeft.position
            : (Vector2)transform.position + Vector2.left * 0.3f;
        Vector2 b = wallCheckRight != null
            ? (Vector2)wallCheckRight.position
            : (Vector2)transform.position + Vector2.right * 0.3f;

        if (a.x <= b.x)
        {
            leftOrigin = a;
            rightOrigin = b;
        }
        else
        {
            leftOrigin = b;
            rightOrigin = a;
        }
    }

    /// <summary>水平 Cast が側面（法線が十分横向き）に当たったか判定する。</summary>
    private bool TryDetectSide(Vector2 sensorOrigin, Vector2 direction, ref RaycastHit2D hit)
    {
        Vector2 origin = transform.position;
        origin.y = sensorOrigin.y;
        float distance = Mathf.Abs(sensorOrigin.x - origin.x) + wallCastDistance;
        distance = Mathf.Max(0.05f, distance);

        hit = Physics2D.BoxCast(origin, wallCheckSize, 0f, direction, distance, terrainLayer);
        if (hit.collider == null)
        {
            return false;
        }

        if (direction.x < 0f)
        {
            return hit.normal.x >= wallNormalThreshold;
        }

        return hit.normal.x <= -wallNormalThreshold;
    }

    /// <summary>側面ヒットしているコライダーから縦移動床を返す。</summary>
    private MovingFloor ResolveMovingFloor()
    {
        MovingFloor movingFloor = FindMovingFloor(leftWallHit.collider);
        if (movingFloor == null)
        {
            movingFloor = FindMovingFloor(rightWallHit.collider);
        }

        if (movingFloor != null)
        {
            cachedMovingFloor = movingFloor;
            movingFloorCacheFrames = MovingFloorCacheMaxFrames;
            return movingFloor;
        }

        if (cachedMovingFloor != null && movingFloorCacheFrames > 0)
        {
            movingFloorCacheFrames--;
            return cachedMovingFloor;
        }

        ClearMovingFloorCache();
        return null;
    }

    private static MovingFloor FindMovingFloor(Collider2D collider)
    {
        if (collider == null)
        {
            return null;
        }

        return collider.GetComponent<MovingFloor>() ?? collider.GetComponentInParent<MovingFloor>();
    }

    /// <summary>縦移動床の1フレームキャッシュを捨てる。</summary>
    private void ClearMovingFloorCache()
    {
        cachedMovingFloor = null;
        movingFloorCacheFrames = 0;
    }

    /// <summary>左右の壁判定ボックスを Scene ビューに描画する。</summary>
    private void OnDrawGizmosSelected()
    {
        GetWorldSideOrigins(out Vector2 leftOrigin, out Vector2 rightOrigin);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(leftOrigin, wallCheckSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(rightOrigin, wallCheckSize);
    }
}
