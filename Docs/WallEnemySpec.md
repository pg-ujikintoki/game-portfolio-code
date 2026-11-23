# 壁沿い敵仕様

## 概要

- 床・壁・天井を伝う敵の共通移動は `WallEnemyBase` が担当する。
- `EnemyCrawler` は、垂直な壁では上下、床と天井では左右に往復する。
- `EnemyCrawlerCannon` は `EnemyCrawler` と同じ移動に、プレイヤーへの連射を足す。
- HP、被ダメージ、撃破、ドロップは [敵共通仕様](EnemyBaseSpec.md) の `EnemyBase` に従う。

## 継承

- `EnemyBase`
  - `WallEnemyBase`
    - `EnemyCrawler`
      - `EnemyCrawlerCannon`

## WallEnemyBase

床・壁・天井に張り付き、内側と外側の角を回る。見た目は足が面に付くよう回転し、進行方向へ左右反転する。

### 移動

- `Rigidbody2D.linearVelocity` で動く。重力は 0、回転は物理に任せない。
- 体の上方向を面の法線に合わせる。`Surface Offset` は体の中心から面までの距離で、コライダーの半径に合わせる。
- 進行方向の正面が別の面なら、その面へ乗り移る（内側の角）。
- 進行方向の面が尽きたら、外側の角を回って隣の面へ移る。
- 進める面が無いときは、`Crawl Mode` が `Patrol` なら折り返す。
- 左右反転は地上敵と同じで、正のスケールは左向きの絵を前提にする。
- 攻撃や射撃は `OnWallUpdate` で足す。

### Crawl Mode

- `ChasePlayer`（既定）
  - 今接している面に沿って、プレイヤーに近い側へ進む。
  - 角の向こうを回る最短経路は探さない。
  - プレイヤーがいないときは、その場に張り付いて進まない。
- `Patrol`
  - 向きを維持して進み、面が尽きたら反転する。
  - `Initial Direction` が開始方向。直立しているときの右が既定で、左も選べる。
  - 追跡中はプレイヤー側を優先し、この初期方向はプレイヤーがいないときの向きに使う。

### 地形

- `Surface Mask` が未設定なら `Ground` レイヤーを使う。
- 選択中のギズモは、足元方向が黄、進行方向が赤、外側の角が水色。

### Inspector

- `Crawl Mode`（default: `ChasePlayer`）
- `Initial Direction`（default: 右）
- `Move Speed`（default: 2）
- `Surface Offset`（default: 0.5）
- `Corner Engage Distance`（default: 0.3）
  - この距離まで次の面が近づいたら、内側の角として回り始める。
- `Surface Probe Distance`（default: 0.35）
  - 中心から面までの距離に加えて、面を探す余長。
- `Surface Mask`
  - 未設定なら `Ground`。

## EnemyCrawler

出現時に付いている面で往復する。壁と床・天井のあいだは移らない。

### 移動

- 法線の横成分が 0.7 以上なら垂直な壁。初期位置から `Patrol Distance` だけ上と下へ進む。
- 法線が上向き（縦成分が 0.7 以上）なら床。下向き（-0.7 以下）なら天井。上下反転した床も天井にあたる。初期位置から `Patrol Distance` だけ右と左へ進む。
- 壁にいるときは床や天井へ乗り移らない。床や天井にいるときは壁へ移らない。面が途切れたら折り返す。
- 範囲の端でも折り返す。プレイヤーの位置では進行方向を変えない。
- 最初の方向は `Start Direction`。既定は上。壁では上、床と天井では上を右、下を左として始める。
- ベースの `Crawl Mode` と `Initial Direction` は使わない。Inspector には出さない。
- 足が面に付く回転と、進行方向への左右反転はベースのまま。
- 選択中のギズモは、往復範囲をピンクの線で示す。壁では縦、床と天井では横。

### Inspector

- `Patrol Distance`（default: 2）
  - 壁では上下、床と天井では左右の距離。
- `Start Direction`（default: 上）
- 速度、体のオフセット、探査距離、地形マスクは `WallEnemyBase` の項目を使う。

## EnemyCrawlerCannon

移動は `EnemyCrawler` と同じ。一定間隔でプレイヤーへ `EnemyBullet` を撃つ。

### 発射

- `Fire Interval` ごとに連射を始める。間隔は連射の最初の弾から測る。
- 1 回の連射は `Bullets Per Shot` 発。最初の 1 発はすぐに出し、続きは `Burst Interval` ごとに出す。
- 連射が `Fire Interval` より長いときは、その連射が終わってから次を始める。
- 各弾は撃った時点のプレイヤー方向へ飛ぶ。弾の右方向が進行方向。
- 速度は `Bullet Speed` を弾へ渡す。既定は `EnemyBullet` の 20。
- 発射位置は `Fire Point`。未設定なら敵の位置。
- `Bullet Prefab` が未設定のときは撃たない。
- プレイヤーがいない間は、発射までのタイマーを進めない。

### Inspector

- `Bullet Prefab`
  - `EnemyBullet` を持つプレハブ。
- `Fire Point`
- `Fire Interval`（default: 3）
- `Bullets Per Shot`（default: 3）
- `Burst Interval`（default: 0.1）
- `Bullet Speed`（default: 20）
- 移動の項目は `EnemyCrawler` と同じ。
