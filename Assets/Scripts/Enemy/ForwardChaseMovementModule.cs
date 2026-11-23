using UnityEngine;

public class ForwardChaseMovementModule : EnemyMovementModuleBase
{
    private static readonly Vector2 DefaultGroundCheckSize = new Vector2(0.5f, 0.05f);

    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = DefaultGroundCheckSize;
    [SerializeField] private LayerMask groundLayer = Physics2D.AllLayers;
    [SerializeField] private float horizontalSpeed = 1f;
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float turnCooldown = 0f;
    [SerializeField] private bool requireTargetToMove = true;

    private bool hasCustomGroundCheck;
    private bool hasCustomGroundSize;
    private bool hasCustomGroundLayer;
    private GroundEnemyBase groundEnemy;

    public override void Initialize(EnemyBase owner)
    {
        base.Initialize(owner);

        groundEnemy = owner as GroundEnemyBase;
        if (groundEnemy == null)
        {
            Debug.LogWarning($"{nameof(ForwardChaseMovementModule)} requires Owner to be a {nameof(GroundEnemyBase)}.", this);
        }
    }

    public override void Tick(float deltaTime)
    {
        if (groundEnemy == null)
        {
            return;
        }

        bool hasTarget = Owner != null && Owner.player != null;
        bool enableMovement = !requireTargetToMove || hasTarget;

        Transform checkTransform = hasCustomGroundCheck && groundCheck != null
            ? groundCheck
            : Owner != null
                ? Owner.transform
                : transform;

        Vector2 effectiveGroundSize = hasCustomGroundSize ? groundCheckSize : DefaultGroundCheckSize;

        LayerMask effectiveLayerMask = hasCustomGroundLayer && groundLayer.value != 0
            ? groundLayer
            : Physics2D.AllLayers;

        groundEnemy.UpdateGroundMovement(
            horizontalSpeed,
            fallSpeed,
            checkTransform,
            effectiveGroundSize,
            effectiveLayerMask,
            enableMovement,
            hasTarget,
            turnCooldown);
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        if (groundEnemy == null)
        {
            return;
        }

        groundEnemy.ApplyGroundVelocityToRigidbody();
    }

    public void ConfigureGroundParameters(Transform checkTransform, Vector2 size, LayerMask layer)
    {
        hasCustomGroundCheck = checkTransform != null;
        hasCustomGroundSize = size.sqrMagnitude > 0f;
        hasCustomGroundLayer = layer.value != 0;

        if (hasCustomGroundCheck)
        {
            groundCheck = checkTransform;
        }

        if (hasCustomGroundSize)
        {
            groundCheckSize = size;
        }
        else
        {
            groundCheckSize = DefaultGroundCheckSize;
        }

        groundLayer = hasCustomGroundLayer ? layer : Physics2D.AllLayers;
    }

    public void ConfigureMovementParameters(float speed, float fall, float cooldown)
    {
        horizontalSpeed = speed;
        fallSpeed = fall;
        turnCooldown = cooldown;
    }

    public void SetRequireTargetToMove(bool value)
    {
        requireTargetToMove = value;
    }
}
