using System.Collections.Generic;
using UnityEngine;

public class ChargeProjectile : PlayerProjectileBase, IProjectileDamageListener
{
    [Header("Collision")]
    [Tooltip("チャージ弾が消滅する地形レイヤー。未設定の場合は非トリガーの衝突で消滅します。")]
    [SerializeField] private LayerMask terrainLayers;
    [Tooltip("敵のシールドに命中した際にチャージ弾を消滅させるかどうか。")]
    [SerializeField] private bool destroyOnShieldHit = true;

    private readonly HashSet<EnemyBase> processedEnemies = new();
    private readonly Dictionary<EnemyBase, Collider2D> pendingHits = new();

    private int enemyHurtBoxLayer = -1;
    private int enemyBodyLayer = -1;
    private int enemyPhysicsLayer = -1;

    protected override void Awake()
    {
        base.Awake();
        enemyHurtBoxLayer = LayerMask.NameToLayer("EnemyHurtBox");
        enemyBodyLayer = LayerMask.NameToLayer("Enemy");
        enemyPhysicsLayer = LayerMask.NameToLayer("EnemyPhysics");
    }

    protected override void HandleTriggerEnter(Collider2D other)
    {
        if (!other)
        {
            return;
        }

        if (IsShield(other))
        {
            DestroySelf();
            return;
        }

        if (!other.isTrigger)
        {
            if (IsEnemyBody(other))
            {
                return;
            }

            if (IsTerrain(other))
            {
                DestroySelf();
                return;
            }

            DestroySelf();
            return;
        }

        if (IsTerrain(other))
        {
            DestroySelf();
            return;
        }

        if (IsEnemyHurtBox(other))
        {
            var enemy = other.GetComponentInParent<EnemyBase>();
            if (!enemy)
            {
                DestroySelf();
                return;
            }

            if (processedEnemies.Contains(enemy))
            {
                return;
            }

            processedEnemies.Add(enemy);
            pendingHits[enemy] = other;
            return;
        }
    }

    private bool IsShield(Collider2D other)
    {
        if (!destroyOnShieldHit)
        {
            return false;
        }

        if (other.GetComponent<EnemyShield>() != null)
        {
            return true;
        }

        return other.GetComponentInParent<EnemyShield>() != null;
    }

    private bool IsTerrain(Collider2D other)
    {
        if (terrainLayers == 0)
        {
            return false;
        }

        int layer = other.gameObject.layer;
        return (terrainLayers.value & (1 << layer)) != 0;
    }

    private bool IsEnemyHurtBox(Collider2D other)
    {
        if (enemyHurtBoxLayer >= 0)
        {
            return other.gameObject.layer == enemyHurtBoxLayer;
        }

        return LayerMask.LayerToName(other.gameObject.layer) == "EnemyHurtBox";
    }

    private bool IsEnemyBody(Collider2D other)
    {
        int layer = other.gameObject.layer;
        if (layer == enemyBodyLayer || layer == enemyPhysicsLayer)
        {
            return true;
        }

        string layerName = LayerMask.LayerToName(layer);
        return layerName == "Enemy" || layerName == "EnemyPhysics";
    }

    public void OnDamageApplied(EnemyBase enemy, DamageResult result, Collider2D hitCollider)
    {
        if (!enemy)
        {
            return;
        }

        Collider2D storedCollider;
        if (pendingHits.TryGetValue(enemy, out var pendingCollider))
        {
            storedCollider = pendingCollider ? pendingCollider : hitCollider;
            pendingHits.Remove(enemy);
        }
        else
        {
            storedCollider = hitCollider;
            if (!processedEnemies.Contains(enemy))
            {
                processedEnemies.Add(enemy);
            }
        }

        if (!result.DamageApplied)
        {
            DestroySelf();
            return;
        }

        if (result.WasDestroyed && IsPierceTarget(storedCollider))
        {
            IgnoreEnemyColliders(enemy);
            return;
        }

        DestroySelf();
    }

    private void IgnoreEnemyColliders(EnemyBase enemy)
    {
        if (!enemy)
        {
            return;
        }

        foreach (var col in enemy.GetComponentsInChildren<Collider2D>(true))
        {
            if (!col)
            {
                continue;
            }

            if (!col.enabled)
            {
                continue;
            }

            if (!col.isTrigger && (IsEnemyBody(col) || col.gameObject.layer == enemyHurtBoxLayer))
            {
                Physics2D.IgnoreCollision(CachedCollider, col, true);
            }
        }
    }

    private bool IsPierceTarget(Collider2D collider)
    {
        if (!collider)
        {
            return true;
        }

        if (collider.TryGetComponent<ChargeProjectilePierceTarget>(out var target))
        {
            return target.AllowPierceOnDeath;
        }

        var parent = collider.GetComponentInParent<ChargeProjectilePierceTarget>();
        if (parent != null)
        {
            return parent.AllowPierceOnDeath;
        }

        return true;
    }
}
