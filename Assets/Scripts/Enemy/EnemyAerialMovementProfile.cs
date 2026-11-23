using UnityEngine;

[CreateAssetMenu(
    fileName = "AerialMovementProfile",
    menuName = "Enemy/Aerial Movement Profile",
    order = 0)]
public class EnemyAerialMovementProfile : ScriptableObject
{
    [Header("Vertical Movement")]
    public EnemyBase.AerialVerticalMovementMode verticalMode = EnemyBase.AerialVerticalMovementMode.PingPong;
    public float moveRange = 0.5f;
    public float moveSpeed = 2f;

    [Header("Horizontal Follow")]
    public bool followPlayerHorizontally = false;
    public float horizontalSpeed = 0f;
    public float horizontalOffset = 0f;
}
