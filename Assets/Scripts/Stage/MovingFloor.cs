using UnityEngine;

public class MovingFloor : MonoBehaviour
{
    public enum MoveDirection { Horizontal, Vertical }
    [Header("移動方向 (Horizontal:左右, Vertical:上下)")]
    public MoveDirection moveDirection = MoveDirection.Horizontal;

    [Header("移動距離 (中心からの最大距離)")]
    public float moveDistance = 3f;

    [Header("移動速度")]
    public float speed = 2f;

    private Vector3 startPos;
    private bool playerOnFloor = false;
    private Transform playerTransform;

    void Start()
    {
        startPos = transform.position;
    }

    void FixedUpdate()
    {
        float offset = Mathf.Sin(Time.time * speed) * moveDistance;
        Vector3 newPos = startPos;
        if (moveDirection == MoveDirection.Horizontal)
            newPos.x += offset;
        else
            newPos.y += offset;
        Vector3 delta = newPos - transform.position;
        transform.position = newPos;
        // 床の子オブジェクト方式なので、ここで座標を直接動かす必要はありません
    }
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var pc = collision.gameObject.GetComponent<PlayerController>();
            var playerRoot = pc != null ? pc.transform : collision.transform;
            var playerCollider = collision.collider.GetComponent<Collider2D>();
            var floorCollider = GetComponent<Collider2D>();
            if (playerCollider != null && floorCollider != null)
            {
                float playerBottom = playerCollider.bounds.min.y;
                float floorTop = floorCollider.bounds.max.y;
                float threshold = 0.05f; // 許容誤差
                bool isWallSliding = pc != null && pc.IsWallSliding;
                if (playerBottom > floorTop - threshold && !isWallSliding)
                {
                    if (!playerOnFloor)
                    {
                        playerRoot.SetParent(transform);
                    }
                    playerOnFloor = true;
                    playerTransform = playerRoot;
                }
                else
                {
                    if (playerOnFloor)
                    {
                        playerRoot.SetParent(null);
                    }
                    playerOnFloor = false;
                }
            }
        }
    }
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var pc = collision.gameObject.GetComponent<PlayerController>();
            var playerRoot = pc != null ? pc.transform : collision.transform;
            // 追加: 親・自身がアクティブな場合のみSetParent(null)を呼ぶ
            if (playerTransform != null && gameObject.activeInHierarchy && playerRoot.gameObject.activeInHierarchy)
            {
                playerRoot.SetParent(null);
            }
            playerOnFloor = false;
        }
    }

    // 床のY方向速度を返す（壁ずり落ち用）
    public float GetFloorVelocityY()
    {
        float offsetNow = Mathf.Sin(Time.time * speed) * moveDistance;
        float offsetPrev = Mathf.Sin((Time.time - Time.fixedDeltaTime) * speed) * moveDistance;
        if (moveDirection == MoveDirection.Horizontal)
        {
            return 0f;
        }
        else
        {
            return (offsetNow - offsetPrev) / Time.fixedDeltaTime;
        }
    }
}
