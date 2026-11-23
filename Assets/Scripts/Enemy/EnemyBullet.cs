using System;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 2f;
    private int direction = 1;
    private Action<EnemyBullet> onDestroyed;

    public void Initialize(int direction, Action<EnemyBullet> onDestroyed)
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
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
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
