using System.Collections.Generic;
using UnityEngine;

public class BitManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;        // プレイヤーのTransform
    public Vector3 PlayerPosition => (player ? player : transform).position;

    [Header("Bit Settings")]
    [SerializeField] private BitAgent bitPrefab;      // ビット本体
    [SerializeField] private Transform anchor;        // 追従親（未設定なら this）
    [SerializeField] private int startCount = 4;      // ★ 生成数（後で自由に変えられる）
    [SerializeField] private float orbitRadius = 1.2f;
    [SerializeField] private float orbitAngularSpeed = 90f; // deg/sec
    [SerializeField] private float orbitStartAngle = 0f;    // 配置の初期位相

    // ランタイム
    private readonly List<BitAgent> _agents = new();
    private float _angleDeg;
    private int _nextIndex;          // 次に飛ばすインデックス（循環）
    private bool _pendingRemoval;    // ポーズ解除時に全消去

    public bool HasBit => _agents.Count > 0;
    public BitAgent Current => null; // 互換用（不要だが参照残ってもビルド通るように）

    void Awake()
    {
        if (!player) player = transform;
        _angleDeg = orbitStartAngle;
    }

    void Update()
    {
        if (!HasBit) return;

        // 円運動の位相を進める
        _angleDeg += orbitAngularSpeed * Time.unscaledDeltaTime; // ポーズ無視なら unscaled
        var center = PlayerPosition;
        int n = _agents.Count;
        if (n == 0) return;

        // n等分した角度にスロットを配置
        float step = 360f / n;
        var parent = anchor ? anchor : transform;
        for (int i = 0; i < n; i++)
        {
            var a = _angleDeg + step * i;
            var rad = a * Mathf.Deg2Rad;
            var slot = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;

            // 存在するビットにスロットを配信
            var b = _agents[i];
            if (b) b.TickOrbitSlot(slot);
            else   RebuildAgentsList(); // 念のため欠損を掃除
        }
    }

    // --- 生成・破棄 ----------------------------------------------------------

    /// <summary>必要数のビットが居なければ生成する（何度呼んでもOK）</summary>
    public void EnsurePopulation(GameObject owner = null)
    {
        var parent = anchor ? anchor : transform;

        // 既に居る数
        int have = _agents.Count;
        for (int i = 0; i < have; i++)
            if (_agents[i] == null) { RebuildAgentsList(); break; }

        // 足りないぶんを生成
        while (_agents.Count < Mathf.Max(0, startCount))
        {
            var agent = Instantiate(bitPrefab, parent.position, Quaternion.identity, parent);
            agent.Initialize(this, _agents.Count); // index を渡す想定（あなたの BitAgent はこれでOK）
            _agents.Add(agent);
        }

        // 多すぎるなら余剰分を破棄（startCountを縮めたとき）
        while (_agents.Count > startCount)
        {
            var last = _agents[_agents.Count - 1];
            if (last) Destroy(last.gameObject);
            _agents.RemoveAt(_agents.Count - 1);
        }
    }

    public void DestroyAllBits()
    {
        foreach (var a in _agents)
            if (a) Destroy(a.gameObject);
        _agents.Clear();
        _nextIndex = 0;
    }

    void RebuildAgentsList()
    {
        _agents.RemoveAll(a => a == null);
        if (_nextIndex >= _agents.Count) _nextIndex = 0;
    }

    // --- 攻撃発艦（1体だけ） -------------------------------------------------

    /// <summary>利用可能なビットを1体だけ出撃させる。見つからなければ何もしない。</summary>
    public void CommandNext(Transform enemy, BitAgent.ShotMode mode, int facingSign)
    {
        if (!HasBit) return;

        int n = _agents.Count;
        for (int k = 0; k < n; k++)
        {
            int i = (_nextIndex + k) % n;
            var a = _agents[i];
            if (a && a.IsAvailable)
            {
                a.CommandAttack(enemy, mode, facingSign);
                _nextIndex = (i + 1) % n; // 次はその次の個体
                return;
            }
        }
        // すべて出撃中なら何もしない（次の入力まで待つ）
    }

    // --- ポーズ連動 ----------------------------------------------------------

    public void SetRemovalPending(bool pending) => _pendingRemoval = pending;

    public void ApplyRemovalIfNeeded()
    {
        if (_pendingRemoval) DestroyAllBits();
        _pendingRemoval = false;
    }
}
