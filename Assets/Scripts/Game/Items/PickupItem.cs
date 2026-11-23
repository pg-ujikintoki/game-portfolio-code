using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PickupItem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;

        [Header("Movement Settings")]
        [Tooltip("プレイヤーの落下速度に合わせるための値。PlayerController の fallSpeed と揃えることを推奨。")]
        [SerializeField] private float fallSpeed = 5f;
        [SerializeField] private bool autoSleepOnGround = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging;

        private readonly List<PickupEffectBase> effects = new List<PickupEffectBase>();

        private bool isCollected;
        private Action onCollected;
        private bool isGrounded;
        private Vector2 lastVelocity;
        private Vector2 lastLoggedPosition;
        private float positionLogTimer;
        private bool wasSleeping;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (body != null)
            {
                body.freezeRotation = true;
                body.gravityScale = 0f;
                body.angularVelocity = 0f;
                body.linearVelocity = Vector2.zero;
            }

            EnsureFallSpeed();
            CollectEffects();

            lastLoggedPosition = transform.position;
        }

        private void EnsureFallSpeed()
        {
            if (fallSpeed > 0.01f)
            {
                if (verboseLogging)
                {
                    Debug.Log($"[PickupItem] {name} fallSpeed preset to {fallSpeed}");
                }
                return;
            }

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null && player.fallSpeed > 0.01f)
            {
                fallSpeed = player.fallSpeed;
                if (verboseLogging)
                {
                    Debug.Log($"[PickupItem] {name} fallSpeed synced to Player ({fallSpeed})");
                }
                return;
            }

            fallSpeed = 5f;
            if (verboseLogging)
            {
                Debug.Log($"[PickupItem] {name} fallSpeed fallback to {fallSpeed}");
            }
        }

        private void OnEnable()
        {
            if (verboseLogging)
            {
                Debug.Log($"[PickupItem] {name} enabled bodyType={body?.bodyType} simulated={body?.simulated}");
            }
        }

        private void CollectEffects()
        {
            effects.Clear();
            GetComponents(effects);

            var childEffects = GetComponentsInChildren<PickupEffectBase>(true);
            foreach (var effect in childEffects)
            {
                if (!effects.Contains(effect))
                {
                    effects.Add(effect);
                }
            }
        }

        public void SetCollectedCallback(Action callback)
        {
            onCollected = callback;
        }

        public void NotifyTriggerEntered(Collider2D trigger, Collider2D other)
        {
            if (isCollected)
            {
                return;
            }

            var player = ResolvePlayer(other);
            if (player == null)
            {
                return;
            }

            var context = new PickupExecutionContext(player.gameObject, player);
            ExecuteEffects(context);
        }

        private PlayerController ResolvePlayer(Collider2D candidate)
        {
            if (candidate == null)
            {
                return null;
            }

            var controller = candidate.GetComponent<PlayerController>();
            if (controller != null)
            {
                return controller;
            }

            controller = candidate.GetComponentInParent<PlayerController>();
            return controller;
        }

        private void ExecuteEffects(PickupExecutionContext context)
        {
            if (effects.Count == 0)
            {
                ConsumePickup();
                return;
            }

            bool anyApplied = false;
            bool shouldConsume = false;

            foreach (var effect in effects)
            {
                if (effect == null || !effect.isActiveAndEnabled)
                {
                    continue;
                }

                var result = effect.TryExecute(context);
                switch (result.Status)
                {
                    case PickupEffectStatus.Applied:
                        anyApplied = true;
                        if (result.ShouldConsume)
                        {
                            shouldConsume = true;
                        }
                        break;
                    case PickupEffectStatus.Skipped:
                        break;
                    case PickupEffectStatus.Failed:
                        if (result.ShouldConsume)
                        {
                            shouldConsume = true;
                        }
                        break;
                }
            }

            if (shouldConsume || anyApplied)
            {
                ConsumePickup();
            }
        }

        private void ConsumePickup()
        {
            if (isCollected)
            {
                return;
            }

            isCollected = true;
            Action callback = onCollected;
            onCollected = null;
            callback?.Invoke();
            Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            bool nowSleeping = body.IsSleeping();

            if (isGrounded)
            {
                body.gravityScale = 0f;
                body.linearVelocity = Vector2.zero;

                if (autoSleepOnGround && !nowSleeping)
                {
                    body.Sleep();
                    nowSleeping = body.IsSleeping();
                }
            }
            else if (!PauseState.IsPaused)
            {
                if (nowSleeping)
                {
                    body.WakeUp();
                    nowSleeping = body.IsSleeping();
                }

                body.gravityScale = 0f;
                float deltaY = -Mathf.Abs(fallSpeed) * Time.fixedDeltaTime;
                transform.position += new Vector3(0f, deltaY, 0f);
                body.position = transform.position;
                body.linearVelocity = new Vector2(0f, deltaY / Time.fixedDeltaTime);
            }
            else
            {
                body.linearVelocity = Vector2.zero;
                body.gravityScale = 0f;
            }


            if (verboseLogging && wasSleeping != nowSleeping)
            {
                Debug.Log($"[PickupItem] {name} sleeping changed {wasSleeping} -> {nowSleeping} (grounded={isGrounded})");
            }
            wasSleeping = nowSleeping;

            if (verboseLogging && lastVelocity != body.linearVelocity)
            {
                Debug.Log($"[PickupItem] {name} grounded={isGrounded} velocity={body.linearVelocity} fallSpeed={fallSpeed}");
                lastVelocity = body.linearVelocity;
            }

            if (verboseLogging)
            {
                positionLogTimer += Time.fixedDeltaTime;
                if (positionLogTimer >= 0.5f)
                {
                    positionLogTimer = 0f;
                    var currentPos = (Vector2)transform.position;
                    Vector2 delta = currentPos - lastLoggedPosition;
                    Debug.Log($"[PickupItem] {name} pos={currentPos} delta={delta} bodyType={body.bodyType} simulated={body.simulated} sleeping={nowSleeping}");
                    lastLoggedPosition = currentPos;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (body == null)
            {
                return;
            }

            if (collision.collider != null && collision.collider.isTrigger)
            {
                return;
            }

            if (collision.contacts.Length > 0)
            {
                for (int i = 0; i < collision.contactCount; i++)
                {
                    var contact = collision.GetContact(i);
                    if (contact.normal.y > 0.25f)
                    {
                        isGrounded = true;
                        body.linearVelocity = Vector2.zero;
                        if (verboseLogging)
                        {
                            Debug.Log($"[PickupItem] {name} grounded by {collision.collider.name}");
                        }
                        break;
                    }
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (body == null)
            {
                return;
            }

            if (collision.collider != null && collision.collider.isTrigger)
            {
                return;
            }

            if (collision.contactCount > 0)
            {
                for (int i = 0; i < collision.contactCount; i++)
                {
                    var contact = collision.GetContact(i);
                    if (contact.normal.y > 0.25f)
                    {
                        if (!isGrounded && verboseLogging)
                        {
                            Debug.Log($"[PickupItem] {name} staying grounded by {collision.collider.name}");
                        }
                        isGrounded = true;
                        return;
                    }
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (body == null)
            {
                return;
            }

            if (collision.collider != null && collision.collider.isTrigger)
            {
                return;
            }

            isGrounded = false;
            if (verboseLogging)
            {
                Debug.Log($"[PickupItem] {name} left {collision.collider.name}");
            }
        }
    }
}
