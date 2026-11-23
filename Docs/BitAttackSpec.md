# ビット攻撃仕様

Bit 武器の現状仕様。数値はプレハブの値。スクリプト初期値と違うものはその旨を書く。Inspector で上書きできる。

関連ドキュメント:

- [プレイヤー操作・行動仕様](PlayerControlsSpec.md)
- [ポーズシステム](PauseSystem.md)
- [敵共通仕様（EnemyBase）](EnemyBaseSpec.md)

## 概要

攻撃のたびに、プレイヤーの周囲を周回しているビットを 1 体だけ出撃させる。射位まで移動し、弾を 1 発撃ってから周回位置へ戻る。弾は発射瞬間の方向へ直進し、追尾しない。

出撃中のビットは次の攻撃に使わない。全員出撃中なら、その入力は何もしない。

## 担当とプレハブ

| 役割 | クラス | プレハブ |
|---|---|---|
| 攻撃入口・索敵・射撃モード | `PlayerAttackController` | `Assets/Prefab/Player/Player.prefab` |
| 生成・周回・1 体ずつの出撃 | `BitManager` | 同上（プレイヤーに付いている） |
| 個体の移動と発射 | `BitAgent` | `Assets/Prefab/Player/Weapons/Bit.prefab` |
| 弾 | `BitBullet` | `Assets/Prefab/Player/Weapons/BitBullet.prefab` |

`BitManager` の `anchor` が未設定のとき、ビットは `BitManager` の子として生成する。移動と照準はワールド座標を使う。ビット本体に Collider は無く、当たり判定は持たない。

## 流れ

1. Bit 武器の攻撃で `EnsurePopulation` を呼び、`startCount` に足りないビットを生成する。
2. `FindTarget` で一番近い敵を探す。いなければターゲットは null。
3. 移動入力から `ShotMode` を決める。向きは `FacingDirection`（壁ずり落ち中の反転込み）。
4. `CommandNext` が、周回中または帰還中のビットを 1 体選んで出撃させる。選ぶ順は循環。
5. ビットは射位へ移動する。移動中と発射待ちのあいだ、先端は敵の左右方向を向く。角度は変えない。
6. 射位に着いたあと `fireCooldown` 待ち、先端から 1 発撃つ。
7. 周回スロットへ戻り、着いたら再び出撃可能になる。

## 周回

`BitManager.Update` がプレイヤー位置を中心に、半径 `orbitRadius` の円を `orbitAngularSpeed`（度/秒）で回す。ビット数で 360 度を等分した位置が各スロット。

- 周回中は `orbitSnapSpeed` でスロットへ吸着する。帰還中の移動は `BitAgent` の Return だけが `returnSpeed` で行う。
- 位相の進行だけ `Time.unscaledDeltaTime` を使う。ポーズ中はスロット角度が進むが、吸着自体は `Time.deltaTime` なのでその場に留まる。
- 出撃中（Assault / Fire）はスロットへ動かず、スロット座標の更新だけ行う。

プレハブ値: ビット数 4、半径 0.8、角速度 90、初期位相 0。スクリプト初期値の半径は 1.2。

## 索敵

プレイヤー位置を中心に、半径 `targetSearchRadius`（プレハブ 12）の円で `OverlapCircleAll` する。

対象レイヤーは `EnemyHurtBox` と `Enemy`。`EnemyPhysics` は敵弾も載るため使わない。マスクに `EnemyHurtBox` が含まれていない場合、起動時にこの 2 レイヤーへ付け替える。

ヒットしたコライダーから親の `EnemyBase` を辿り、そのルートが一番近い敵をターゲットにする。`EnemyBase` が無いオブジェクトは無視する。

`Boss01` は `EnemyTank` のバリアントで、元の接触コライダーを名前・タグ・レイヤー 11（`EnemyHurtBox`）に上書きしている。元の HurtBox 子は削除済み。この上書きはバリアントの変更リストに入るため、プレハブ本文の `m_Name` には出てこない。索敵と被弾はこのレイヤー 11 のコライダーを使う。接触ダメージ用の `EnemyContact`（レイヤー 15）は別に追加されている。

