using UnityEngine;

public class BitAgent : MonoBehaviour
{
    public enum State { Orbit, Assault, Fire, Return }

    [Header("Move")]
    [SerializeField] private float orbitSnapSpeed = 12f;   // スロット追従速度
    [SerializeField] private float assaultSpeed   = 14f;   // 出撃速度
    [SerializeField] private float returnSpeed    = 16f;   // 帰投速度
    [SerializeField] private float arriveEpsilon  = 0.05f; // 到達判定

    [Header("Attack")]
    [SerializeField] private float approachDistance = 2.4f; // 敵からの射位距離
    [SerializeField] private float fireCooldown = 0.1f;
    [SerializeField] private BitBullet bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float bulletSpeed = 18f;

    private BitManager manager;
    private int slotIndex;
    private Vector3 currentOrbitSlot;
    private State state = State.Orbit;

    private Transform target;       // 追う敵
    private Vector3 firingPoint;    // 射位
    private float fireTimer;
    private int facingSign = 1;     // +1 右向き / -1 左向き
    private float lockedEnemyY;
    private float horizontalSide = 1f; // 敵から見た射位の左右。-1 が左

    // 発射角度制御
    public void SetForceTop(bool v) { forceTopShot = v; Log($"SetForceTop {v}"); }
    private bool forceTopShot; // 攻撃ボタン時に上が押されていたらtrue

    // 外部参照
    public bool IsAvailable => state == State.Orbit || state == State.Return;
    public enum ShotMode { Auto45, TopDown, Front, Back }
    private ShotMode shotMode = ShotMode.Auto45;
    [SerializeField] private Transform player;                 // ← プレイヤーのTransformを割当
    public Transform PlayerPosition => player ? player : transform;

    [SerializeField] private bool debugLog = true;

    void Log(string msg)
    {
    }


    public void Initialize(BitManager m, int index)
    {
        manager = m;
        slotIndex = index;
        state = State.Orbit;
        transform.position = manager.PlayerPosition;
        Log($"Initialize slot={slotIndex} pos={transform.position}");
    }

    public void TickOrbitSlot(Vector3 slotWorld)
    {
        currentOrbitSlot = slotWorld;

        // 周回中だけここで吸着する。帰還は Update の Return が returnSpeed で動かす。
        if (state != State.Orbit)
        {
            return;
        }

        Vector3 dir = currentOrbitSlot - transform.position;
        transform.position += dir.normalized * Mathf.Min(orbitSnapSpeed * Time.deltaTime, dir.magnitude);
    }

    public void CommandAttack(Transform enemy, ShotMode mode, int facing)
    {
        target = enemy;
        shotMode = mode;
        facingSign = facing >= 0 ? 1 : -1;
        if (target && mode != ShotMode.TopDown)
        {
            SnapshotHorizontalApproach();
        }
        firingPoint = target ? ComputeFiringPoint() : ComputeNoTargetPoint();
        state = State.Assault;
        Log($"CommandAttack mode={shotMode} target={(target?target.name:"null")} firingPoint={firingPoint}");
    }

    private void Update()
    {
        var prev = state;
        switch (state)
        {
            case State.Assault:
                // 射位へ向かう
                MoveTowards(firingPoint, assaultSpeed);
                if ((transform.position - firingPoint).sqrMagnitude <= arriveEpsilon * arriveEpsilon)
                {
                    transform.position = firingPoint;
                    fireTimer = 0f;
                    state = State.Fire;
                }
                break;

            case State.Fire:
                fireTimer += Time.deltaTime;
                if (fireTimer >= fireCooldown)
                {
                    FireOnce();
                    state = State.Return;
                }
                break;

            case State.Return:
                // マネージャが毎フレーム currentOrbitSlot を更新済みなのでそこへ帰る
                MoveTowards(currentOrbitSlot, returnSpeed);
                if ((transform.position - currentOrbitSlot).sqrMagnitude <= arriveEpsilon * arriveEpsilon)
                {
                    transform.position = currentOrbitSlot;
                    state = State.Orbit;
                    target = null;
                }
                break;
        }
        if (debugLog && prev != state) Log($"State {prev} -> {state} pos={transform.position}");

        // 目標の敵が動いている間は射位を追従させる（Assault中のみ）
        if (state == State.Assault && target)
            firingPoint = ComputeFiringPoint();

        if (state == State.Assault || state == State.Fire)
            FaceTipTowardEnemy();
    }

    private Vector2 FacingVector => new Vector2(facingSign, 0f);

