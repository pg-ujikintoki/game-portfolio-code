using UnityEngine;

public class PunchHitbox : MonoBehaviour, IAttackStatsProvider
{
    public float lifeTime = 0.5f; // 一瞬で消える

    [Header("Attack Stats")]
    [SerializeField] private AttackStatsData attackStatsData;
    [SerializeField] private AttackStats attackStatsOverride = new AttackStats(1, 3);


    // プレイヤー側でセットする攻撃方向（右向きなら (1,0)、左向きなら (-1,0) など）
    [HideInInspector] public Vector2 hitDirection = Vector2.right;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // ここで敵にダメージ処理など
        }
    }

    public AttackStats Stats => attackStatsData != null ? attackStatsData.ToAttackStats() : attackStatsOverride;

    public int Damage => Stats.Damage;

    public int ComboPoint => Stats.ComboPoint;

}