## 射撃モード

移動入力の優先度は上だけ。下入力は無入力と同じ。左右も、敵がいるときは無入力と同じ横撃ちになる。閾値は 0.5。

| 入力 | ShotMode | 敵がいるときの射位 | 弾の方向 |
|---|---|---|---|
| 上 | TopDown | 敵の真上、距離 `approachDistance` | ビット位置から敵の中心 |
| なし / 左右 | Auto45 / Front / Back | 攻撃開始時の敵の高さ。横にはプレイヤー側へ `approachDistance` | 横方向へまっすぐ（Y 成分なし） |

敵がいないとき、弾は右固定にしない。

| ShotMode | 射位 | 弾の方向 |
|---|---|---|
| TopDown | プレイヤーの真上 | 向いている方向 |
| Front / Auto45 | 向いている方向へ `approachDistance` | 向いている方向 |
| Back | 向いている方向の逆 | 向いている方向の逆 |

上入力の射位は、出撃中に敵が生きていれば毎フレーム敵の真上へ追従する。無入力と左右は、高さを攻撃開始時の敵の Y で固定し、横位置だけ敵の X に追従する。並ぶ側も攻撃開始時のプレイヤー側で固定する。到達判定は `arriveEpsilon`（0.05）。

`SetForceTop` が true のときは敵位置を無視して真下へ撃つ。呼び元は無く、通常プレイでは使わない。

プレハブ値: 射位距離 3.6、発射待ち 0.1 秒、弾速 18。スクリプト初期値の射位距離は 2.4。出撃速度 14、帰投速度 16、周回吸着 12。

## 向き

見た目の先端は子 `bulletSpawnPoint` で、プレハブ初期位置はスプライト右端（ローカル X = 0.15）。細かい位置はこの子を動かして調整する。

出撃中（Assault）と発射待ち（Fire）は、回転を変えず `localScale.x` の符号だけを反転する。先端のワールド X が敵と逆の側なら反転し、敵のいる左右を向く。上下には傾かない。

- 敵の X とビットの X が同じときは、攻撃開始時に決めた並び側の反対（敵がいる側）を向く。
- 敵がいないときは、その攻撃で撃つ横方向を向く。
- プレイヤー自身の左右反転は、先端と本体のワールド座標を比べて吸収する。

## 弾

弾は向いたあとの `bulletSpawnPoint` から出す。未設定ならビット本体の位置から出す。上入力の飛ぶ方向はビット本体から敵の中心で、横撃ちは敵のいる左右へ水平に飛ぶ。

`BitBullet` は `Rigidbody2D` を持たず、`Update` でワールド座標を進める。寿命は 2 秒。レイヤーは `PlayerBullet`、タグは `Bullet`。

ヒット対象は `EnemyHurtBox`。そのレイヤーに触れると弾を破棄する。ダメージは敵側の `EnemyBase` がタグ `Bullet` を受けて適用する。攻撃力は `AttackStatsData`、未設定なら `AttackStats` の上書き値（初期ダメージ 1、コンボポイント 1）。

HurtBox の Include と Exclude が両方 Default だと、どのレイヤーとも接触しない。Bokujin、Crawler、CrawlerCannon、Shielder の HurtBox はこの状態だったため、両方 0（レイヤー衝突行列に従う）に戻してある。`PlayerBullet` と `EnemyHurtBox` は衝突行列上、接触する。

## 生成と削除

- 攻撃のたびに `EnsurePopulation` する。足りなければ生成し、`startCount` を超えていれば余剰を破棄する。
- `Loadout` のメインとサブのどちらにも Bit が無いとき、`SetRemovalPending(true)` で削除を予約する。
- 実際の削除はポーズ解除時の `ApplyRemovalIfNeeded`。手順は [ポーズシステム](PauseSystem.md)。

## 既知の挙動

- `BitAgent` の `player` 参照は照準に使わない。プレイヤー位置は `BitManager.PlayerPosition`。
- 弾は直進のみ。発射後に敵が動いても曲がらない。
