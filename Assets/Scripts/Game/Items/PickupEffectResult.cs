using UnityEngine;

namespace Game.Items
{
    public enum PickupEffectStatus
    {
        None = 0,
        Applied = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct PickupEffectResult
    {
        public static readonly PickupEffectResult None = new PickupEffectResult(PickupEffectStatus.None, false);

        public PickupEffectResult(PickupEffectStatus status, bool shouldConsume)
        {
            Status = status;
            ShouldConsume = shouldConsume;
        }

        public PickupEffectStatus Status { get; }
        public bool ShouldConsume { get; }

        public PickupEffectResult WithConsume(bool consume)
        {
            return new PickupEffectResult(Status, consume);
        }
    }

    public readonly struct PickupExecutionContext
    {
        public PickupExecutionContext(GameObject collector, PlayerController playerController)
        {
            Collector = collector;
            PlayerController = playerController;
        }

        public GameObject Collector { get; }
        public PlayerController PlayerController { get; }
    }
}
