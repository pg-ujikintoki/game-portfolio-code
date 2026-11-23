using UnityEngine;

public class SlashHitbox : MonoBehaviour, IAttackStatsProvider
{
    public float lifeTime = 0.5f; // 一瞬で消える

    [Header("Attack Stats")]
    [SerializeField] private AttackStatsData attackStatsData;
    [SerializeField] private AttackStats attackStatsOverride = new AttackStats(3, 2);


    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
        }
    }

    public AttackStats Stats => attackStatsData != null ? attackStatsData.ToAttackStats() : attackStatsOverride;

}

