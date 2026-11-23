using UnityEngine;

/// <summary>
/// 床・壁・天井を伝い、角を回る敵の共通移動。速度は Rigidbody2D に合わせ、足が面に付くよう回転し、進行方向へ左右反転する。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class WallEnemyBase : EnemyBase
{
    public enum WallCrawlMode
    {
        ChasePlayer,
        Patrol
    }

    public enum WallCrawlInitialDirection
    {
        [InspectorName("右")]
        Right = 1,
        [InspectorName("左")]
        Left = -1
    }

    private const float Skin = 0.02f;
    private const float SameSurfaceNormalDot = 0.5f;
    private const float ChaseDirectionHysteresis = 0.05f;

    [Header("Wall Crawl")]
    [Tooltip("追跡は今いる面に沿ってプレイヤー側へ進む。巡回は向きを維持し、面が尽きたら反転する。")]
    [SerializeField] private WallCrawlMode crawlMode = WallCrawlMode.ChasePlayer;
    [Tooltip("巡回を始める方向。直立しているときの右が既定。追跡中はプレイヤーがいる側を優先する。")]
    [SerializeField] private WallCrawlInitialDirection initialDirection = WallCrawlInitialDirection.Right;
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [Tooltip("体の中心から接地面までの距離。コライダーの半径に合わせる。")]
    [SerializeField, Min(0.01f)] private float surfaceOffset = 0.5f;
    [Tooltip("この距離まで壁が近づいたら、内側の角として次の面へ回る。")]
    [SerializeField, Min(0.01f)] private float cornerEngageDistance = 0.3f;
    [Tooltip("中心から面までの距離に加えて、面を探す余長。")]
    [SerializeField, Min(0.01f)] private float surfaceProbeDistance = 0.35f;
    [Tooltip("未設定なら Ground レイヤー。")]
    [SerializeField] private LayerMask surfaceMask;

    protected int crawlSign = 1;

    private Vector2 surfaceUp = Vector2.up;
    private bool wasPathBlocked;
    private int appliedFacingSign;
    private readonly RaycastHit2D[] raycastHits = new RaycastHit2D[8];

    protected override void Awake()
    {
        base.Awake();
        ConfigureSurfaceRigidbody();
    }

    protected override void CommonStart()
    {
        base.CommonStart();
        BeginSurfaceCrawl();
    }

    protected override void OnResetState()
    {
        base.OnResetState();
        BeginSurfaceCrawl();
    }

    protected sealed override void CommonUpdate()
    {
        OnWallUpdate();
    }

    protected virtual void OnWallUpdate()
    {
    }

    protected sealed override void CommonFixedUpdate()
    {
        UpdateWallLocomotion();
    }

    protected void BeginSurfaceCrawl()
    {
        ConfigureSurfaceRigidbody();
        crawlSign = InitialCrawlSign();
        wasPathBlocked = false;
        appliedFacingSign = 0;
        surfaceUp = ((Vector2)transform.up).sqrMagnitude > 0.0001f
            ? ((Vector2)transform.up).normalized
            : Vector2.up;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        OnSurfaceCrawlBegan();
        TryInitialAttach();
        ApplyTravelFacing(crawlSign);
    }

    protected virtual void OnSurfaceCrawlBegan()
    {
    }

    private void ConfigureSurfaceRigidbody()
    {
        if (rb == null)
        {
            return;
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.angularVelocity = 0f;
    }

    private void UpdateWallLocomotion()
    {
        if (rb == null)
        {
            return;
        }

        UpdateTravelConstraints();
        int sign = ResolveCrawlSign(surfaceUp);
        bool foundSurface = TryResolveSurface(sign, out RaycastHit2D stickHit, out bool canAdvance);

        if (!foundSurface && ReversesWhenPathEnds)
        {
            int reversedSign = -sign;
            if (TryResolveSurface(reversedSign, out RaycastHit2D recoveredHit, out bool recoveredCanAdvance))
            {
                sign = reversedSign;
                stickHit = recoveredHit;
                canAdvance = recoveredCanAdvance;
                foundSurface = true;
                OnPathEndReversed(sign, surfaceUp);
            }
        }

        if (!foundSurface)
        {
            isGrounded = false;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!canAdvance && ReversesWhenPathEnds)
        {
            if (!wasPathBlocked)
            {
                sign = -sign;
                wasPathBlocked = true;
                OnPathEndReversed(sign, surfaceUp);
                if (TryResolveSurface(sign, out RaycastHit2D reversedHit, out bool reversedCanAdvance) && reversedCanAdvance)
                {
                    stickHit = reversedHit;
                    canAdvance = true;
                }
            }
        }
        else if (canAdvance)
        {
            wasPathBlocked = false;
        }

        crawlSign = sign;
        StickToSurface(stickHit, canAdvance);
        ApplyTravelFacing(crawlSign);
    }

    protected virtual void UpdateTravelConstraints()
    {
    }

    protected virtual bool ReversesWhenPathEnds => crawlMode == WallCrawlMode.Patrol;

    protected virtual void OnPathEndReversed(int newSign, Vector2 surfaceNormal)
    {
    }

    protected virtual bool AllowsSurface(Vector2 normal)
    {
        return true;
    }

    protected virtual int ResolveCrawlSign(Vector2 up)
    {
        if (crawlMode == WallCrawlMode.Patrol || player == null)
        {
            return crawlSign;
        }

        Vector2 tangent = SurfaceTangent(up);
        float side = Vector2.Dot((Vector2)player.position - rb.position, tangent);
        if (side > ChaseDirectionHysteresis)
        {
            return 1;
        }

        if (side < -ChaseDirectionHysteresis)
        {
            return -1;
        }

        return crawlSign;
    }

    private bool TryResolveSurface(int sign, out RaycastHit2D stickHit, out bool canAdvance)
    {
        stickHit = default;
        canAdvance = false;

        Vector2 position = rb.position;
        Vector2 up = surfaceUp;
        Vector2 forward = SurfaceTangent(up) * sign;
        float downDistance = surfaceOffset + surfaceProbeDistance;

        Vector2 footOrigin = position + up * Skin;
        bool hasFoot = TryRaycast(footOrigin, -up, downDistance, out RaycastHit2D footHit);

        bool hasInner = TryRaycast(position, forward, cornerEngageDistance, out RaycastHit2D innerHit)
            && Vector2.Dot(innerHit.normal, up) <= SameSurfaceNormalDot;

        Vector2 aheadOrigin = position + forward * cornerEngageDistance + up * Skin;
        bool hasAhead = TryRaycast(aheadOrigin, -up, downDistance, out RaycastHit2D aheadHit);

        bool hasOuter = false;
        RaycastHit2D outerHit = default;
        if (!hasAhead)
        {
            Vector2 outerOrigin = position + forward * (cornerEngageDistance + Skin) - up * (surfaceOffset + Skin);
            hasOuter = TryRaycast(outerOrigin, -forward, cornerEngageDistance + surfaceOffset, out outerHit);
        }

        bool innerAllowed = hasInner && AllowsSurface(innerHit.normal);
        bool outerAllowed = hasOuter && AllowsSurface(outerHit.normal);
        bool aheadAllowed = hasAhead && AllowsSurface(aheadHit.normal);
        bool blockedByOtherSurface = (hasInner && !innerAllowed) || (hasOuter && !outerAllowed);

        if (innerAllowed)
        {
            stickHit = innerHit;
            canAdvance = true;
            return true;
        }

        if (outerAllowed)
        {
            stickHit = outerHit;
            canAdvance = true;
            return true;
        }

        if (hasFoot && AllowsSurface(footHit.normal))
        {
            stickHit = footHit;
            canAdvance = aheadAllowed && !blockedByOtherSurface;
            return true;
        }

        return false;
    }

    private void TryInitialAttach()
    {
        if (rb == null)
        {
            return;
        }

        float distance = surfaceOffset + surfaceProbeDistance;
        Vector2 position = rb.position;
        if (!TryRaycast(position, -surfaceUp, distance, out RaycastHit2D hit)
            && !TryRaycast(position, Vector2.down, distance, out hit))
        {
            OnInitialSurfaceResolved(surfaceUp, false);
            return;
        }

        surfaceUp = hit.normal.normalized;
        OnInitialSurfaceResolved(surfaceUp, true);
        crawlSign = ResolveCrawlSign(surfaceUp);
        StickToSurface(hit, canAdvance: false);
    }

    protected virtual void OnInitialSurfaceResolved(Vector2 surfaceNormal, bool foundSurface)
    {
    }

    private void StickToSurface(RaycastHit2D hit, bool canAdvance)
    {
        Vector2 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : surfaceUp;
        surfaceUp = normal;

        float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;

        bool chaseWithoutTarget = crawlMode == WallCrawlMode.ChasePlayer && player == null;
        float speed = canAdvance && !chaseWithoutTarget ? moveSpeed : 0f;
        Vector2 travel = SurfaceTangent(normal) * crawlSign;
        Vector2 stickPoint = hit.point + normal * surfaceOffset;
        float deltaTime = Time.fixedDeltaTime;
        Vector2 correction = deltaTime > 0f ? (stickPoint - rb.position) / deltaTime : Vector2.zero;

        rb.linearVelocity = travel * speed + correction;
        isGrounded = true;
    }

    private int InitialCrawlSign()
    {
        return initialDirection == WallCrawlInitialDirection.Left ? -1 : 1;
    }

    private void ApplyTravelFacing(int sign)
    {
        if (sign == 0 || appliedFacingSign == sign)
        {
            return;
        }

        appliedFacingSign = sign;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * -sign;
        transform.localScale = scale;
    }

    private bool TryRaycast(Vector2 origin, Vector2 direction, float distance, out RaycastHit2D closest)
    {
        closest = default;
        if (distance <= 0f || direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(ResolveSurfaceMask());
        filter.useLayerMask = true;

        int count = Physics2D.Raycast(origin, direction.normalized, filter, raycastHits, distance);
        float bestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D candidate = raycastHits[i];
            if (candidate.collider == null || IsOwnCollider(candidate.collider))
            {
                continue;
            }

            if (candidate.distance < bestDistance)
            {
                bestDistance = candidate.distance;
                closest = candidate;
                found = true;
            }
        }

        return found;
    }

    private bool IsOwnCollider(Collider2D collider)
    {
        if (rb != null && collider.attachedRigidbody == rb)
        {
            return true;
        }

        return collider.transform == transform || collider.transform.IsChildOf(transform);
    }

    private LayerMask ResolveSurfaceMask()
    {
        if (surfaceMask.value == 0)
        {
            surfaceMask = LayerMask.GetMask("Ground");
        }

        return surfaceMask;
    }

    private static Vector2 SurfaceTangent(Vector2 up)
    {
        return new Vector2(up.y, -up.x);
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 position = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 up = surfaceUp.sqrMagnitude > 0.0001f ? surfaceUp : (Vector2)transform.up;
        if (up.sqrMagnitude <= 0.0001f)
        {
            up = Vector2.up;
        }

        up.Normalize();
        int gizmoSign = Application.isPlaying ? crawlSign : InitialCrawlSign();
        Vector2 forward = SurfaceTangent(up) * (gizmoSign == 0 ? 1 : gizmoSign);
        float downDistance = surfaceOffset + surfaceProbeDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(position, position - up * downDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + forward * cornerEngageDistance);

        Gizmos.color = Color.cyan;
        Vector2 outerOrigin = position + forward * (cornerEngageDistance + Skin) - up * (surfaceOffset + Skin);
        Gizmos.DrawLine(outerOrigin, outerOrigin - forward * (cornerEngageDistance + surfaceOffset));
    }
}
