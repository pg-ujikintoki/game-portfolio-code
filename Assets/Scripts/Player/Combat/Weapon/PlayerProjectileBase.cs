using System;
using UnityEngine;

public abstract class PlayerProjectileBase : MonoBehaviour, IAttackStatsProvider
{
    [Header("Movement")]
    [Tooltip("弾速。プラス方向は右方向へ発射します。")]
    [SerializeField] private float speed = 20f;
    [Tooltip("弾が自動的に破棄されるまでの寿命（秒）。0 以下の場合は自動破棄されません。")]
    [SerializeField] private float lifeTime = 2f;


    [Header("Attack Stats")]
    [Tooltip("攻撃力を ScriptableObject から参照する場合に設定します。")]
    [SerializeField] private AttackStatsData attackStatsData;
    [Tooltip("AttackStatsData が未設定のときに使用するデフォルト値です。")]
    [SerializeField] private AttackStats attackStatsOverride = new AttackStats(1, 1);

    private int direction = 1;
    private GameObject owner;
    private Action<PlayerProjectileBase> onDestroyed;
    private bool isDestroyed;

    protected Rigidbody2D CachedRigidbody { get; private set; }
    protected Collider2D CachedCollider { get; private set; }

    protected virtual void Awake()
    {
        CachedCollider = GetComponent<Collider2D>();
        CachedRigidbody = GetComponent<Rigidbody2D>();
    }

    public void Initialize(int direction, GameObject owner, Action<PlayerProjectileBase> destroyedCallback)
    {
        this.direction = direction >= 0 ? 1 : -1;
        this.owner = owner;
        onDestroyed = destroyedCallback;

        if (owner != null)
        {
            IgnoreOwnerColliders(owner);
        }

        ApplyInitialVelocity();
        ScheduleDestruction();
    }

    protected virtual void ApplyInitialVelocity()
    {
        if (CachedRigidbody != null)
        {
            CachedRigidbody.linearVelocity = Vector2.right * direction * speed;
        }
    }

    private void ScheduleDestruction()
    {
        if (lifeTime <= 0f)
        {
            return;
        }

        Destroy(gameObject, lifeTime);
    }

    protected void DestroySelf()
    {
        if (isDestroyed)
        {
            return;
        }

        isDestroyed = true;
        onDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (isDestroyed)
        {
            return;
        }

        isDestroyed = true;
        onDestroyed?.Invoke(this);
    }

    protected virtual bool ShouldIgnoreCollision(Collider2D other)
    {
        return false;
    }

    protected abstract void HandleTriggerEnter(Collider2D other);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (ShouldIgnoreCollision(other))
        {
            return;
        }

        HandleTriggerEnter(other);
    }

    public void IgnoreOwnerColliders(GameObject ownerObject)
    {
        if (CachedCollider == null || ownerObject == null)
        {
            return;
        }

        foreach (var col in ownerObject.GetComponentsInChildren<Collider2D>())
        {
            Physics2D.IgnoreCollision(CachedCollider, col, true);
        }
    }

    public AttackStats Stats => attackStatsData != null ? attackStatsData.ToAttackStats() : attackStatsOverride;

    protected int Direction => direction;
    protected GameObject Owner => owner;
    protected float Speed => speed;
}
