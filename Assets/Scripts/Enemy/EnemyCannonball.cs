using System;
using UnityEngine;

public class EnemyCannonball : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 2f;
    private int direction = 1;
    public float fallSpeed = 4.0f; // 落下加速度
    private float verticalVelocity = 0f;
    private Action<EnemyCannonball> onDestroyed;

    public void Initialize(int direction, Action<EnemyCannonball> onDestroyed)
    {
        this.direction = direction;
        this.onDestroyed = onDestroyed;
        Destroy(gameObject, lifeTime);
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // 横方向の移動
        float horizontal = direction * speed * Time.deltaTime;

        // 下方向に加速（重力のような処理）
        verticalVelocity -= fallSpeed * Time.deltaTime;
        float vertical = verticalVelocity * Time.deltaTime;

        // 移動
        transform.Translate(new Vector2(horizontal, vertical));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            onDestroyed?.Invoke(this); // 呼び出し元に削除通知
            Destroy(gameObject);
        }
        if (other.CompareTag("Ground"))
        {
            onDestroyed?.Invoke(this); // 呼び出し元に削除通知
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        onDestroyed?.Invoke(this);
    }
}
