using UnityEngine;

public class ChargeProjectilePierceTarget : MonoBehaviour
{
    [Tooltip("チャージショットで撃破された際に弾を貫通させるかどうか。")]
    [SerializeField] private bool allowPierceOnDeath = true;

    public bool AllowPierceOnDeath => allowPierceOnDeath;
}
