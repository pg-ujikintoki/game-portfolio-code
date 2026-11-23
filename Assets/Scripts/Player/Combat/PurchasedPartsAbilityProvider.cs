using Preparation;
using UnityEngine;

[DisallowMultipleComponent]
public class PurchasedPartsAbilityProvider : MonoBehaviour, IPlayerAbilityProvider
{
    [Header("Part IDs")]
    [Tooltip("このパーツを購入済みの場合、チャージショットを解放します。")]
    [SerializeField] private string chargeShotPartId = "W102";
    [Tooltip("この Equipment パーツが ON の場合、チャージガトリングを解放します。")]
    [SerializeField] private string chargeGatlingPartId = "W103";

    [Header("Demo Recording")]
    [Tooltip("デモ撮影用。true のとき、所持・装備に関係なくチャージショットを使えます。デバッグ機能とは独立です。撮影後は false に戻してください。")]
    [SerializeField] private bool forceUnlockChargeShotForDemo;

    public bool HasAbility(PlayerAbilityType abilityType)
    {
        return abilityType switch
        {
            PlayerAbilityType.ChargeShot => forceUnlockChargeShotForDemo || IsPartAbilityUnlocked(chargeShotPartId),
            PlayerAbilityType.ChargeGatling => IsPartAbilityUnlocked(chargeGatlingPartId),
            _ => false,
        };
    }

    private bool IsPartAbilityUnlocked(string partId)
    {
        if (string.IsNullOrWhiteSpace(partId))
        {
            return false;
        }

        var progress = ResolveProgressManager();
        if (progress == null)
        {
            return false;
        }

        if (!progress.HasPart(partId))
        {
            return false;
        }

        if (!progress.TryGetPart(partId, out var part) || part == null)
        {
            Debug.LogWarning($"[PurchasedPartsAbilityProvider] PartData '{partId}' was not found in IntermissionProgressManager catalog. Falling back to owned-state unlock.", this);
            return true;
        }

        return IsAbilityUnlockedByPartType(part);
    }

    private bool IsAbilityUnlockedByPartType(PartData part)
    {
        switch (part.GetEffectivePartType())
        {
            case PartType.Passive:
                return true;

            case PartType.Equipment:
                {
                    var runtimeStateManager = ResolveRuntimeStateManager();
                    return runtimeStateManager != null && runtimeStateManager.IsEquipmentEnabled(part.PartId);
                }

            case PartType.Active:
            default:
                return false;
        }
    }

    private static IntermissionProgressManager ResolveProgressManager()
    {
        var progress = IntermissionProgressManager.Instance;
        if (progress != null)
        {
            return progress;
        }

        return FindFirstObjectByType<IntermissionProgressManager>(FindObjectsInactive.Include);
    }

    private static PartRuntimeStateManager ResolveRuntimeStateManager()
    {
        var runtimeStateManager = PartRuntimeStateManager.Instance;
        if (runtimeStateManager != null)
        {
            return runtimeStateManager;
        }

        runtimeStateManager = FindFirstObjectByType<PartRuntimeStateManager>(FindObjectsInactive.Include);
        if (runtimeStateManager != null)
        {
            return runtimeStateManager;
        }

        var runtimeStateManagerObject = new GameObject("PartRuntimeStateManager");
        return runtimeStateManagerObject.AddComponent<PartRuntimeStateManager>();
    }
}
