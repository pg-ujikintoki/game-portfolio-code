# ボス共通仕様（BossBase）

## 概要

- 全ボス共通の被ダメージ制御と演出は `BossBase` が担当する。
- 具体的には、無敵時間中のダメージ受理制御、点滅演出、リセット/Disable時の安全な状態復帰を一元化している。
- 各ボス固有行動（移動・攻撃・AI）は `BossBase` 継承クラス側で実装する。
- アイテムドロップは `EnemyBase` の `Item Drops` を使用する。詳細は [敵共通仕様](EnemyBaseSpec.md)。
- 被ダメージ SE は `EnemyBase` と共通。撃破 SE は `BossBase.bossDefeatSfx` が優先され、未設定なら通常敵の撃破 SE を使う。詳細は [SE仕様](SeSpec.md)。
- ステージクリアはボス撃破の瞬間に成立する。詳細は [ステージ進行仕様](StageFlowSpec.md)。
- ボスはステージ読み込み時に `BossSpawnPoint` へ非表示で生成される。`BossDoor` の侵入演出が終わったあと表示され、`BossBase` が有効になる。手順は同仕様の「ボス部屋への侵入」。

## 継承構造

- `EnemyBase`
  - `BossBase`（共通仕様）
    - `Boss01`（`Stage01` に配置しているボス）
    - `BossOp`（クラスは残っている。プレハブは無く、ステージには置いていない）

## ライフサイクル

- `Awake`
  - `notifyStageAsBoss` の値を `SetBossFlag(...)` に反映する。
- `CommonStart`
  - 点滅対象 `SpriteRenderer` をキャッシュし、表示状態を正規化する。
- `CommonUpdate`（sealed）
  - 無敵点滅の終了判定を更新し、`OnBossUpdate()` を呼ぶ。
- `CommonFixedUpdate`（sealed）
  - `OnBossFixedUpdate()` を呼ぶ。
- `OnResetState`
  - 無敵状態関連（時刻・コンボ基準・点滅コルーチン）をリセットし、`OnBossResetState()` を呼ぶ。
- `OnDisable`
  - 点滅コルーチンを停止し、スプライト可視状態を復帰する。

## ダメージ受理ルール

- `CanReceiveDamage(DamagePayload payload)` で以下を適用する。
  - `EnemyBase` 側判定に通らない場合は無効。
  - 無敵時間中は、`payload.ComboPoint <= lastDamageComboPoint` の追撃を無効化。
  - 無敵時間中でも `ComboPoint` が前回より高い攻撃は有効。
- ダメージ適用時（`OnDamageApplied`）に以下を更新する。
  - `lastDamageTime = Time.time`
  - `lastDamageComboPoint = payload.ComboPoint`
  - 点滅コルーチン開始

## 無敵点滅仕様

- 点滅間隔は `invincibilityBlinkInterval`（最小 0.02 秒にクランプ）。
- 点滅対象は `spriteHolder` が設定されていればその配下、未設定なら自身配下の `SpriteRenderer` 全て。
- コルーチン停止時は必ず全スプライトを可視状態へ戻す。

## Inspector パラメータ

- `notifyStageAsBoss`（default: true）
  - `true` の場合、撃破時に `EnemyBase` 経由で `StageManager.OnBossDefeated()` 対象になる。
  - `OnValidate` でも反映されるため、Editor上での変更が有効。
- `invincibilityDuration`
  - 被弾後の無敵時間（秒）。
- `invincibilityBlinkInterval`
  - 点滅切替間隔（秒）。
- `spriteHolder`
  - 点滅対象を限定したい場合に指定する親Transform。

## 実装ガイドライン（新規ボス作成時）

- 新規ボスクラスは `BossBase` を継承する。
- 固有ロジックは以下のフックで実装する。
  - `OnBossStart()`
  - `OnBossResetState()`
  - `OnBossUpdate()`
  - `OnBossFixedUpdate()`
- 同時に `movementModuleBehaviour` / `attackModuleBehaviour` を使う場合は二重制御に注意する。
  - 固有ロジックをクラス内で完結する場合、Modules は `None` 推奨。
