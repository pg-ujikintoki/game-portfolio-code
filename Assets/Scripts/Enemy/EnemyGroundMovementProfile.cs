using UnityEngine;

[CreateAssetMenu(
    fileName = "GroundMovementProfile",
    menuName = "Enemy/Ground Movement Profile",
    order = 0)]
public class EnemyGroundMovementProfile : ScriptableObject
{
    [Header("Movement")]
    [SerializeField] private float horizontalSpeed = 1f;
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float turnCooldown = 0f;

    public float HorizontalSpeed => horizontalSpeed;
    public float FallSpeed => fallSpeed;
    public float TurnCooldown => turnCooldown;
}
