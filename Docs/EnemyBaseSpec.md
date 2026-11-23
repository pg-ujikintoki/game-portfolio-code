# 敵共通仕様（EnemyBase）

## 概要

- 通常敵・ボス共通の基盤は `EnemyBase` が担当する。
- HP、被ダメージ、撃破、スポナー通知、アイテムドロップは `EnemyBase` 側で処理する。被ダメージ SE と撃破 SE もここが持つ。詳細は [SE仕様](SeSpec.md)。
- ボス固有の無敵・点滅は `BossBase` が担当する。詳細は [ボス共通仕様](BossCommonSpec.md)。

## 継承構造

- `EnemyBase`
  - `GroundEnemyBase` / `AerialEnemyBase` / `WallEnemyBase`（通常敵）
    - `EnemyBokujin` / `EnemyCanon` / `EnemyTank`（地上）
    - `EnemyDrone` / `EnemyHelpDrone`（空中）
    - `EnemyCrawler`（壁は上下、床と天井は左右に往復）
      - `EnemyCrawlerCannon`（同じ移動に加え、一定間隔でプレイヤーへ弾を撃つ）
  - `BossBase`（ボス共通）
    - `Boss01`（`Stage01` に配置）
    - `BossOp`（クラスだけ残っている。プレハブは無い）

壁沿いの移動と `EnemyCrawler` / `EnemyCrawlerCannon` の詳細は [壁沿い敵仕様](WallEnemySpec.md)。

## アイテムドロップ

撃破時に、敵ごとに設定したテーブルから最大1個のアイテムをドロップする。

### 担当クラス

- `EnemyBase`
  - Inspector の `Item Drops`（`dropTable`）を保持する。
  - `Die()` 内で、敵破棄の前に `dropTable.TrySpawnAt(transform.position)` を呼ぶ。戻り値の生成物は、ボスのとき `StageManager.OnBossDefeated` に渡す。
- `EnemyDropTable` / `EnemyDropEntry`
  - エントリ配列と排他抽選、生成処理を担当する。

### 抽選ルール

- 排他抽選。1回の撃破でドロップするのは最大1個。
- 0〜100 の乱数を1回振り、エントリを上から順に確率加算する。
- 乱数が入った最初の1件だけを生成する。
- 合計が 100 未満なら、残りは「何も出ない」。
- 合計が 100 を超える場合は、先頭から累計 100 までが有効。以降のエントリは出ない。
- `Item Prefab` が未設定、`PickupItem` が階層内に無い、確率が 0 以下のエントリはスキップし、抽選枠を消費しない。

例: コイン 50%、回復 10% → コイン 50% / 回復 10% / 何も出ない 40%。

### 生成・出現

- 生成位置は敵の座標。回転は `identity`。
- 指定したプレハブ全体（ルート含む）を `Instantiate` する。
- 落下・接地・取得は既存の `PickupItem` に任せる。水平方向の散らしや跳ねは行わない。
- 距離デスポーン（`EnemySpawner.DespawnCurrentEnemy`）では抽選しない。撃破（`Die()`）時のみ。
- ボス部屋への侵入でフィールドを片付けるときも `Die()` は呼ばない。スポナーは停止し、既に落ちている `PickupItem` も消す。詳細は [ステージ進行仕様](StageFlowSpec.md)。
- ボス撃破、リスポーン後の再撃破も対象。毎回独立に抽選する。
- ボス（`isBoss`）の撃破そのものがステージクリアになる。ドロップの取得ではクリアしない。遷移は [ステージ進行仕様](StageFlowSpec.md) を参照。

### Inspector 設定

`EnemyBase`（ボス含む各敵プレハブ）の `Item Drops`

- `Size`
  - ドロップ候補の件数。
- `Item Prefab`
  - ドロップするプレハブ。
  - `PickupItem` はルートでも子でもよい。`Life_10` / `Coin_10` のように子がスクリプトを持つ構成も指定できる。
- `Drop Chance Percent`
  - 0〜100。上から加算する排他抽選の確率。

未設定（空テーブル）なら何もドロップしない。

### プレハブ側の前提

ドロップ先は [通貨・アイテムシステム](CurrencyAndItemSystem.md) の `PickupItem` プレハブを使う。

例:

- `Assets/Prefab/Items/Coin_10.prefab`
- `Assets/Prefab/Items/Life_10.prefab`
