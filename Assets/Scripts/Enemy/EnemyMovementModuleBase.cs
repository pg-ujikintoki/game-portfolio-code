using UnityEngine;

public abstract class EnemyMovementModuleBase : MonoBehaviour, IEnemyMovement
{
    protected EnemyBase Owner { get; private set; }

    public virtual void Initialize(EnemyBase owner)
    {
        Owner = owner;
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public virtual void FixedTick(float fixedDeltaTime)
    {
    }

    public virtual void ResetState()
    {
    }
}
