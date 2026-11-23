# 通貨管理・アイテム取得システム概要

## 1. 通貨管理

### 目的
- ゲーム内通貨をシーンをまたいでメモリ上に保持し、コイン取得から加算する。
- ファイルには保存しない。タイトルのニューゲームで `startingCurrency`（いまは 0）に戻す。

### 主なコンポーネント
- **`GameProgressManager`**
  - `DontDestroyOnLoad` でシーンをまたいでも存続する常駐マネージャ。
  - `CurrencyWallet`（`PlayerCurrencyWallet`）を内包し、通貨の加算／消費を管理。
  - `startingCurrency` を持ち、初期所持金をインスペクタで設定可能。
  - 公開メソッド：`TryAddCurrency(int)`, `TrySpendCurrency(int)`, `HasCurrency(int)` など。

- **`PlayerCurrencyWallet`**
  - 純C#クラス。残高管理、加算／消費、防御ロジック、不正入力防止、変更通知 (`OnCurrencyChanged`) を提供。
  - `GameProgressManager` がインスタンスを保持し、UIやアイテム等はマネージャを介して利用する。

- **`IntermissionProgressManager`**
  - 所持パーツの一覧を持つ。タイトルのニューゲーム時、シーンに無ければ空のインスタンスを作る。
  - `GameProgressManager` が無いときの通貨加算先として、ローカルの `PlayerCurrencyWallet` を持つ。通常のプレイでは通貨の本体は `GameProgressManager`。

### イベント通知
- `PlayerCurrencyWallet.OnCurrencyChanged` で残高変更を通知する。
- `IntermissionProgressManager` も `OnCurrencyChanged` と `OnDiamondsChanged` を持つ。
- 画面上の通貨表示は、いまの HUD には出していない。

### 設定手順
1. タイトルにある `GameProgressManager` の `Starting Currency` を設定する。
2. ニューゲームのたびに、この値が開始時の残高になる。

---

## 2. Item 共通基盤

### 目的
- プレイヤー接触で取得できるアイテムの物理挙動と取得ロジックを共通化。
- シーン間で転用しやすく、種類追加が容易な構造を実現する。

### 主なコンポーネント

#### `PickupItem`
- Rigidbody2D／Colliderを持つプレハブの共通ルートコンポーネント。
- プレイヤー接触検知、取得イベント、効果適用、二重取得防止、物理挙動（落下／接地）を担当。
- 特徴:
  - `body.gravityScale = 0` とし、`FixedUpdate` で手動落下（`fallSpeed`）を制御。
  - 床着地時には `Sleep()` で静止させる。ポーズ時やプレイヤー非操作時も停止。
  - `effects` リストにアタッチされた `PickupEffectBase` 派生コンポーネントを列挙し、順に実行。
  - 成功／失敗／スキップ結果に応じて自動消滅。
  - 消費時に `SetCollectedCallback` で登録した処理を1回呼ぶ。

#### `PickupTriggerRelay`
- 子オブジェクトの Trigger Collider にアタッチ。
- `OnTriggerEnter2D` 等で親 `PickupItem.NotifyTriggerEntered` を呼び出し、プレイヤー接触を伝達。

#### `PickupEffectBase`
- 抽象クラス。アイテム固有効果はこのクラスを継承したコンポーネントで実装する。
- `TryExecute(PickupExecutionContext context)` を実装し、`PickupEffectResult` を返す。

#### `PickupExecutionContext` / `PickupEffectResult`
- 取得者（プレイヤー `GameObject`／`PlayerController`）などの情報をカプセル化。
- 結果ステータス（`Applied` / `Skipped` / `Failed`）と「アイテムを消費するか」フラグを持つ。

### 物理挙動
- ルート：`Rigidbody2D`（Dynamic, Freeze Rotation Z）。
- 物理用Collider：`CapsuleCollider2D` など `isTrigger = false`。
- 取得用Collider：子オブジェクトに `CircleCollider2D`（`isTrigger = true`）＋ `PickupTriggerRelay`。
- レイヤー例：ルートを `Pickup`、Triggerを `PickupTrigger` として Physics 2D Layer Collision Matrix を調整（Groundとだけ衝突ON、Player/EnemyとはOFF）。

### 設定手順（共通）
1. 空のGameObjectに `PickupItem`、`Rigidbody2D`、物理用Colliderを設定。
2. 子に Trigger Collider を配置し `PickupTriggerRelay` をアタッチ。
3. ルートに必要な `PickupEffectBase` 派生コンポーネントを追加。
4. `fallSpeed`（Inspector）をプレイヤーと同じ（例: 5）に設定。

---

## 3. 具体アイテム

### 3.1 通貨アイテム

#### コンポーネント
- `CurrencyPickupEffect`（`PickupEffectBase`継承）
  - `amount`: 加算する通貨量（Inspectorで設定）。
  - `fallbackToIntermissionManager`: `GameProgressManager` 不在時のフォールバック有無。
  - 処理: `GameProgressManager.Instance.TryAddCurrency(amount)` 成功時に `Applied` + `ShouldConsume = true`。

#### Inspector 設定手順
1. 通貨アイテム用プレハブ（例: `Coin_10`）を複製。
2. ルートオブジェクトに `CurrencyPickupEffect` を追加。
3. `Amount` に加算量を設定（1 / 10 / 100 など）。
4. 必要に応じて外観 Sprite を差し替え。

配置されている通貨プレハブは `Assets/Prefab/Items/Coin_10.prefab`。HP を回復する取得アイテムは無い。`PlayerLife.Heal` は残っているが、拾う効果からは呼ばない。

---

## 4. Prefab 作成・シーン配置

### 基本ステップ
1. `Assets/Prefab/Items` 等にフォルダを用意し、共通ベースのPickupプレハブを複製。
2. 上記の要件に従って効果コンポーネントを追加し、値を設定。
3. シーン上にドラッグ＆ドロップして配置。
4. プレイモードで落下／接地／取得を確認。

### 物理・レイヤー設定（参考）
- `Pickup` レイヤー: `Ground` のみ衝突ON。`Player`／`Enemy`／`PlayerTrigger` 等はOFF。
- `PickupTrigger` レイヤー: `Player` / `PlayerTrigger` のみ衝突ON（Trigger判定）。

---

## 5. 動作確認チェックリスト

### 通貨アイテム
- [ ] プレイヤーと接触すると取得できる。
- [ ] 設定した通貨量が `GameProgressManager` の通貨に加算される。
- [ ] 取得後にアイテムが破棄される（再取得不可）。
- [ ] `Amount` を変更した別Prefabでも正しく加算される。
- [ ] シーン遷移後も加算額が保持される。
- [ ] タイトルでニューゲームすると、通貨が `startingCurrency` に戻る。

### 共通
- [ ] 落下／接地のあと、接触で取得できる。
- [ ] `PickupItem` にアイテム種別ごとの `switch` 文は存在しない。
- [ ] `PickupEffectBase` を継承したクラスを追加するだけで、新しいアイテム効果を実装できる。
