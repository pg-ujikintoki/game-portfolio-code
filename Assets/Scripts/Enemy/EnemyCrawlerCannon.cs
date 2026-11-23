using UnityEngine;

/// <summary>
/// EnemyCrawler と同じ壁の上下往復に加え、一定間隔でプレイヤーへ弾を撃つ。
/// </summary>
public class EnemyCrawlerCannon : EnemyCrawler
{
    [Header("Cannon")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [Tooltip("この秒数ごとに連射を始める。")]
    [SerializeField, Min(0.01f)] private float fireInterval = 3f;
    [Tooltip("1回の連射で出す弾数。各弾はその時点のプレイヤー方向へ飛ぶ。")]
    [SerializeField, Min(1)] private int bulletsPerShot = 3;
    [Tooltip("連射中の、弾と弾の間隔。")]
    [SerializeField, Min(0f)] private float burstInterval = 0.1f;
    [Tooltip("弾の速度。")]
    [SerializeField, Min(0f)] private float bulletSpeed = 20f;

    private float fireTimer;
    private float burstTimer;
    private int shotsRemaining;

    protected override void OnResetState()
    {
        base.OnResetState();
        fireTimer = 0f;
        burstTimer = 0f;
        shotsRemaining = 0;
    }

    protected override void OnWallUpdate()
    {
        if (player == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        if (shotsRemaining > 0)
        {
            fireTimer += deltaTime;
            burstTimer += deltaTime;
            if (burstTimer < burstInterval)
            {
                return;
            }

            FireOne();
            shotsRemaining--;
            burstTimer = 0f;
            return;
        }

        fireTimer += deltaTime;
        if (fireTimer < fireInterval)
        {
            return;
        }

        FireOne();
        shotsRemaining = Mathf.Max(0, bulletsPerShot - 1);
        burstTimer = 0f;
        fireTimer = 0f;
    }

    private void FireOne()
    {
        if (bulletPrefab == null || player == null)
        {
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 toPlayer = (Vector2)player.position - (Vector2)origin;
        float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        GameObject bulletObject = Instantiate(bulletPrefab, origin, Quaternion.Euler(0f, 0f, angle));
        EnemyBullet bullet = bulletObject.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            bullet.Initialize(1, null);
            bullet.speed = bulletSpeed;
        }
    }
}
