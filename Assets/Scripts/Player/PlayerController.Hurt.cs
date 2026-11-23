using System.Collections;
using UnityEngine;

public partial class PlayerController
{
    /// <summary>物理接触の開始を接触ダメージとして処理する。</summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleContactSource(GetCollisionSource(collision));
    }

    /// <summary>物理接触の継続を接触ダメージとして処理する。</summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleContactSource(GetCollisionSource(collision));
    }

    /// <summary>物理接触の終了でダメージ源の登録を外す。</summary>
    private void OnCollisionExit2D(Collision2D collision)
    {
        UnregisterContactDamager(GetCollisionSource(collision));
    }

    /// <summary>トリガー侵入を敵弾または接触ダメージとして処理する。</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTriggerOverlap(other);
    }

    /// <summary>トリガー滞在を敵弾または接触ダメージとして処理する。</summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        HandleTriggerOverlap(other);
    }

    /// <summary>トリガー退出で接触ダメージ源の登録を外す。</summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.isTrigger || other.CompareTag(EnemyBulletTag))
        {
            return;
        }

        UnregisterContactDamager(other);
    }

    /// <summary>トリガーとの重なりを敵弾ヒットか接触ダメージかに振り分ける。</summary>
    private void HandleTriggerOverlap(Collider2D other)
    {
        if (!other.isTrigger)
        {
            return;
        }

        if (other.CompareTag(EnemyBulletTag))
        {
            HandleEnemyBulletHit(other);
            return;
        }

        HandleContactSource(other);
    }

    /// <summary>Collision2D からダメージ判定に使うコンポーネントを取る。</summary>
    private static Component GetCollisionSource(Collision2D collision)
    {
        return collision.collider != null ? collision.collider : collision.transform;
    }

    /// <summary>ノックバック中は操作を止め、横方向へ押し出す。</summary>
    private IEnumerator KnockbackCoroutine(float direction)
    {
        isControlLocked = true;
        isAnimatorKnockback = true;
        animator.SetTrigger(AnimKnockback);
        isJumping = false;
        wallMotor?.ClearKicking();
        isAnimatorJumpUping = false;

        float elapsed = 0f;
        float knockbackSpeed = knockbackDistance / knockbackDuration;

        while (elapsed < knockbackDuration)
        {
            velocity.x = knockbackSpeed * direction;
            velocity.y = 0f;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        velocity.x = 0f;
        isControlLocked = false;
        isAnimatorKnockback = false;
    }

    /// <summary>接触した相手がダメージ源なら登録し、ダメージ適用を試みる。</summary>
    private void HandleContactSource(Component source)
    {
        EnemyContactDamager damager = RegisterContactDamager(source);
        if (damager == null)
        {
            return;
        }

        TryApplyContactDamage(damager);
    }

    /// <summary>接触中の EnemyContactDamager を登録する。</summary>
    private EnemyContactDamager RegisterContactDamager(Component source)
    {
        EnemyContactDamager damager = FindContactDamager(source);
        if (damager == null)
        {
            return null;
        }

        activeContactDamagers.Add(damager);
        return damager;
    }

    /// <summary>接触が終わった EnemyContactDamager の登録を外す。</summary>
    private void UnregisterContactDamager(Component source)
    {
        EnemyContactDamager damager = FindContactDamager(source);
        if (damager != null)
        {
            activeContactDamagers.Remove(damager);
        }
    }

    /// <summary>コンポーネントまたは親から接触ダメージ源を探す。</summary>
    private static EnemyContactDamager FindContactDamager(Component source)
    {
        if (source == null)
        {
            return null;
        }

        return source.GetComponent<EnemyContactDamager>() ?? source.GetComponentInParent<EnemyContactDamager>();
    }

    /// <summary>無敵でなければ接触ダメージを適用する。</summary>
    private void TryApplyContactDamage(EnemyContactDamager damager)
    {
        if (damager == null || isInvincible)
        {
            return;
        }

        ApplyDamageFromDamager(damager);
    }

    /// <summary>接触ダメージ値を解決できたらノックバック付きでダメージを与える。</summary>
    private void ApplyDamageFromDamager(EnemyContactDamager damager)
    {
        if (!TryResolveContactDamage(damager, out int damage, out Vector3 enemyPosition))
        {
            return;
        }

        ApplyDamageWithKnockback(damage, enemyPosition);
    }

    /// <summary>接触ダメージの数値と発生位置を取り出す。</summary>
    private bool TryResolveContactDamage(EnemyContactDamager damager, out int damage, out Vector3 enemyPosition)
    {
        damage = 0;
        enemyPosition = transform.position;

        if (damager == null)
        {
            return false;
        }

        int resolvedDamage = Mathf.Max(0, damager.ContactDamage);
        if (resolvedDamage <= 0)
        {
            return false;
        }

        damage = resolvedDamage;
        enemyPosition = damager.transform.position;
        return true;
    }

    /// <summary>無敵終了後、まだ重なっている接触ダメージを1件適用する。</summary>
    private void TryApplyPendingContactDamage()
    {
        if (isInvincible || activeContactDamagers.Count == 0)
        {
            return;
        }

        contactDamagerBuffer.Clear();
        contactDamagerBuffer.AddRange(activeContactDamagers);

        foreach (var damager in contactDamagerBuffer)
        {
            if (damager == null || !damager.isActiveAndEnabled)
            {
                activeContactDamagers.Remove(damager);
                continue;
            }

            if (!IsStillOverlapping(damager))
            {
                activeContactDamagers.Remove(damager);
                continue;
            }

            ApplyDamageFromDamager(damager);
            break;
        }

        contactDamagerBuffer.Clear();
    }

    /// <summary>ダメージ源とプレイヤーの非トリガーコライダーが重なっているか判定する。</summary>
    private bool IsStillOverlapping(EnemyContactDamager damager)
    {
        if (damager == null)
        {
            return false;
        }

        Collider2D damagerCollider = damager.GetComponent<Collider2D>();
        if (damagerCollider == null || !damagerCollider.enabled)
        {
            return false;
        }

        if (playerColliders == null || playerColliders.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider2D playerCollider = playerColliders[i];
            if (playerCollider == null || !playerCollider.enabled || playerCollider.isTrigger)
            {
                continue;
            }

            ColliderDistance2D distance = damagerCollider.Distance(playerCollider);
            if (distance.isOverlapped)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>プレイヤー配下のコライダー一覧を取り直す。</summary>
    private void RefreshPlayerColliders()
    {
        playerColliders = GetComponentsInChildren<Collider2D>(true);
    }

    /// <summary>敵弾ヒットを処理し、弾を消す。</summary>
    private void HandleEnemyBulletHit(Collider2D bullet)
    {
        if (isInvincible)
        {
            return;
        }

        Vector3 bulletPosition = bullet.transform.position;
        ApplyDamageWithKnockback(DefaultEnemyContactDamage, bulletPosition);

        Destroy(bullet.gameObject);
    }

    /// <summary>ダメージを与え、ノックバックと無敵を開始する。</summary>
    private void ApplyDamageWithKnockback(int damage, Vector3 sourcePosition)
    {
        if (life == null || damage <= 0)
        {
            return;
        }

        life.TakeDamage(damage);

        isDashing = false;
        isAnimatorDashing = false;

        float direction = (transform.position.x - sourcePosition.x >= 0) ? 1f : -1f;
        StartCoroutine(KnockbackCoroutine(direction));

        isInvincible = true;
        invincibleTimer = InvincibleDuration;
        SetEnemyCollisionEnabled(false);
        StartInvincibilityBlink();
    }

    /// <summary>点滅対象の SpriteRenderer をキャッシュする。</summary>
    private void CacheSpriteRenderers()
    {
        if (spriteHolder != null)
        {
            spriteRenderers = spriteHolder.GetComponentsInChildren<SpriteRenderer>(true);
        }
        else
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }

    /// <summary>敵レイヤーとの衝突無視に使うレイヤー番号を覚える。</summary>
    private void InitializeCollisionLayers()
    {
        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyPhysicsLayer = LayerMask.NameToLayer("EnemyPhysics");
        isEnemyCollisionIgnored = false;
        SetEnemyCollisionEnabled(false);
    }

    /// <summary>無敵点滅コルーチンを開始する。</summary>
    private void StartInvincibilityBlink()
    {
        if (invincibilityBlinkCoroutine != null) return;

        invincibilityBlinkCoroutine = StartCoroutine(InvincibilityBlinkCoroutine());
    }

    /// <summary>無敵点滅を止め、描画と敵衝突を元に戻す。</summary>
    private void StopInvincibilityBlink()
    {
        if (invincibilityBlinkCoroutine != null)
        {
            StopCoroutine(invincibilityBlinkCoroutine);
            invincibilityBlinkCoroutine = null;
        }

        SetRenderersVisible(true);
        SetEnemyCollisionEnabled(true);
    }

    /// <summary>無敵中、一定間隔でスプライトの表示／非表示を切り替える。</summary>
    private IEnumerator InvincibilityBlinkCoroutine()
    {
        float interval = Mathf.Max(0.02f, invincibleBlinkInterval);
        WaitForSeconds wait = new WaitForSeconds(interval);
        bool isVisible = false;

        while (isInvincible)
        {
            isVisible = !isVisible;
            SetRenderersVisible(isVisible);
            yield return wait;
        }

        SetRenderersVisible(true);
        invincibilityBlinkCoroutine = null;
    }

    /// <summary>キャッシュしたスプライトの表示状態を切り替える。</summary>
    private void SetRenderersVisible(bool isVisible)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].enabled = isVisible;
        }
    }

    /// <summary>プレイヤーと敵レイヤーの衝突を有効／無効にする。</summary>
    private void SetEnemyCollisionEnabled(bool enabled)
    {
        if (playerLayer < 0)
        {
            return;
        }

        bool shouldIgnore = !enabled;
        if (isEnemyCollisionIgnored == shouldIgnore)
        {
            return;
        }

        if (enemyLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, shouldIgnore);
        }

        if (enemyPhysicsLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyPhysicsLayer, shouldIgnore);
        }

        isEnemyCollisionIgnored = shouldIgnore;
    }

    /// <summary>無敵タイマーを進め、終了時に残留接触ダメージを見る。</summary>
    private void UpdateInvincibility()
    {
        bool invincibilityWasActive = isInvincible;
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
                StopInvincibilityBlink();
            }
        }

        if (invincibilityWasActive && !isInvincible)
        {
            TryApplyPendingContactDamage();
        }
    }
}
