using UnityEngine;

public class BitBullet : MonoBehaviour, IAttackStatsProvider
{
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private LayerMask hitLayers; // ぶつかったら消す対象（Enemyなど）

    [Header("Attack Stats")]
    [SerializeField] private AttackStatsData attackStatsData;
    [SerializeField] private AttackStats attackStatsOverride = new AttackStats(1, 1);

    private Vector2 vel;


    public void Launch(Vector2 dir, float speed)
    {
        vel = dir.normalized * speed;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += (Vector3)(vel * Time.deltaTime);
        if (vel.sqrMagnitude > 0.001f)
            transform.right = vel; // 見た目の向き
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayers) != 0)
        {
            // ヒット時の処理（ダメージなどは相手側スクリプトで）
            Destroy(gameObject);
        }
    }

    public AttackStats Stats => attackStatsData != null ? attackStatsData.ToAttackStats() : attackStatsOverride;
}

