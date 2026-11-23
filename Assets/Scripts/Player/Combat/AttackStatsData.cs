using UnityEngine;

[CreateAssetMenu(menuName = "Combat/AttackStatsData", fileName = "AttackStatsData")]
public class AttackStatsData : ScriptableObject
{
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(0)] private int comboPoint = 1;

    public int Damage => damage;
    public int ComboPoint => comboPoint;

    public AttackStats ToAttackStats()
    {
        return new AttackStats(damage, comboPoint);
    }
}
