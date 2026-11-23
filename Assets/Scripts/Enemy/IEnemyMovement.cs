public interface IEnemyMovement
{
    void Initialize(EnemyBase owner);
    void Tick(float deltaTime);
    void FixedTick(float fixedDeltaTime);
    void ResetState();
}
