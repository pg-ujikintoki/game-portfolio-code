using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyContactDamager : MonoBehaviour
{
    [SerializeField] private EnemyBase owner;
    [SerializeField, Min(-1)] private int overrideDamage = -1;

    public EnemyBase Owner
    {
        get
        {
            if (owner == null)
            {
                owner = GetComponentInParent<EnemyBase>();
            }

            return owner;
        }
    }

    public int ContactDamage => overrideDamage >= 0 ? overrideDamage : Owner != null ? Owner.ContactDamage : 0;

    private void Reset()
    {
        owner = GetComponentInParent<EnemyBase>();
    }
}
