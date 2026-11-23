# エンディング仕様（仮）

このドキュメントは仮仕様です。画面、曲、待ち時間、遷移条件は確定していません。いま動いている実装を、あとから差し替える前提で記録しています。

関連ドキュメント:

- [ステージ進行仕様](StageFlowSpec.md)
- [タイトル仕様（仮）](TitleSpec.md)
- [ステージBGM仕様](StageBgmSpec.md)

## 対応範囲

- `EndingScene` の表示
- シーン開始時の BGM と、途中からのフェードアウト
- 一定時間後の `TitleScene` への遷移
- ステージクリアから `EndingScene` へ入る現在の条件

スタッフロール、スキップ入力、エンディング専用の演出は含みません。進行と通貨のファイル保存もありません。

## シーン

`Assets/Scenes/EndingScene.unity`

進行は `EndingSceneController`（`Assets/Scripts/Ending/EndingSceneController.cs`）。背景のステージとプレイヤーは `TitleStagePresenter`（`Assets/Scripts/Title/TitleStagePresenter.cs`）。タイトル画面と同じコンポーネントで、同じ見た目にしている。

入力は受け取らない。表示中にボタンを押しても、待ち時間もフェードも変わらない。プレイヤーは操作できない。

## 画面

`TitleStagePresenter` がシーン開始時に次を行う。

1. `Stage01` プレハブを原点に生成する。
2. `PlayerSpawnPoint` にプレイヤーを置く。スポーン地点が無いときは `StageDefinition` の開始座標を使う。
3. プレイヤーの操作を止める。移動も攻撃もしない。
4. ステージ内のヘルプトリガーを無効にする。プレイヤーを置くより前なので範囲には入らない。ヘルプは開かず、看板があっても `Idle` のまま。
5. カメラを正射影サイズ 3.5 にし、画面全体が `LevelBounds` に収まる位置へ置く。アクションステージ開始時と同じ構図にする。

体力バーなどの戦闘 UI は出さない。敵も、この開始地点の画面には出さない。

その上に、画面中央の文字を重ねる。担当は Canvas 上の `EndingText`。Canvas は Screen Space Overlay。

```
Thank you for playing.
Scheduled for release in 2027
```

カメラに Pixel Perfect Camera が付いている場合は無効にする。エンディングのカメラには付いていない。正射影サイズは `orthographicSize`（初期値 3.5）で上書きする。

## 入り方

ステージクリア後、`StageManager.WaitAfterClear` が `SceneLoader.Load("EndingScene")` する。クリアのたびにこのシーンへ入る。`GameProgressManager` の有無では分岐しない。

クリア表示とクリア SE の手順は [ステージ進行仕様](StageFlowSpec.md) の「ステージクリア」と [ステージBGM仕様](StageBgmSpec.md)。

## 開始時

1. `TitleStagePresenter.Awake` でステージ、プレイヤー、カメラを用意する。
2. `EndingSceneController.Start` で `Time.timeScale` を 1 に戻す。ステージ側で止まっていた時間を、タイトルへ持ち越さない。
3. BGM をループ再生する。
4. 2.5 秒後から BGM をフェードアウトする。
5. 5 秒後に `TitleScene` を単発ロードする。

フェードとシーン遷移は別々に待つ。初期値では、無音になるタイミングとタイトルへの遷移が同じ 5 秒目になる。

## BGM

| 項目 | 内容 |
|---|---|
| フィールド | `endingBgm` |
| 現在の割り当て | 未設定。再配布できない音源はポートフォリオ用に外している |
| 音量 | `bgmVolume` 0.45 |
| 再生 | シーン開始時にループ。2D（`spatialBlend` 0）、`playOnAwake` はオフ |
| フェード開始 | `bgmFadeStartTime` 2.5 秒 |
| フェード時間 | `bgmFadeDuration` 2.5 秒 |

`EndingSceneController` が、自身のオブジェクトへ BGM 用 `AudioSource` を実行時に追加する。クリップが未設定のときは再生とフェードを飛ばし、遷移の待ちはそのまま進む。

0 秒から 2.5 秒までは `bgmVolume` のまま鳴らす。2.5 秒目から音量を直線で下げ、5 秒目に 0 にする。待ちは実時間（`WaitForSecondsRealtime` と `Time.unscaledDeltaTime`）なので、`Time.timeScale` が 0 でも進む。

`bgmFadeDuration` が 0 以下のときは、フェード開始と同時に音量を 0 にする。

クリップが無いので、フェードの待ちは進むが音は鳴らない。Inspector の `endingBgm` で差し替えできます。フェードの開始と長さも Inspector で変えられます。

`TitleScene` へ移るとシーンごと破棄されるので、鳴り残った BGM もそこで止まる。タイトル側の画面と BGM は [タイトル仕様（仮）](TitleSpec.md)。

## タイトルへの遷移

待ち時間は `titleTransitionDelay`。初期値は 5 秒。実時間（`WaitForSecondsRealtime`）で待つ。`Time.timeScale` が 0 でもこの待ちは進む。

0 以下のときは待たず、開始処理の直後に `SceneLoader.Load("TitleScene")` する。

遷移時にステージ進行、通貨、所持パーツ、ヘルプの表示済みは変更しない。タイトルでニューゲームを始めると、進行と通貨を初期化し、ヘルプの表示済みも消す。次のステージでは最初のヘルプがもう一度開く。

## 未定

次は仮のまま残している。

- エンディング専用の曲と、ループするか 1 回で止めるか
- 表示文言、スタッフロール、複数画面
- 背景を Stage01 の開始地点以外にするか
- ボタンやキーによるスキップ
- クリア後の遷移先をエンディング以外にするか
