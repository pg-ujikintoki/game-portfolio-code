using System;
using Game.Items;
using UnityEngine;

[Serializable]
public class EnemyDropEntry
{
    [Tooltip("PickupItem をルート、または子に持つプレハブを指定します。")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField, Range(0f, 100f)] private float dropChancePercent;

    public GameObject ItemPrefab => itemPrefab;
    public float DropChancePercent => dropChancePercent;

    public bool HasPickupItem()
    {
        return itemPrefab != null && itemPrefab.GetComponentInChildren<PickupItem>(true) != null;
    }
}

[Serializable]
public class EnemyDropTable
{
    [SerializeField] private EnemyDropEntry[] entries = Array.Empty<EnemyDropEntry>();

    public GameObject TrySpawnAt(Vector3 position)
    {
        GameObject prefab = RollPrefab();
        if (prefab == null)
        {
            return null;
        }

        return UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
    }

    public GameObject RollPrefab()
    {
        if (entries == null || entries.Length == 0)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, 100f);
        float cumulative = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            EnemyDropEntry entry = entries[i];
            if (entry == null || !entry.HasPickupItem())
            {
                continue;
            }

            float chance = Mathf.Clamp(entry.DropChancePercent, 0f, 100f);
            if (chance <= 0f)
            {
                continue;
            }

            float previous = cumulative;
            cumulative = Mathf.Min(100f, cumulative + chance);
            bool inRange = roll >= previous && (roll < cumulative || cumulative >= 100f);
            if (inRange)
            {
                return entry.ItemPrefab;
            }

            if (cumulative >= 100f)
            {
                return null;
            }
        }

        return null;
    }
}
