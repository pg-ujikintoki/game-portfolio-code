# ステージBGM仕様

`ActionStageScene` の BGM とステージクリア SE。数値は `StageManager` の初期値と、シーンに入っている現在の割り当て。Inspector で上書きできる。

関連ドキュメント:

- [ステージ進行仕様](StageFlowSpec.md)
- [ポーズシステム](PauseSystem.md)
- [ヘルプ仕様](HelpSpec.md)
- [SE仕様](SeSpec.md)

## 概要

再生は `StageManager`（`Assets/Scripts/Stage/StageManager.cs`）が行う。`ActionStageScene` の `StageManager` オブジェクトに、BGM 用と SE 用の `AudioSource` を実行時に追加する。どちらも 2D（`spatialBlend` 0）、`playOnAwake` はオフ。BGM はループする。

ポーズとヘルプは `Time.timeScale` を 0 にするが、BGM は止めない。クリア SE も止めない。

## 音の割り当て

| 用途 | フィールド | 現在の割り当て |
|---|---|---|
| ステージ BGM | `stageBgm` | 未設定 |
| ボス BGM | `bossBgm` | 未設定 |
| クリア SE | `stageClearSfx` | 未設定 |

ポートフォリオ用に、再配布できない音源は外している。シーンに残っている参照先のファイルはリポジトリに無い。

音量は BGM `bgmVolume` 0.45、クリア SE `stageClearSfxVolume` 0.9。

ステージ BGM は、再生中のステージプレハブに付いている `StageDefinition.bgm` が優先される。未設定ならシーンの `stageBgm` を使う。`Stage01` の `bgm` もシーンの `stageBgm` も未設定なので、いまは鳴らない。

ボス BGM とクリア SE はシーン側だけ。ステージごとの差し替えはない。

曲が未設定のときは、その再生を飛ばす。ボス BGM が無いときはステージ BGM を止めず、撃破までそのまま流す。

## 流れ

1. ステージ読み込みの `Start` でステージ BGM をループ再生する。READY 表示より前に鳴る。
2. ボス部屋の侵入演出で WARNING を出した瞬間に、ボス BGM へ切り替える。警告の 2 秒間もボス BGM。切り替えにクロスフェードは無い。
3. ボスを倒した `OnBossDefeated` で、その瞬間に BGM を止めてステージクリアにする。撃破後にステージ BGM へは戻さない。
4. クリアと同時にクリア SE を 1 回鳴らす。鳴り終わりは待たず、`stageClearSceneDelay` 秒後に次のシーンへ移る。

ボス部屋へ入る手順の全体は [ステージ進行仕様](StageFlowSpec.md) の「ボス部屋への侵入」。クリア条件は同仕様の「ステージクリア」。

## クリア後の待ち

待ちはクリア開始からの `stageClearSceneDelay` を実時間で測る。クリア SE の長さは含めない。`Time.timeScale` が 0 でもこの待ちは進む。

初期値は 3 秒。0 以下のときは待たず、クリア処理に入った時点で遷移する。

クリアのたびに `EndingScene` へ入る。入ったあとの BGM とタイトルへの戻りは [エンディング仕様（仮）](EndingSpec.md)。シーンを単発ロードするので、移動と同時にステージ BGM も止まる。

## ポーズとヘルプ

ポーズメニューとヘルプ表示のあいだも BGM は流れ続ける。`StageManager` は `PauseState` を購読しない。

ボス部屋の侵入演出中はポーズもヘルプも開けない。WARNING からボス出現までの 2 秒もこのロック中なので、その区間でポーズはできない。ボス出現後の戦闘中は開ける。そのときも BGM は継続する。

プレイヤーが倒れてシーンを読み直した場合は、ステージ開始からなのでステージ BGM に戻る。
