# Boss01 仕様

## 概要

- `Boss01` は `BossBase` を継承した地上型ボス。
- 共通の被弾無敵・点滅仕様は `BossBase` を利用し、行動AIのみ `Boss01` が持つ。
- 行動はランダム選択だが、同一行動の連続実行は行わない。

## 行動ステート

- 登場直後
  - `initialActionDelaySeconds`（default: 0.5）のあいだ行動を開始しない。
  - 待機中も重力/接地更新は継続する。
  - 経過後に `Idle` と同じく最初の行動を選択する。
- `Idle`
  - 次行動を選択し、行動開始時のプレイヤーX座標をスナップショットする。
- `Acting`
  - 現在行動を更新し、終了条件を満たすまで継続する。
- `Cooldown`
  - 行動完了後に `actionCooldownSeconds` 待機してから次行動へ遷移する。
  - 待機中も重力/接地更新は継続する（空中停止を防止）。

## 行動選択ルール

- 行動候補は以下の3種類。
  - `BurstShot`
  - `WalkToSnapshotX`
  - `JumpToSnapshotX`
- 直前行動 `lastAction` と同じ行動は候補から除外する。
- 有効候補内では等確率で選択する。

## 各行動仕様

### 1. BurstShot（3連射）

- 行動開始時に1回だけプレイヤー方向を向く。
- 行動中はその場停止（水平移動なし）。
- `burstShotInterval` 間隔で弾を3発発射する。
- 3発目発射時点で行動終了。
- `bulletPrefab` または `firePoint` が未設定の場合は、行動詰まり防止のため完了扱いにする。

### 2. WalkToSnapshotX（歩行移動）

- 行動開始時点のプレイヤーX座標（`actionStartPlayerX`）を目標に固定する。
- 目標X方向へ向きを固定して歩行する（途中でプレイヤー追尾の向き変更はしない）。
- `|targetX - currentX| <= arrivalToleranceX` で到達完了。

### 3. JumpToSnapshotX（ジャンプ移動）

- 行動開始時点のプレイヤーX座標を目標に固定する。
- 向きを目標X方向へ固定し、1回ジャンプで移動する。
- ジャンプ上昇は `jumpPower` と `maxJumpTime` で制御する。
- 水平速度は「目標距離 / 推定滞空時間」で算出し、`jumpHorizontalSpeedCap` で上限制限する。
- 一度地面を離れた後、再接地したフレームで行動完了とする。

## 推定滞空時間の計算

- `EstimateJumpAirTime()` で概算する。
- 上昇時間: `maxJumpTime`
- 頂点高近似: `apexHeight = jumpPower * maxJumpTime`
- 下降時間近似: `fallDuration = apexHeight / fallSpeed`
- 合計: `totalAirTime = maxJumpTime + fallDuration`
- 水平速度: `targetDistance / totalAirTime`

## Inspector パラメータ

- `bulletPrefab`
  - 発射する弾Prefab（`EnemyBullet` が必要）。
- `firePoint`
  - 弾の生成位置。
- `burstShotInterval`（default: 0.3）
  - 3連射時の発射間隔（秒）。
- `moveSpeed`（default: 1.0）
  - 地上移動速度。
- `fallSpeed`（default: 5.0）
  - 落下速度。
- `groundCheck`
  - 接地判定位置。
- `groundCheckSize`（default: 0.5, 0.05）
  - 接地判定サイズ。
- `groundLayer`
  - 接地判定対象レイヤー。
- `arrivalToleranceX`（default: 0.1）
  - 歩行行動の到達許容誤差。
- `initialActionDelaySeconds`（default: 0.5）
  - 登場してから最初の行動を開始するまでの待機時間。
- `actionCooldownSeconds`（default: 1.0）
  - 行動終了後の待機時間。
- `jumpPower`（default: 5.0）
  - ジャンプ上昇速度。
- `maxJumpTime`（default: 0.5）
  - 上昇継続時間。
- `jumpHorizontalSpeedCap`（default: 12.0）
  - ジャンプ中水平速度の上限。

## Modules 設定方針

- `Boss01` はクラス内で移動・攻撃・行動AIを完結しているため、`EnemyBase` の Modules は `None` 推奨。
- 追加で Modules を設定すると、同じ処理の二重実行が発生する可能性がある。

## 既知の調整ポイント

- 地形高低差や障害物が大きいステージでは、ジャンプ着地点が目標Xからずれる場合がある。
- その場合は次の順で調整する。
  - `jumpHorizontalSpeedCap`
  - `jumpPower`
  - `maxJumpTime`
  - `arrivalToleranceX`
