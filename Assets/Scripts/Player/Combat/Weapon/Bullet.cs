using System;
using System.Collections;
using UnityEngine;

public class Bullet : PlayerProjectileBase
{
    private Action<Bullet> onDestroyed;
    public bool isReflected { get; private set; }

    public void Initialize(int direction, GameObject owner, Action<Bullet> destroyedCallback)
    {
        onDestroyed = destroyedCallback;
        base.Initialize(direction, owner, HandleDestroyed);
    }

    private void HandleDestroyed(PlayerProjectileBase projectile)
    {
        onDestroyed?.Invoke(this);
    }

    protected override bool ShouldIgnoreCollision(Collider2D other)
    {
        return isReflected;
    }

    protected override void HandleTriggerEnter(Collider2D other)
    {
        if (isReflected)
        {
            return;
        }

        var shield = other.GetComponent<EnemyShield>() ?? other.GetComponentInParent<EnemyShield>();
        if (shield != null)
        {
            shield.ReflectBullet(gameObject);
            return;
        }

        if (LayerMask.LayerToName(other.gameObject.layer) == "EnemyHurtBox")
        {
            DestroySelf();
        }
    }

    public void Reflect(Vector2 shieldNormal)
    {
        isReflected = true;

        if (CachedRigidbody != null)
        {
            Vector2 incoming = CachedRigidbody.linearVelocity.normalized;
            Vector2 reflected = Vector2.Reflect(incoming, shieldNormal).normalized;
            CachedRigidbody.linearVelocity = reflected * Speed;
        }

        StartCoroutine(DisableColliderBriefly());
    }

    private IEnumerator DisableColliderBriefly()
    {
        if (!CachedCollider)
        {
            yield break;
        }

        CachedCollider.enabled = false;
        yield return new WaitForFixedUpdate();
        CachedCollider.enabled = true;
    }
}


