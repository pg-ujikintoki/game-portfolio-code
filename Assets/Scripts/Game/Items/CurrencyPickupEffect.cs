using UnityEngine;

namespace Game.Items
{
    public class CurrencyPickupEffect : PickupEffectBase
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private bool fallbackToIntermissionManager = true;

        public override PickupEffectResult TryExecute(PickupExecutionContext context)
        {
            if (amount == 0)
            {
                return new PickupEffectResult(PickupEffectStatus.Skipped, false);
            }

            bool applied = TryAddToWallet(amount);
            if (!applied)
            {
                Debug.LogWarning($"[CurrencyPickupEffect] Failed to add currency. amount={amount}");
                return new PickupEffectResult(PickupEffectStatus.Failed, false);
            }

            return new PickupEffectResult(PickupEffectStatus.Applied, true);
        }

        private bool TryAddToWallet(int value)
        {
            var progress = GameProgressManager.Instance;
            if (progress != null)
            {
                return progress.TryAddCurrency(value);
            }

            if (!fallbackToIntermissionManager)
            {
                return false;
            }

            var intermission = Preparation.IntermissionProgressManager.Instance;
            if (intermission == null)
            {
                return false;
            }

            intermission.AddDiamonds(value);
            return true;
        }
    }
}
