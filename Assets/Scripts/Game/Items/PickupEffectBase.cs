using UnityEngine;

namespace Game.Items
{
    public abstract class PickupEffectBase : MonoBehaviour
    {
        public abstract PickupEffectResult TryExecute(PickupExecutionContext context);
    }
}