    private Vector3 PlayerOrigin => manager ? manager.PlayerPosition : transform.position;

    /// <summary>敵がいないとき、向いている方向（背面入力ならその逆）へ撃つ。</summary>
    private Vector2 ComputeNoTargetShotDirection()
    {
        if (shotMode == ShotMode.Back)
        {
            return -FacingVector;
        }

        return FacingVector;
    }

    private Vector3 ComputeNoTargetPoint()
    {
        Vector3 basePos = PlayerOrigin;
        if (shotMode == ShotMode.TopDown)
        {
            return basePos + Vector3.up * approachDistance;
        }

        Vector2 dir = ComputeNoTargetShotDirection();
        return basePos + (Vector3)(dir * approachDistance);
    }

    private Vector3 ComputeFiringPoint()
    {
        if (!target || manager == null)
        {
            return ComputeNoTargetPoint();
        }

        if (shotMode == ShotMode.TopDown)
        {
            Vector3 above = target.position + Vector3.up * approachDistance;
            Log($"ComputeFiringPoint mode={shotMode} point={above}");
            return above;
        }

        // 無入力・左右は、攻撃開始時の敵の高さで横に並ぶ。X だけ敵に追従する。
        Vector3 point = new Vector3(
            target.position.x + horizontalSide * approachDistance,
            lockedEnemyY,
            target.position.z);
        Log($"ComputeFiringPoint mode={shotMode} point={point}");
        return point;
    }

    private void SnapshotHorizontalApproach()
    {
        lockedEnemyY = target.position.y;
        float playerX = manager != null ? manager.PlayerPosition.x : transform.position.x;
        float enemyX = target.position.x;
        if (Mathf.Abs(playerX - enemyX) < 0.0001f)
        {
            horizontalSide = -facingSign;
            return;
        }

        horizontalSide = playerX < enemyX ? -1f : 1f;
    }

    private void FireOnce()
    {
        if (!bulletPrefab) return;

        Vector2 dir;
        if (forceTopShot)
        {
            dir = Vector2.down;
        }
        else if (target && shotMode == ShotMode.TopDown)
        {
            dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        }
        else if (target)
        {
            float dx = target.position.x - transform.position.x;
            float dirX = Mathf.Abs(dx) < 0.0001f ? -horizontalSide : Mathf.Sign(dx);
            dir = new Vector2(dirX, 0f);
        }
        else
        {
            dir = ComputeNoTargetShotDirection();
        }

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = FacingVector;
        }

        var bullet = Instantiate(bulletPrefab, BulletSpawnPosition(), Quaternion.identity);
        bullet.Launch(dir, bulletSpeed);
    }

    /// <summary>角度は変えず、先端（ローカル +X）が敵の左右どちら側かを向くよう scale.x だけ反転する。</summary>
    private void FaceTipTowardEnemy()
    {
        float dirX;
        if (target)
        {
            dirX = target.position.x - transform.position.x;
            if (Mathf.Abs(dirX) < 0.0001f)
            {
                dirX = -horizontalSide;
            }
        }
        else
        {
            dirX = ComputeNoTargetShotDirection().x;
        }

        FaceTipHorizontally(dirX);
    }

    private void FaceTipHorizontally(float worldDirX)
    {
        if (Mathf.Abs(worldDirX) < 0.0001f)
        {
            return;
        }

        float tipX = bulletSpawnPoint ? bulletSpawnPoint.position.x : transform.position.x + transform.lossyScale.x;
        float tipSide = tipX - transform.position.x;
        if (Mathf.Abs(tipSide) < 0.0001f)
        {
            tipSide = transform.lossyScale.x;
        }

        bool tipOnPositiveX = tipSide > 0f;
        bool wantPositiveX = worldDirX > 0f;
        if (tipOnPositiveX == wantPositiveX)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = -scale.x;
        transform.localScale = scale;
    }

    private Vector3 BulletSpawnPosition()
    {
        FaceTipTowardEnemy();
        return bulletSpawnPoint ? bulletSpawnPoint.position : transform.position;
    }

    private void MoveTowards(Vector3 dst, float speed)
    {
        Vector3 d = dst - transform.position;
        if (d.sqrMagnitude <= 0f) return;
        float step = speed * Time.deltaTime;
        transform.position += d.normalized * Mathf.Min(step, d.magnitude);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (state == State.Assault || state == State.Fire)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(firingPoint, 0.05f);
            if (target) {
                Gizmos.DrawLine(transform.position, firingPoint);
                Gizmos.DrawLine(firingPoint, target.position);
            }
        }
    }
#endif
}
