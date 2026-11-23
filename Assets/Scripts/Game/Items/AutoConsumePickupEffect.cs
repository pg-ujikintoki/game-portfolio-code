using UnityEngine;

namespace Game.Items
{
    public class AutoConsumePickupEffect : PickupEffectBase
    {
        public override PickupEffectResult TryExecute(PickupExecutionContext context)
        {
            return new PickupEffectResult(PickupEffectStatus.Applied, true);
        }
    }
}
