using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShield : MonoBehaviour
{
    public bool isBroken = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken) return;

        if (other.CompareTag("Bullet"))
        {
            // 弾を反射（方向を反転 or 特定演出）
            //ReflectBullet(other.gameObject);
        }
        else if (other.CompareTag("Slash"))
        {
            BreakShield();
        }
    }

    /*public void ReflectBullet(Bullet bulletScript)
    {
        Debug.Log("ReflectBullet");

        Vector2 normal = (transform.lossyScale.x >= 0) ? Vector2.left : Vector2.right;
        bulletScript.Reflect(normal);
    }*/

    public void ReflectBullet(GameObject bullet)
    {
        Debug.Log("ReflectBullet");
        Bullet bulletScript = bullet.GetComponent<Bullet>();

        var rb = bullet.GetComponent<Rigidbody2D>();
        Debug.Log($"Before reflect vel={rb.linearVelocity}");

        if (bulletScript != null)
        {
            Vector2 normal;
            // 左右の向きはローカルスケールXで判定
            if (transform.lossyScale.x >= 0)
            {
                normal = Vector2.left;
            }
            else
            {
                normal = Vector2.right;
            }
            bulletScript.Reflect(normal);
            // ★押し出し（盾の外側に出す）
            //bulletScript.transform.position = (Vector2)bulletScript.transform.position + normal * 0.08f;
            // 上に向かって45度反射（通常は左から来ると仮定して）
            //Vector2 fakeNormal = Vector2.left;
            // 左側に盾がある場合 //bulletScript.Reflect(fakeNormal);
        }
        Debug.Log($"After reflect vel={rb.linearVelocity}");
    }

    private void BreakShield()
    {
        foreach (var col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        var enemy = GetComponentInParent<EnemyBase>();
        if (enemy) enemy.OnShieldBroken();

        Destroy(gameObject);
    }

    public void TakeShieldDamage()
    {
        var enemy = GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            enemy.OnShieldBroken();
        }

        Destroy(gameObject);
    }
}
