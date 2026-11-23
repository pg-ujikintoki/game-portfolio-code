using UnityEngine;

public class FallDeathZone : MonoBehaviour
{
    // 将来、エフェクトや専用演出を変えたい場合にフラグ増やしてもOK
    [SerializeField] string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // プレイヤーの死亡 or リスポーン処理を呼ぶ
        var life = other.GetComponent<PlayerLife>(); // あなたの側のスクリプト名に合わせて
        if (life != null)
        {
            Debug.Log("FallDeathZone: Player fell");  // ← デバッグ用
            life.OnFallDeath();   // こういうメソッドを用意しておく
        }
        else
        {
            Debug.LogWarning("PlayerLife が見つからなかったので、とりあえずログだけ出す");
        }
    }
}
