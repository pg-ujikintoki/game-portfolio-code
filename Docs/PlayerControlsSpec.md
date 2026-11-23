# プレイヤー操作・行動仕様

プレイ中の操作と、それに対応するプレイヤー行動の現状仕様。数値はコード／プレハブの初期値。Inspector で上書きできるものはその旨を書く。

関連ドキュメント:

- [ポーズシステム](PauseSystem.md)
- [ヘルプ仕様](HelpSpec.md)
- [ステージ進行仕様](StageFlowSpec.md)
- [チャージショット仕様](ChargeShotSpec.md)
- [ビット攻撃仕様](BitAttackSpec.md)
- [通貨・アイテムシステム](CurrencyAndItemSystem.md)

## 概要

- 2D 横スクロール。重力は Unity 物理ではなく、`PlayerController` が毎フレーム `velocity` を組み立て `Rigidbody2D.linearVelocity` に渡す。
- 入力は `PlayerControls`（Input System）→ `PlayerInputRouter` → `PlayerController` / `PlayerAttackController` / `PlayerChargeController`。
- メイン／サブ武器は `Loadout` で持ち、ポーズメニューから切り替える。初期値は Main = Gun、Sub = Sword。

## 操作一覧

`Assets/PlayerControls.inputactions` の `Player` / `System` マップ。

| 行動 | キーボード | ゲームパッド | Input Action |
|---|---|---|---|
| 左右移動 | ← / → | 左スティック / 十字 | `Player.Move` |
| ジャンプ | Space | South（A / ×） | `Player.Jump` |
| ダッシュ | Right Shift | LB（Left Shoulder） | `Player.Dash` |
| メイン攻撃 | Z | West（X / □） | `Player.FireMain` |
| サブ攻撃 | X | RB（Right Shoulder） | `Player.FireSub` |
| ポーズ | 1 | Start | `System.Pause` |
| ヘルプ | 2 / B | East（B / ○） | `System.Help` |

- 上下入力（↑ / ↓、スティック Y）は歩行には使わない。ビットでは上入力だけが敵の真上からの攻撃になり、下入力は無入力と同じ。
- キーボードの B はヘルプを開く。開いているヘルプは、ページが残っていても B で閉じる。2 は次のページへ進み、最終ページで閉じる。

## 移動

担当: `PlayerController` / `PlayerController.Locomotion.cs`

### 歩行

- 接地・非ダッシュ時、`Move.x * walkSpeed`（初期 5）を横速度にする。
- 接地かつ `|Move.x| > 0.01` のときだけ歩きアニメ（`isWalking`）。空中の左右入力では立てない。向き反転は空中でも行う。

### ダッシュ

- **開始条件**: 接地中、非ダッシュ、ダッシュ押下。
- 横速度は `dashSpeed * 向き`（初期 10）。開始時に SE を鳴らす。割り当ては [SE仕様](SeSpec.md)。
- **地上での終了**: 次のいずれか。
  - `maxDashDuration`（初期 0.5 秒）経過
  - ダッシュボタンを離す
  - このフレームで左右の向きが変わった
- 終了時、接地していれば横速度を 0 にする。
- 空中ではダッシュを**新規開始できない**。地上で開始したダッシュが空中に持ち込まれた場合は継続し、空中では左右入力で向きと速度を切り替えられる。
- 空中で壁に触れているとダッシュは解除される（壁蹴り直後の横ロック中は解除しない）。

### ジャンプ

- **開始**: 接地中にジャンプ押下。上昇中は接地判定を無視する（`coyoteTime` = 0.1 秒）。
- ジャンプボタン押し続けで最大 `maxJumpTime`（初期 0.5 秒）まで `jumpPower`（初期 5）で上昇。
- ボタンを離す、または最大時間を超えると上昇をやめ、落下へ移る。
- 通常落下速度は `fallSpeed`（初期 5）。壁滑り中は後述の値に置き換わる。

### 接地

- `groundCheck` から下へ BoxCast し、`groundLayer` のうち法線が上向きの面があれば接地。壁側面が先に当たっても、同時に床があれば接地する。
- ジャンプ中、およびジャンプ直後の無視時間中は接地しない。

## 壁

担当: `PlayerWallMotor`

地形は Ground レイヤーを使う。左右からの BoxCast が当たった面の法線で側面かどうかを見る（`normal.x` の絶対値が大きい＝壁）。床の上面は `normal.y` が大きいので、ジャンプ中の左右入力だけでは壁ずり落ちにならない。

