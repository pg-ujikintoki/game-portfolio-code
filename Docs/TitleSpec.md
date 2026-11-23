# タイトル仕様（仮）

このドキュメントは仮仕様です。画面、曲、文言、メニューの見せ方は確定していません。いま動いている実装を、あとから差し替える前提で記録しています。

関連ドキュメント:

- [ステージ進行仕様](StageFlowSpec.md)
- [エンディング仕様（仮）](EndingSpec.md)
- [ステージBGM仕様](StageBgmSpec.md)
- [SE仕様](SeSpec.md)

## 対応範囲

- `TitleScene` の表示
- 背景のステージと、操作できないプレイヤー
- `TitleLogo` の七色の発光
- シーン開始時の BGM
- ニューゲーム開始時の SE
- 決定キーによるニューゲーム

オプション、実績、専用ロゴ画像は含みません。ステージを出たあとの流れは [ステージ進行仕様](StageFlowSpec.md)。

## シーン

`Assets/Scenes/TitleScene.unity`

ビルド設定では、有効なシーンの先頭です。続くシーンは `ActionStageScene`、`EndingScene`。

| 役割 | クラス | 場所 |
|---|---|---|
| メニューと BGM | `TitleManager` | `Assets/Scripts/Title/TitleManager.cs` |
| 背景のステージとプレイヤー | `TitleStagePresenter` | `Assets/Scripts/Title/TitleStagePresenter.cs` |
| ロゴの発光 | `TitleLogoRainbowGlow` | `Assets/Scripts/Title/TitleLogoRainbowGlow.cs` |
| ステージ進行 | `GameProgressManager` | タイトルに配置。`Awake` で `DontDestroyOnLoad` |

UI は `Canvas`（Screen Space Overlay）に載せる。入力は `EventSystem` ではなく、`TitleManager` が `PlayerControls` を直接読む。マウスのクリックでは開始しない。

## 入り方

1. ゲーム起動。有効シーンの先頭が `TitleScene`。
2. エンディングの待ち時間が終わったあと。手順は [エンディング仕様（仮）](EndingSpec.md)。

## 画面

`TitleStagePresenter` がシーン開始時に次を行う。エンディングと同じコンポーネントで、同じ見た目にしている。

1. `Stage01` プレハブを原点に生成する。
2. `Player` プレハブを `PlayerSpawnPoint` に置く。スポーン地点が無いときは `StageDefinition` の開始座標を使う。
3. プレイヤーの操作を止める。移動も攻撃もしない。
4. ステージ内のヘルプトリガーを無効にする。プレイヤーを置くより前なので範囲には入らない。ヘルプは開かず、看板があっても `Idle` のまま。
5. カメラを正射影サイズ 3.5 にし、画面全体が `LevelBounds` に収まる位置へ置く。アクションステージ開始時と同じ構図にする。

体力バーなどの戦闘 UI は出さない。敵も、この開始地点の画面には出さない。

タイトルの `Main Camera` には Pixel Perfect Camera が付いている。`TitleStagePresenter` はそれを無効にし、`orthographicSize`（初期値 3.5）で上書きする。ポストプロセスはオフ。ロゴの光は、カメラのブルームではなく文字マテリアルのグローです。

その上に、Canvas の文字を重ねる。

| オブジェクト | いまの状態 | 内容 |
|---|---|---|
| `TitleLogo` | 表示。画面中央上（Y 240） | 下の仮文言。フォントは LiberationSans SDF、サイズ 72 |
| `Credit` | 表示。画面下（Y -322） | 文字は `音楽：魔王魂`。フォントは BIZ UD ゴシック。対応する音源はポートフォリオ用に外してある |
| `NewGame` | 非表示 | 文字は `New Game`。色の対象には残るが、画面には出ない |

`TitleLogo` の仮文言:

```
Demo Version

Press X button to start
```

`NewGame` は非表示です。見た目の案内は `TitleLogo` の文言だけです。開始そのものは、下の決定入力で行います。

## ロゴの発光

`TitleLogo` の `TitleLogoRainbowGlow` が、文字全体を同じ色で光らせる。色は次の順で、7 秒で一周する。

1. 赤
2. 橙
3. 黄
4. 緑
5. 青
6. 藍
7. 紫

各色は枠の 55% を保ち、残りで次の色へ滑らかに移る。文字の外側には同じ色のにじみが出る。にじみの濃さは正弦波でゆれ、約 0.82 から 1.0 のあいだを往復する。待ちは実時間（`Time.unscaledTime`）なので、`Time.timeScale` が 0 でも発光は進む。

| 項目 | 内容 |
|---|---|
| マテリアル | `Assets/Materials/TitleLogoGlow.mat` |
| シェーダー | `TextMeshPro/Distance Field Overlay`。キーワード `GLOW_ON` |
| 一周 | `cycleDuration` 7 秒 |
| 内側の光 | `glowInner` 0.1 |
| 外側の光 | `glowOuter` 0.5 |
| 減衰 | `glowPower` 0.6 |
| 濃さ | `glowAlpha` 1 |

