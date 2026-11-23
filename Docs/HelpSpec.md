# ヘルプ仕様

`ActionStageScene` で、ステージ上の範囲に入ったときだけ読めるヘルプ。数値と文面はスクリプトの初期値、および現在のシーンとプレハブの割り当て。Inspector で上書きできる。

対象はヘルプウィンドウ、それを出すトリガー、範囲に合わせて状態が変わる看板。`EnemyHelpDrone` は敵であり、このヘルプとは別。

関連ドキュメント:

- [ポーズシステム](PauseSystem.md)
- [プレイヤー操作・行動仕様](PlayerControlsSpec.md)
- [ステージ進行仕様](StageFlowSpec.md)
- [ステージBGM仕様](StageBgmSpec.md)
- [SE仕様](SeSpec.md)

## 概要

ヘルプはポーズと同じ一時停止状態で開く。`Time.timeScale` は 0、`PauseState.IsPaused` は true。ポーズメニューとヘルプウィンドウは同時に開けない。

再生の入口はステージプレハブ上の `HelpTrigger`。プレイヤーがそのトリガーに触れているあいだ、そのトリガーの `HelpSequence` が「今読めるヘルプ」になる。範囲の外ではヘルプキーを押しても開かない。

同じ範囲に入ると、その `HelpSequence` に紐づく看板があれば `Readable` になる。範囲を出ると `Idle` に戻る。手順は [看板](#看板)。

ヘルプ表示中も BGM は止めない。鳴り始めている SE も止めない。

## 構成

```
PauseManager（SystemRoot）
├── HelpWindowCanvas / HelpWindowController
└── HelpAvailableMark（画面上のマーク）

ステージプレハブ
├── HelpTrigger
│   └── HelpSequence（ScriptableObject）
└── HelpSignboard（任意。HelpSequence と 1 対 1）
```

| 役割 | クラス | 場所 |
|---|---|---|
| 開閉と一時停止 | `PauseManager` | `Assets/Scripts/System/PauseManager.cs`。`ActionStageScene` の `SystemRoot` |
| 範囲の検知 | `HelpTrigger` | `Assets/Scripts/Stage/HelpTrigger.cs` |
| 本文データ | `HelpSequence` / `HelpEntry` | `Assets/Scripts/UI/Help/HelpSequence.cs` |
| ウィンドウ表示 | `HelpWindowController` | `Assets/Scripts/UI/Help/HelpWindowController.cs`。`HelpCanvas` |
| 画面上のマーク | `HelpAvailableMark` | `Assets/Scripts/UI/Help/HelpAvailableMark.cs`。HUD の `Canvas` |
| ステージ上の看板 | `HelpSignboard` | `Assets/Scripts/Stage/HelpSignboard.cs`。プレハブは `Assets/Prefab/Stage/HelpSignboard.prefab` |

`HelpSequence` は Create メニューの `Help/Help Sequence` から作る。

## 操作

`PauseManager` は `PlayerInput` とは別に `PlayerControls` を持ち、有効なあいだ `System` マップを有効にしたままにする。ヘルプキーは、プレイ中の `Player` マップでも、ヘルプ中の `UI` マップでも届く。

| 操作 | キー | Input Action | 効果 |
|---|---|---|---|
| ヘルプ | 2 / ゲームパッド East（B / ○） | `System.Help` | 閉じていれば開く。開いていれば次のページ。最終ページなら閉じる |
| ヘルプ | B | `System.Help` | 閉じていれば開く。開いていれば、ページを残していてもその場で閉じる |
| キャンセル | Esc / ゲームパッド East（B / ○） | `UI.Cancel` | ページを残していても、その場で閉じる |

- キーボードの B は、開く条件は 2 と同じ。開いているときは次のページへ進まず、その場で閉じる。
- キャンセルは `PlayerInput` の `UI` マップ。ヘルプを開いたときにアクションマップが `UI` へ切り替わるので、開いているあいだだけ届く。
- ヘルプ中のポーズキーは無視する。ポーズメニュー中のヘルプキーも無視する。
- 読めるヘルプが無いときにヘルプキーを押すと、開かずにログを出して終わる。

## データ

`HelpSequence` は `HelpEntry` のリスト。

| フィールド | 内容 |
|---|---|
| `message` | 本文。空文字のエントリも 1 ページとして数える |
| `image` | そのページのキャラクター画像。未設定なら画像枠を出さない |
| `showOnlyOnce` | 一度開いたら、同じ周回では再表示しない。タイトルのニューゲームで忘れる。初期値はオフ。詳細は [一度だけ表示](#一度だけ表示) |

エントリが 1 件以上あるときだけ有効（`IsValid`）。0 件のアセットは登録しない。

表示は常に先頭ページから。`showOnlyOnce` がオフのあいだは読了を覚えない。再度開けばまた先頭になる。ページ番号の表示は無い。

## 一度だけ表示

設定場所はトリガーではなく `HelpSequence` アセットの `showOnlyOnce`。同じアセットを複数のトリガーが参照していると、どれか一つを開いた時点で、そのアセットを使う範囲はすべて読み済みになる。

オフのときは、範囲内で閉じてまだ中にいればマークが戻り、ヘルプキーで先頭ページから開ける。

オンのときは、`HelpWindowController.Show` が成功した時点で表示済みになる。最終ページまで読まなくても、開いただけで済む。キャンセルで閉じても同じ。

表示済みのあとは、同じ周回では次のようになる。

- 範囲に入っても `PauseManager` へ登録しない。ヘルプキーでは開かない。
- 開いた直後に範囲内へ残っていても、登録を外す。マークは戻らない。
- 範囲を出て入り直しても、`HelpAvailableMark` は出ない。
- 看板は表示済みでも範囲の出入りに従う。詳細は [看板](#看板)。
- 死亡で `ActionStageScene` を読み直しても、表示済みは残る。
- タイトルでニューゲームを始めると表示済みを消す。次にステージへ入ったとき、最初のヘルプももう一度開く。
- `ContinueCurrentSession` では消さない。
- アプリの起動時、エディタでは再生を始めたときにも消える。`GameSaveData` には入れない。

`autoShowOnEnter` と重なると、範囲に入った瞬間にウィンドウが開いて表示済みになる。そのヘルプでは、マークが出る隙はない。閉じたあとも、範囲内に残っていても、入り直してもマークは出ない。

## トリガー

`HelpTrigger` は `Collider2D` 必須。追加時の Reset で、そのコライダーはトリガーになる。現在のステージでは `BoxCollider2D`。

プレイヤー判定は、触れたコライダーの親に `PlayerController` があること。`pauseManager` が空なら、実行時にシーン内の `PauseManager` を探す。

| フィールド | 意味 |
|---|---|
| `sequence` | この範囲で読める `HelpSequence` |
| `autoShowOnEnter` | 入った瞬間にウィンドウを開く。初期値はオフ |
| `indicator` | 範囲の外にいるときだけ出す、ステージ上の目印。中に入ると消す |

入ったとき、有効で未表示の `sequence` を `PauseManager` へ登録する。`showOnlyOnce` で表示済みのアセットは登録しない。プレイヤーの当たり判定がすべて範囲を出たとき、登録元が自分なら登録を消す。そのときウィンドウが開いていれば閉じる。

`autoShowOnEnter` がオンでも、次のときは開かない。

- すでにポーズメニューが開いている
- ボス部屋の侵入演出でポーズ入力がロックされている

この場合も、未表示なら登録自体は行う。ロックやポーズが解けても、あとから自動では開かない。範囲内に残っていて、まだ表示済みでなければ、マークが出て、ヘルプキーで開ける。

複数のトリガーが重なったときの現状は次のとおり。

- あとから入ったトリガーが、今読めるヘルプを上書きする。ヘルプを開いたままなら、新しい本文の先頭ページに切り替わる。
- 出たときに解除されるのは、今の登録元だけ。上書きされた側から出ても、今のヘルプは残る。
- プレイヤーのコライダーが複数あると、最初の 1 つが入ったときだけ登録する。2 つ目以降の進入では登録し直さない。開いているヘルプを先頭ページへ戻さない。まだ触れているコライダーがあるあいだは解除しない。すべて出たときに解除する。本体の `BoxCollider2D` と、子の `DamageCollider` の両方が対象。

`indicator` は画面上のマークとは別。ステージ内のオブジェクトを、範囲の外でのみ出す。現在の `Stage01` では未割り当てなので、ステージ上の目印は出ていない。

範囲への出入りは、同じ `sequence` の看板にも伝える。ヘルプの登録とは別に行う。詳細は [看板](#看板)。

## 看板

`HelpSignboard` はステージ上のオブジェクト。参照するのは `HelpSequence` で、トリガーそのものではない。未設定の看板は `Idle` のまま。看板が無いシーケンスは、ヘルプウィンドウだけ動く。

プレハブは `Assets/Prefab/Stage/HelpSignboard.prefab`。`SpriteRenderer`、`Animator`、`HelpSignboard` を持つ。追加時の Require でも、この 2 コンポーネントが付く。プレハブ自体のシーケンスと Sprite は未設定。`Stage01` には `HelpSignboard01` から `HelpSignboard08` まで置いてあり、それぞれ同じ番号の `HelpSequence` を割り当てている。`HelpTrigger00` 用の看板は無い。

| フィールド | 意味 |
|---|---|
| `sequence` | 連動する `HelpSequence`。未設定なら登録せず、状態も変えない |
| `animator` | bool `Readable` で状態を切り替える。空なら同じオブジェクトの `Animator` を使う |

Animator は `Assets/Animator/Stage/HelpSignboard.controller`。更新モードは通常（スケール時間）。

| 状態 | 条件 | クリップ |
|---|---|---|
| `Idle` | 初期状態。`Readable` がオフ | 未割り当て |
| `Readable` | `Readable` がオン | 未割り当て |

遷移は両方向、Exit Time はオフ、遷移時間は 0。クリップを割り当てるまでは、パラメータが変わっても見た目は変わらない。`Readable` 側のクリップは Loop Time をオンにすると、範囲内で繰り返す。

範囲の出入りは `HelpTrigger` が知らせる。知らせる条件はヘルプの登録より前で、次のときも看板は範囲に従う。

- `sequence` にエントリが無く、`PauseManager` が登録をスキップする
- `showOnlyOnce` で表示済みのため、ヘルプキーでは開かない
- `PauseManager` がシーンに無い
- ポーズメニューが開いている
- ボス部屋の侵入演出でポーズ入力がロックされている

プレイヤーの重なりが 0 から 1 になったときに入り、1 から 0 になったときに出たとする。本体の `BoxCollider2D` と、子の `DamageCollider` の両方が対象。2 つ目以降のコライダーでは知らせ直さない。

同じ `HelpSequence` を複数のトリガーが参照しているときは、そのどれかに入っているあいだ `Readable` をオンにする。1 つから出ても、別のトリガーがまだ範囲内ならオンのまま。すべて出たら `Idle` に戻す。シーケンスが違う看板は、それぞれの範囲で別々に切り替わる。ヘルプウィンドウが後から入ったトリガーの本文に切り替わっても、先の看板は先の範囲に残っていれば `Readable` のまま。

トリガーが範囲内のまま無効化されたときは、そのトリガーの分だけ看板の範囲内を外す。`indicator` は範囲の外の表示に戻る。`PauseManager` に残っているヘルプ登録は、この無効化では消さない。

看板が後から有効になったときは、その時点の範囲に合わせる。すでに入っていれば `Readable`、入っていなければ `Idle`。無効化すると登録を外し、`Readable` をオフにする。

`indicator` とは別物。看板は範囲の内外で出したまま、状態だけ変える。

同じシーケンスを複数の看板が参照したときは、次の警告を出して、先に有効になった看板だけを動かす。

```
[HelpSignboard] HelpSequence '名前' は既に '先の看板' に紐づいています。先に有効な看板だけを動かします: 後の看板
```

先の看板が無効になると、次の看板が範囲の状態を引き継ぐ。登録と範囲内の記録は起動中だけ。アプリの起動時、エディタでは再生を始めたときに消える。`GameSaveData` には入れない。

タイトルとエンディングは、プレイヤーを置く前に `HelpTrigger` を無効にする。範囲には入らないので、Stage01 に看板を置いても `Idle` のまま。手順は [タイトル仕様（仮）](TitleSpec.md) と [エンディング仕様（仮）](EndingSpec.md)。

## ウィンドウ

`HelpCanvas` は初期状態で非表示。`PauseManager` と `HelpWindowController` も起動時に閉じる。シーンに残っている本文は、開くまでは出さない。開くときに表示し、`HelpWindowController.Show` で先頭ページを出す。閉じるときに本文と画像を消し、キャンバスも非表示に戻す。

キャンバスは Screen Space Overlay。スケールは画面サイズ合わせ、基準解像度 1280×720、Match 0.5。

```
HelpCanvas
└── DialogRoot（画面下の横並び）
    ├── CharacterPanel
    │   └── CharacterImage
    └── TextPanel
        └── DialogText
```

`DialogRoot` は下端に寄せた横並び。`CharacterPanel` は 200×200。画像が無いページでは `CharacterPanel` を隠し、`CharacterImage` も無効にする。本文は `DialogText`（TextMeshPro、サイズ 36）。

次のページは `ShowNext`。次が無ければ false を返し、`PauseManager` がヘルプを閉じる。

## 画面上のマーク

HUD の `Canvas` にある `HelpAvailableMark`。アンカーは下端中央。位置は下端から 660、大きさ 96×96、スケール 0.5。縦横比は維持し、クリックは受け取らない。見た目のオンオフは、同じオブジェクトの `Image.enabled`。

画像は `Assets/Images/Splite/ActionStageUI/ActionStageUI.png` の `ActionStageUI_2`。Sprite が空のあいだは出さない。

出る条件はすべて満たすとき。

- 有効なヘルプが登録されている（トリガーの中にいる）
- そのヘルプが `showOnlyOnce` の表示済みではない
- ポーズメニューもヘルプウィンドウも開いていない
- ポーズ入力がロックされていない
- `PauseManager` が有効

ヘルプを開いているあいだ、ポーズ中、トリガーの外、ボス部屋の侵入演出中は消える。`showOnlyOnce` がオフのヘルプを閉じてまだトリガーの中にいるときは、再び出る。`showOnlyOnce` がオンのヘルプは、開いたあとは範囲内に残っていても、入り直しても出ない。

## 開いているあいだのゲーム

ヘルプへ入るとき、ポーズメニューを開くときと同じ処理になる。

- `PauseState` を一時停止にする
- `Time.timeScale` を 0 にする
- `PlayerInput` のアクションマップを `UI` にする
- `HelpCanvas` を表示する

閉じるときはマップを `Player` に戻し、`timeScale` を 1 に戻す。ポーズ解除と同じく `BitManager.ApplyRemovalIfNeeded()` を呼ぶ。ヘルプ中に予約された Bit の削除は、閉じたときに反映される。

範囲内のままヘルプを開いているあいだ、看板の `Readable` はオンのまま残る。アニメは `timeScale` が 0 なので止まる。範囲を出るとヘルプは閉じ、看板は `Idle` に戻る。

ボス部屋の侵入演出は `StageManager` が `SetPauseInputLocked(true)` を呼ぶ。このあいだはヘルプを開かず、自動表示もマークも出ない。すでに開いていたヘルプは、ロックだけでは閉じない。ボス出現後にロックを解除する。手順は [ステージ進行仕様](StageFlowSpec.md)。

## 現在の割り当て

画像は、エントリがあるものすべて `Assets/Images/Splite/Doctor/Doctor.png`。

`HelpSignboard01` から `HelpSignboard08` は、同じ番号のトリガーと同じ `HelpSequence` で連動する。`HelpTrigger00` に対応する看板は無い。

### Stage01

`Assets/Prefab/Stage/Stages/Stage01/Stage01.prefab`。`indicator` はすべて未設定。

| トリガー | アセット | 入ったとき自動で開く | 一度だけ | 本文 |
|---|---|---|---|---|
| `HelpTrigger00` | `Stage01Help00` | する | する | シミュレーション開始！！ |
| `HelpTrigger01` | `Stage01Help01` | しない | しない | 段差はAボタンでジャンプして乗り越えて。 |
| `HelpTrigger02` | `Stage01Help02` | しない | しない | 敵よ！Xボタンで攻撃して！ |
| `HelpTrigger03` | `Stage01Help03` | しない | しない | この敵の攻撃はまっすぐしか飛ばないの。 / 段差を使って小さくジャンプしながら攻撃してみて。 |
| `HelpTrigger04` | `Stage01Help04` | しない | しない | 天井に貼り付いてる敵はビットを使うと攻撃しやすいよ。 |
| `HelpTrigger05` | `Stage01Help05` | しない | しない | ここはダッシュで勢いをつけて飛び越えて！ |
| `HelpTrigger06` | `Stage01Help06` | しない | しない | この下にはたくさんの敵がいるよ。チャージショットでまとめて倒して！ |
| `HelpTrigger07` | `Stage01Help07` | しない | しない | 壁に向かってジャンプを繰り返すと壁を登ることができるよ |
| `HelpTrigger08` | `Stage01Help08` | しない | しない | この先に強い敵がいるよ。気をつけて！ |

`HelpTrigger03` だけ 2 ページ。2 ページ目はヘルプキーでもう一度押したとき。`showOnlyOnce` はオフなので、閉じたあとも範囲内ならマークが戻り、先頭から読み直せる。

`HelpTrigger00` は入った瞬間に開く。開いた時点で表示済みになるので、この範囲では `HelpAvailableMark` は出ない。閉じたあと範囲内に残っていても、死亡でステージを読み直しても、その周回では出ない。タイトルからニューゲームすると、次の入場でまた開く。

## 画面上のマークの画像

`HelpAvailableMark` の Sprite は `ActionStageUI_2`。位置と大きさは HUD の `Canvas` 上の RectTransform で変える。Sprite を外すと、条件を満たしてもマークは出ない。
