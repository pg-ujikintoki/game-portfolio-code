using System.Collections;
using UnityEngine;

/// <summary>
/// ボス部屋のシャッター。触れると StageManager が上昇、プレイヤーの歩行、下降を行う。
/// </summary>
public class BossDoor : MonoBehaviour
{
    [SerializeField] private float riseDistance = 3f;
    [SerializeField] private float riseSpeed = 4f;
    [SerializeField] private float playerWalkDistance = 3f;

    private Rigidbody2D body;

    public float PlayerWalkDistance => playerWalkDistance;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public IEnumerator Raise()
    {
        yield return MoveVertical(riseDistance);
    }

    public IEnumerator Lower()
    {
        yield return MoveVertical(-riseDistance);
    }

    private IEnumerator MoveVertical(float distance)
    {
        Vector2 start = body != null ? body.position : (Vector2)transform.position;
        Vector2 end = start + Vector2.up * distance;
        if (riseSpeed <= 0f || Mathf.Approximately(distance, 0f))
        {
            SetPosition(end);
            yield break;
        }

        float duration = Mathf.Abs(distance) / riseSpeed;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetPosition(Vector2.Lerp(start, end, t));
            yield return null;
        }

        SetPosition(end);
    }

    private void SetPosition(Vector2 position)
    {
        if (body != null)
        {
            body.position = position;
            return;
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    private void OnValidate()
    {
        riseDistance = Mathf.Max(0f, riseDistance);
        riseSpeed = Mathf.Max(0f, riseSpeed);
        playerWalkDistance = Mathf.Max(0f, playerWalkDistance);
    }
}