接地中は壁扱いにしない。足場の角に着地したときは床を優先する。

### 壁滑り

次をすべて満たすと落下を遅くする。

- 空中
- ジャンプ上昇中ではない
- 左右いずれかの壁に触れている
- 縦速度が 0 以下（下降中）
- 触れている壁側へ入力している

落下速度は `wallSlideFallSpeed`（初期 3）。縦に動く床（`MovingFloor`）に触れている場合は、その縦速度を加算する。

壁ずり落ち中は Animator の `isWallSliding` と `isJumpDowning` を true にする。歩き／上昇／ダッシュは false。着地したら空中フラグをすべて落とし、Idle（または Walk）へ戻す。遷移は Unity の Animator ウィンドウで組む。

壁ずり落ち中は入力で壁側を向くため、`bulletSpawnPoint` と `slashSpawnPoint` のローカル X を反転する。出した攻撃 Prefab も同様に左右反転する（斬撃は出現位置まわりのオフセットと向き、弾・パンチ・チャージ弾は見た目の向き）。進行方向も壁と反対側になる。

`PlayerController.IsWallSliding` は移動床がプレイヤーを子にするかの判定にも使う。壁滑り中は床の子にしない。

### 壁蹴りジャンプ

- **開始**: 空中かつ壁接触中にジャンプ押下。接地ジャンプより優先。
- 壁と逆方向へ飛び、向きもそちらへ変える。
- 横初速は `walkSpeed * 1.2`。ダッシュボタン押しっぱなしならさらに 1.5 倍。
- 上昇中もダッシュ押しっぱなしかつ壁蹴り中なら、縦速度を 1.5 倍する。
- 直後 `wallKickLockDuration`（初期 0.1 秒）は歩行・ダッシュで横速度を上書きしない。
- 着地、ノックバック、落下救済開始／終了で壁蹴り中フラグを落とす。

回数制限やクールダウンによる再キック禁止は未実装。空中で壁に触れれば、ロック解除後は再度蹴れる。

## 攻撃

担当: `PlayerAttackController`、チャージは `PlayerChargeController`

Z がメイン、X がサブ。押下で `Loadout` の対応スロットの武器を撃つ。

| 武器 | 行動 |
|---|---|
| Gun | 向き方向へ弾を撃つ。クールダウン 0.2 秒。同時存在は最大 3。チャージ対応。 |
| Sword | 3 段斬撃コンボ。各段 0.3 / 0.3 / 0.4 秒。攻撃中に再入力すると次段を予約。3 段目のヒットボックスは 0.1 秒遅れ。 |
| Punch | 正面にパンチヒットボックス。クールダウン 0.5 秒。 |
| Bit | 周回中のビットを 1 体出撃させ、敵へ 1 発撃って戻す。上入力は敵の真上から。それ以外は攻撃時の敵の高さへ移動し、横にまっすぐ撃つ。詳細は [ビット攻撃仕様](BitAttackSpec.md)。 |

- チャージショット（Gun、能力解放時）: 通常弾を撃ったまま押し続けるとチャージ。完了後に離すとチャージ弾（またはチャージガトリング）。詳細は [チャージショット仕様](ChargeShotSpec.md)。
- ロードアウト変更はポーズ中。Bit がメイン／サブのどちらにも無い場合、ポーズ解除後にビットを消す。

## 被ダメージ・体力

担当: `PlayerController.Hurt.cs`、`PlayerLife`

- 最大 HP 初期 100。0 で死亡。
- 敵本体の `EnemyContactDamager` 接触、または Tag `EnemyBullet` の弾でダメージ。敵弾は固定 1 ダメージで、ヒットした弾は破棄する。
- ダメージ時:
  1. HP を減らす
  2. ダッシュ解除
  3. 敵と逆方向へノックバック（距離 0.1、時間 0.5 秒）。この間は操作不可
  4. 1.0 秒の無敵。スプライト点滅。`Enemy` / `EnemyPhysics` レイヤーとの衝突を無視
- 無敵終了時、まだ敵に重なっていれば続けて接触ダメージを1回入れる。
- HP 0: 操作停止、攻撃停止、Game Over 表示、5 秒後にシーン再読み込み。被ダメージ SE は鳴らず、撃破 SE だけが鳴る。音の割り当ては [SE仕様](SeSpec.md)。
- 回復アイテムは `PlayerLife.Heal`。詳細は [通貨・アイテムシステム](CurrencyAndItemSystem.md)。

