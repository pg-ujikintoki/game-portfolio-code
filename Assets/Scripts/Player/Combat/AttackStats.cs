using UnityEngine;

[System.Serializable]
public struct AttackStats
{
    [SerializeField, Min(0)] private int damage;
    [SerializeField, Min(0)] private int comboPoint;

    public AttackStats(int damage, int comboPoint)
    {
        this.damage = Mathf.Max(0, damage);
        this.comboPoint = Mathf.Max(0, comboPoint);
    }

    public int Damage => damage;
    public int ComboPoint => comboPoint;
}

public interface IAttackStatsProvider
{
    AttackStats Stats { get; }
}
