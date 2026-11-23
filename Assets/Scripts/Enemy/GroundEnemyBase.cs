using UnityEngine;

public abstract class GroundEnemyBase : EnemyBase
{
    [SerializeField] private EnemyGroundMovementProfile groundMovementProfile;

    protected Vector2 velocity = Vector2.zero;
    protected int facingDirection = 1;
    protected float lastTurnTime = float.NegativeInfinity;

    protected override void CommonStart()
    {
        base.CommonStart();
        InitializeGroundRuntimeState();
    }

    protected override void OnResetState()
    {
        base.OnResetState();
        ResetGroundRuntimeState();
    }

    protected virtual void InitializeGroundRuntimeState()
    {
        isGrounded = false;
        lastTurnTime = float.NegativeInfinity;
        facingDirection = 1;
        velocity = Vector2.zero;
    }

    protected virtual void ResetGroundRuntimeState()
    {
        velocity = Vector2.zero;
        facingDirection = 1;
        lastTurnTime = float.NegativeInfinity;
    }

    protected internal EnemyGroundMovementProfile GroundMovementProfile => groundMovementProfile;

    protected internal void UpdateGroundMovement(
        float horizontalSpeed,
        float fallSpeedValue,
        Transform groundCheckTransform,
        Vector2 groundCheckDimensions,
        LayerMask groundLayerMask,
        bool enableHorizontalMovement,
        bool enableAutoTurn,
        float turnCooldown = 0f)
    {
        float resolvedHorizontalSpeed = groundMovementProfile != null
            ? groundMovementProfile.HorizontalSpeed
            : horizontalSpeed;
        float resolvedFallSpeed = groundMovementProfile != null
            ? groundMovementProfile.FallSpeed
            : fallSpeedValue;
        float resolvedTurnCooldown = groundMovementProfile != null
            ? groundMovementProfile.TurnCooldown
            : turnCooldown;

        UpdateStandardMovement(
            ref velocity,
            ref facingDirection,
            ref lastTurnTime,
            resolvedHorizontalSpeed,
            resolvedFallSpeed,
            groundCheckTransform,
            groundCheckDimensions,
            groundLayerMask,
            enableHorizontalMovement,
            enableAutoTurn,
            resolvedTurnCooldown);
    }

    protected internal void ApplyGroundVelocityToRigidbody()
    {
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }
}
