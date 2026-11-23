using UnityEngine;

public class BossOp : BossBase
{
    [Header("BossOp Attack")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireInterval = 3f;
    public float detectionRange = 10f;

    [Header("BossOp Ground Movement")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float fallSpeed = 5.0f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    [SerializeField] private LayerMask groundLayer;

    private float timer = 0f;
    private Vector2 velocity = Vector2.zero;
    private int facingDirection = 1;
    private float lastTurnTime = float.NegativeInfinity;

    protected override void OnBossResetState()
    {
        timer = 0f;
        velocity = Vector2.zero;
        facingDirection = 1;
        lastTurnTime = float.NegativeInfinity;
        isGrounded = false;
    }

    protected override void OnBossUpdate()
    {
        bool hasTarget = player != null;

        UpdateStandardMovement(
            ref velocity,
            ref facingDirection,
            ref lastTurnTime,
            moveSpeed,
            fallSpeed,
            groundCheck,
            groundCheckSize,
            groundLayer,
            hasTarget,
            hasTarget);

        if (!hasTarget)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= fireInterval)
        {
            Fire();
            timer = 0f;
        }
    }

    protected override void OnBossFixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }

    private void Fire()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(facingDirection, null);
        }
    }
}
