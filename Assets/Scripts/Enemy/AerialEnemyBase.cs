using UnityEngine;

public abstract class AerialEnemyBase : EnemyBase
{
    [SerializeField] private EnemyAerialMovementProfile aerialMovementProfile;

    [SerializeField] private float moveRange = 0.5f;
    [SerializeField] private float moveSpeed = 2.0f;

    protected int facingDirection;
    protected float lastTurnTime = float.NegativeInfinity;
    protected float startY;

    protected override void CommonStart()
    {
        base.CommonStart();
        InitializeAerialRuntimeState();
    }

    protected override void OnResetState()
    {
        base.OnResetState();
        ResetAerialRuntimeState();
    }

    protected sealed override void CommonUpdate()
    {
        UpdateAerialLocomotion();
        OnAerialUpdate();
    }

    protected virtual void OnAerialUpdate()
    {
    }

    protected virtual void InitializeAerialRuntimeState()
    {
        CaptureSpawnAltitude();
        ResetFacingState();
        SnapFacingTowardsPlayer();
    }

    protected virtual void ResetAerialRuntimeState()
    {
        CaptureSpawnAltitude();
        ResetFacingState();
        SnapFacingTowardsPlayer();
    }

    protected internal EnemyAerialMovementProfile AerialMovementProfile => aerialMovementProfile;

    protected void UpdateAerialLocomotion()
    {
        if (player == null)
        {
            return;
        }

        UpdateFacingDirectionTowardsPlayer(ref facingDirection, ref lastTurnTime);

        EnemyAerialMovementProfile profile = aerialMovementProfile;
        float resolvedMoveRange = profile != null ? profile.moveRange : moveRange;
        float resolvedMoveSpeed = profile != null ? profile.moveSpeed : moveSpeed;
        AerialVerticalMovementMode resolvedVerticalMode = profile != null
            ? profile.verticalMode
            : AerialVerticalMovementMode.PingPong;
        bool followHorizontally = profile != null && profile.followPlayerHorizontally;
        float resolvedHorizontalSpeed = profile != null ? profile.horizontalSpeed : 0f;
        float resolvedHorizontalOffset = profile != null ? profile.horizontalOffset : 0f;

        UpdateAerialMovement(
            transform,
            startY,
            resolvedMoveRange,
            resolvedMoveSpeed,
            resolvedVerticalMode,
            followHorizontally,
            resolvedHorizontalSpeed,
            resolvedHorizontalOffset);
    }

    private void CaptureSpawnAltitude()
    {
        startY = transform.position.y;
    }

    private void ResetFacingState()
    {
        facingDirection = 0;
        lastTurnTime = float.NegativeInfinity;
    }

    private void SnapFacingTowardsPlayer()
    {
        UpdateFacingDirectionTowardsPlayer(ref facingDirection, ref lastTurnTime, forceApply: true);
    }
}