実行時に、文字のマテリアルインスタンスへ色を書き込む。光が文字のメッシュで切れないよう、`extraPadding` を有効にする。マテリアルにグローが無いときは、同じアトラスのまま Overlay シェーダーへ差し替える。差し替えできないときは発光をやめ、文字はそのまま残る。

この文言も色も仮です。専用ロゴは未定です。Inspector の `TitleLogoRainbowGlow` で一周の速さと光の広さを変えられます。

## BGM

| 項目 | 内容 |
|---|---|
| フィールド | `TitleManager.titleBgm` |
| 現在の割り当て | 未設定。再配布できない音源はポートフォリオ用に外している |
| 音量 | `bgmVolume` 0.45 |

`TitleManager.Start` が、自身のオブジェクトへ BGM 用 `AudioSource` を実行時に追加する。ループする。2D（`spatialBlend` 0）、`playOnAwake` はオフ。クリップが未設定のときは鳴らさない。フェードはしない。

`ActionStageScene` へ単発ロードすると、タイトルのオブジェクトごと破棄され、BGM も止まる。

クリップが無いので、タイトルでは BGM は鳴らない。Inspector の `titleBgm` で差し替えできます。

## ニューゲーム開始 SE

ニューゲームを決めたあと、`ActionStageScene` へ移る前に 1 回鳴らす。

| 項目 | 内容 |
|---|---|
| フィールド | `TitleManager.startGameSfx` |
| 現在の割り当て | 未設定。再配布できない音源はポートフォリオ用に外している |
| 音量 | `startGameSfxVolume` 0.9 |

`TitleManager` が、自身のオブジェクトへ SE 用 `AudioSource` を実行時に追加する。2D（`spatialBlend` 0）、ループしない、`playOnAwake` はオフ。クリップの長さだけ実時間で待ち、鳴り終わってから `ActionStageScene` を単発ロードする。`Time.timeScale` が 0 でもこの待ちは進む。

クリップが未設定のときは鳴らさず、待たずにロードする。BGM はシーンを出るまで止めない。Inspector の `startGameSfx` で差し替えできます。

## 操作

`TitleManager` が読むアクションは次のとおり。画面の文言は `Press X button to start` です。上下移動とキャンセルは読みません。

| 操作 | アクション | 現在の割り当て |
|---|---|---|
| 決定 | `UI.Submit` | `Enter`、Gamepad `buttonSouth` |
| 決定 | `Player.FireMain` | `Z`、Gamepad `buttonWest` |
| 決定 | `Player.FireSub` | `X`。同じアクションの RB では開始しない |

`Submit`、`FireMain`、キーボードの `X` は同じ決定として扱う。遷移を始めたあとは、追加の決定を捨てる。

マウスのクリックでは開始しない。

## ニューゲーム

決定が入ると、メニュー項目の選択はせず、そのまま開始する。

`startGameMode` の初期値は `NewGame`。

1. `GameProgressManager` の進行をリセットする。ステージ番号は 1、通貨は `startingCurrency`（いまは 0）に戻る。
2. 所持パーツを初期化する。`IntermissionProgressManager` が無ければ空のものを作る。シーンに初期所持が置いていないので、この経路では所持パーツは空になる。
3. 装備状態を空にする。`PartRuntimeStateManager` が無ければ作る。
4. 一度だけ表示のヘルプの表示済みを消す。次のステージでは最初のヘルプももう一度開く。
5. 開始 SE を鳴らし、鳴り終わってから `ActionStageScene` を単発ロードする。未設定ならすぐにロードする。

`startMenuLabels` の先頭だけをシアン、それ以外を白にする。いま割り当てがあるのは非表示の `NewGame` だけなので、この色は画面に出ない。

Inspector の `startGameMode` を `ContinueCurrentSession` にすると、進行とヘルプの表示済みはリセットせず、装備状態だけ読み直してから、同じ開始 SE のあと `ActionStageScene` へ入る。通常は `NewGame` のまま使う。

## 注意

決定の処理は `Update` にある。`Time.timeScale` が 0 のあいだは進まない。エンディングから戻るときは、遷移前に `Time.timeScale` を 1 に戻している。発光と BGM は、その停止中も進む。

`GameProgressManager` はタイトルに置いてある。ステージ一覧は `Stage01`。開始ステージ番号は 1、開始時の通貨は 0。エディタでは `Assets/Prefab/Stage/Stages` にある `StageDefinition` 付きプレハブへ一覧を同期する。

## 未定

次は仮のまま残している。

- 作品タイトルのロゴ画像と、正式な文言
- 画面の案内は `Press X button` だが、決定は `X` に加えて `Enter` / `Z` とゲームパッドの South / West でも入る
- `NewGame` の文字を表示するか
- タイトル専用の曲
- 背景を Stage01 の開始地点以外にするか
