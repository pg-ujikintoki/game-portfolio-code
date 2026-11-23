using UnityEngine;

public interface IProjectileDamageListener
{
    void OnDamageApplied(EnemyBase enemy, DamageResult result, Collider2D hitCollider);
}
