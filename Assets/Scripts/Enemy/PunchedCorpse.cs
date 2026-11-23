using UnityEngine;

public class PunchedCorpse : MonoBehaviour
{
    Vector2 moveDir;
    float speed;
    float lifeTime;
    int damage;

    bool initialized = false;

    public void Init(Vector2 dir, float speed, float lifeTime, int damage)
    {
        this.moveDir = dir.normalized;
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.damage = damage;
        initialized = true;

        // 一定時間後に自動消滅
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!initialized) return;

        // シンプルに直線移動
        transform.Translate(moveDir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 生きている敵（Tag=="Enemy"）に当たったらダメージ
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                // ここは通常ダメージ扱い（チェーンパンチしないなら isPunch=false）
                enemy.TakeDamage(damage, moveDir, false);
            }

            Destroy(gameObject);
        }
        // 地形にぶつかったら消える（タグ名はプロジェクトに合わせて）
        else if (other.CompareTag("Ground"))// || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
