using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTank : GroundEnemyBase
{
    public GameObject bulletPrefab;          // 敵弾のプレハブ
    public Transform firePoint;              // 弾を出す位置（子オブジェクトを指定）
    public float fireInterval = 3f;          // 発射間隔
    public float detectionRange = 10f;

    private float timer = 0f;
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float fallSpeed = 5.0f;

    private ForwardChaseMovementModule forwardMovementModule;


    // 接地判定
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.05f); // 幅広で薄い
    [SerializeField] private LayerMask groundLayer;

    public float turnCooldown = 1f;     // 方向転換のクールタイム(秒)

    protected override void Awake()
    {
        EnsureForwardMovementModule();
        base.Awake();
    }

    protected override void OnResetState()

    {
        base.OnResetState();
        timer = 0f;
    }

    protected override void CommonUpdate()
    {
        if (player == null)
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


    private void EnsureForwardMovementModule()
    {
        ForwardChaseMovementModule module = null;

        if (MovementModuleSource is ForwardChaseMovementModule assignedModule)
        {
            module = assignedModule;
        }
        else
        {
            module = GetComponent<ForwardChaseMovementModule>();
            if (module == null)
            {
                module = gameObject.AddComponent<ForwardChaseMovementModule>();
            }

            RegisterMovementModule(module);
        }

        forwardMovementModule = module;
        ConfigureForwardMovementModule();
    }

    private void ConfigureForwardMovementModule()
    {
        if (forwardMovementModule == null)
        {
            return;
        }

        forwardMovementModule.ConfigureGroundParameters(groundCheck, groundCheckSize, groundLayer);
        forwardMovementModule.ConfigureMovementParameters(moveSpeed, fallSpeed, turnCooldown);
        forwardMovementModule.SetRequireTargetToMove(true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        forwardMovementModule = GetComponent<ForwardChaseMovementModule>();
        ConfigureForwardMovementModule();
    }
#endif

    private void Fire()


    {

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        bulletScript.Initialize(facingDirection, null); // 敵弾はコールバック不要
    }
}