## 落下救済

担当: `PlayerLife.OnFallDeath`、`PlayerController.FallRescue.cs`

発動条件:

- `FallDeathZone` に入る
- または `GlobalFallDeathChecker` のワールド下限（初期 Y = -50）より下

死亡していなければ 1 秒待って救済開始。

1. **急上昇**（0.3 秒、速度 18）: 左右不可。地面に触れたら次へ。
2. **浮遊**: 左右は歩行の 0.7 倍。ジャンプ押しで上昇（強さ 5）、離すとゆっくり落下（速度 2）。
3. **終了**: 開始から 5 秒、または接地して 0.2 秒経過。

救済中は通常のダッシュ・ジャンプ・重力は動かない。

## 操作が効かないとき

| 状態 | 内容 |
|---|---|
| ポーズ | `PauseState.IsPaused`。移動・物理・攻撃イベント停止。ジャンプ経過時間などの内部状態は凍結し、解除後に再開する。ポーズ中にジャンプを離した場合は解除後に上昇を終了する。 |
| ステージ演出 | `isControlLockedStage`。READY 表示では速度 0。ボス部屋への侵入中は移動・攻撃・チャージを止める。ドアが開いたあと、接地するまでその場で落下し、着地してから演出歩行のあいだだけ `walkSpeed` で右へ進む。手順はステージ仕様。 |
| ノックバック | `isControlLocked`。移動入力は無視。壁滑り判定は残る。 |
| 死亡 | `DisableControl` と入力／攻撃コンポーネントの無効化。 |
| 落下救済 | 通常移動の代わりに救済専用の移動。 |

`DisableControl` / `EnableControl` はステージ開始シーケンスや死亡処理からも呼ばれる。

## 担当クラス

| クラス | 役割 |
|---|---|
| `PlayerInputRouter` | 入力の読み取りと 1 フレームフラグ |
| `PlayerController` | 移動の進行、速度の適用、操作ロック |
| `PlayerController.Locomotion` | 歩行・ダッシュ・ジャンプ・接地 |
| `PlayerWallMotor` | 壁判定・壁滑り・壁蹴り |
| `PlayerController.Hurt` | 接触ダメージ・無敵・ノックバック |
| `PlayerController.FallRescue` | 落下救済 |
| `PlayerAttackController` | 武器発射 |
| `PlayerChargeController` | チャージの進行 |
| `ChargeEffectController` | スロットごとのチャージ演出 |
| `PlayerLife` | HP、死亡、落下救済の開始 |
| `Loadout` | メイン／サブ武器 |

## 編集する場所

キャラクターのコンポーネント、数値、見た目、チャージ演出は `Assets/Prefab/Player/Player.prefab` だけを変更する。

`ActionStageScene` の Player はこのプレハブのインスタンスで、上書きは位置と回転だけである。シーン上のインスタンスにコンポーネントを足したり、Inspector で数値を上書きしたりしない。上書きが残ると、プレハブを直してもシーン側が優先される。

出現位置はステージプレハブの `PlayerSpawnPoint`。`StageManager` がステージ読み込み時にプレイヤーをそこへ動かす。シーンに置いた座標は、スポーン地点が無いときの位置である。

チャージ演出の段階と割り当ては [チャージショット仕様](ChargeShotSpec.md) を参照。

## 主な初期値

`PlayerController` / `PlayerWallMotor`（プレハブ）:

- 歩行 5 / ダッシュ 10 / ダッシュ最大 0.5 秒
- ジャンプ力 5 / 可変ジャンプ 0.5 秒 / 落下 5
- 接地無視 0.1 秒
- 壁滑り落下 3 / 壁蹴り横倍率 1.2 / ダッシュ蹴り倍率 1.5 / 横ロック 0.1 秒
- ノックバック 0.1 / 0.5 秒、無敵 1.0 秒
- 落下救済: 全体 5 秒、急上昇 0.3 秒・速度 18、浮遊落下 2、左右 0.7 倍

## 現状の制限

- 空中からのダッシュ開始はできない。
- 壁蹴りの回数制限・再キック用クールダウンは無い。
- 壁蹴り追加の前段として移動処理は整理済み。回数制限などは未実装。
