# ポーズ機能 実装概要（ActionStageScene）

## 1. 目的
- `ActionStageScene` でのプレイ中に一時停止し、武器選択（Main/Sub）を行えるようにする。
- ヘルプ表示機能ともコンテキストを共有し、ポーズメニューとヘルプウィンドウは同時に開けない。一方が表示中はもう一方への遷移を拒否する。ヘルプ自体の手順とデータは [ヘルプ仕様](HelpSpec.md)。

## 2. 全体構成

```
PauseManager（中核・状態管理）
├── PauseState（静的な一時停止フラグ / イベント）
├── PauseMenuCanvas（ポーズメニューUI本体）
│   └── PauseMenuUI（Main/Sub武器トグルの管理）
└── HelpWindowCanvas / HelpWindowController（ヘルプ表示）
```

## 3. 主なコンポーネント

### `PauseManager`（`Assets/Scripts/System/PauseManager.cs`）
- `SystemRoot` に配置され、シーン全体のポーズ状態を一元管理する。
- `PlayerControls`（Input System）の `System.Pause` / `System.Help` / UI `Cancel` を購読し、コンテキストを切り替える。
- 内部状態 `PauseContext { None, PauseMenu, Help }` を持ち、`SetContext()` で遷移を一本化。`PauseMenu` と `Help` の相互遷移は拒否する（ポーズ中はヘルプ不可、ヘルプ中はポーズ不可）。自動表示のヘルプもポーズ中は開かない。
- ボス部屋への侵入演出中、`StageManager` が `SetPauseInputLocked(true)` を呼ぶ。この間はポーズもヘルプも開かず、自動ヘルプも出ない。ボス出現後に解除する。手順は [ステージ進行仕様](StageFlowSpec.md)。
- ポーズ中とヘルプ表示中も、ステージ BGM とクリア SE は止めない。音の継続条件は [ステージBGM仕様](StageBgmSpec.md)。
- 遷移時に以下をまとめて制御:
  - `PauseState.Set(paused)` の更新
  - `Time.timeScale`（一時停止中は `0`、再開で `1`）
  - `PlayerInput.SwitchCurrentActionMap()`（一時停止中は `"UI"`、再開で `"Player"`）
  - `pauseMenuCanvas` / `helpWindowCanvas` の表示切替
  - 解除時に `BitManager.ApplyRemovalIfNeeded()` を呼び、ポーズ中に予約された Bit の削除を反映

### `PauseState`（`Assets/Scripts/System/PauseState.cs`）
- 静的クラス。`IsPaused` フラグと `OnChanged` イベントを提供。
- `Time.timeScale = 0` だけでは止まらない処理（入力受付、攻撃、物理演算など）を、各所で `PauseState.IsPaused` を参照して個別に止めるために使う。
- 主な参照箇所:
  - `PlayerController`（`Update`/`FixedUpdate` 内でポーズ中は早期リターン）
  - `PlayerInputRouter`（ポーズ中は移動・ジャンプ・ダッシュ入力を更新せず、解除時に実ボタン状態へ再同期する。`FireMain` / `FireSub` イベントは発火しない）
  - `PickupItem`（ポーズ中は落下処理を止める）
  - `PlayerChargeController`（`OnChanged` を購読し、解除時にチャージ入力状態を再評価）

### `PauseMenuUI`（`Assets/Scripts/System/PauseMenuUI.cs`）
- ポーズメニューを開いたときに、現在の `Loadout`（Main/Sub武器）をトグルグループに反映。
- 初期フォーカスを現在選択中のトグルに合わせる。
- トグル変更時に `Loadout.SetMain` / `Loadout.SetSub` を呼び出す。

### `WeaponOptionView`（`Assets/Scripts/System/WeaponOptionView.cs`）
- Main/Sub武器トグル1件分の表示。選択中フレーム（`SelectedFrame`）の表示に加え、`Update()` で現在フォーカス中かどうかを判定しラベル色を強調表示する。

### `BitManager`（`Assets/Scripts/Player/Combat/Weapon/Bit/BitManager.cs`）
- Bit武器の生成・削除を管理。攻撃そのものの仕様は [ビット攻撃仕様](BitAttackSpec.md)。
- `Loadout` 変更時に `SetRemovalPending()` で削除予約し、ポーズ解除時に `PauseManager` から `ApplyRemovalIfNeeded()` が呼ばれて実際に削除される。

## 4. キーボード/パッドナビゲーション

- Main武器行・Sub武器行はそれぞれ `UINavRow`（`Assets/Scripts/UI/UINavRow.cs`）で行内の左右移動・行間の上下移動を自動配線している。
  - `UINavRow.OnEnable()` のたびに `Wire()` を実行し、`items` の左右循環と `upRow` / `downRow` に応じた上下リンクを設定する。
  - 行は Main と Sub の 2 つ。Sub の下には続かない。

## 5. 既知の注意点

- Main武器行の一部トグル（例: `MainWeaponOption3`）には、プレハブインスタンスの上書きとして設計時に個別の `Explicit` ナビゲーションが設定されている箇所がある。行構成を変更する際は `UINavRow` の自動配線と競合しないか確認すること。

## 6. 関連スクリプト一覧

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/System/PauseManager.cs` | ポーズ全体の状態管理・入力受付・画面切替 |
| `Assets/Scripts/System/PauseState.cs` | 静的な一時停止フラグとイベント |
| `Assets/Scripts/System/PauseMenuUI.cs` | Main/Sub武器選択UI |
| `Assets/Scripts/System/WeaponOptionView.cs` | 武器トグル1件の表示（選択フレーム・色） |
| `Assets/Scripts/UI/UINavRow.cs` | 行単位のUIナビゲーション自動配線 |
| `Assets/Scripts/UI/ToggleSelectMakesOn.cs` | フォーカス時にトグルをONにする補助 |
| `Assets/Scripts/Player/Combat/Weapon/Bit/BitManager.cs` | ポーズ連動のBit武器削除予約・適用 |
| `Assets/Scripts/Player/Combat/PlayerChargeController.cs` | ポーズ連動のチャージ状態再評価 |
