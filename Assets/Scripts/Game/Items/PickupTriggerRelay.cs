using UnityEngine;

namespace Game.Items
{
    [RequireComponent(typeof(Collider2D))]
    public class PickupTriggerRelay : MonoBehaviour
    {
        [SerializeField] private PickupItem pickup;

        private void Awake()
        {
            if (pickup == null)
            {
                pickup = GetComponentInParent<PickupItem>();
            }

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            pickup?.NotifyTriggerEntered(GetComponent<Collider2D>(), other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            pickup?.NotifyTriggerEntered(GetComponent<Collider2D>(), other);
        }
    }
}
