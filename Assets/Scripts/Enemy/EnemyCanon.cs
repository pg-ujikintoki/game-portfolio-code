using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCanon : GroundEnemyBase
{
    public GameObject bulletPrefab;          // 敵弾のプレハブ
    public Transform firePoint;              // 弾を出す位置（子オブジェクトを指定）
    [SerializeField] public float fireInterval = 3f;          // 発射間隔
    public float detectionRange = 10f;

    private float timer = 0f;
    [SerializeField] private float fallSpeed = 5.0f;

    // 接地判定
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.05f); // 幅広で薄い
    [SerializeField] private LayerMask groundLayer;

    public float turnCooldown = 1f;     // 方向転換のクールタイム(秒)

    protected override void OnResetState()
    {
        base.OnResetState();
        timer = 0f;
    }

    protected override void CommonUpdate()
    {
        bool hasTarget = player != null;

        UpdateGroundMovement(
            0f,
            fallSpeed,
            groundCheck,
            groundCheckSize,
            groundLayer,
            false,
            hasTarget,
            turnCooldown);

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

    private void FixedUpdate()
    {
        ApplyGroundVelocityToRigidbody();
    }

    private void Fire()

    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        bulletScript.Initialize(facingDirection, null); // 敵弾はコールバック不要
    }
}
