using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBokujin : GroundEnemyBase
{
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float fallSpeed = 5.0f;



    // 接地判定
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.05f); // 幅広で薄い
    [SerializeField] private LayerMask groundLayer;

    public float turnCooldown = 1f;     // 方向転換のクールタイム(秒)

    protected override void CommonUpdate()
    {
        bool hasTarget = player != null;

        UpdateGroundMovement(
            moveSpeed,
            fallSpeed,
            groundCheck,
            groundCheckSize,
            groundLayer,
            hasTarget,
            hasTarget,
            turnCooldown);

        if (!hasTarget)
        {
            return;
        }
    }

    protected override void CommonFixedUpdate()
    {
        ApplyGroundVelocityToRigidbody();
    }
}



