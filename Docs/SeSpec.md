# SE仕様

効果音の担当と、いまプレハブへ入っている割り当て。数値は各コンポーネントの初期値と、プレハブに入っている現在の割り当て。Inspector で上書きできる。

ステージ BGM とステージクリア SE は [ステージBGM仕様](StageBgmSpec.md)。

関連ドキュメント:

- [プレイヤー操作・行動仕様](PlayerControlsSpec.md)
- [敵共通仕様](EnemyBaseSpec.md)
- [ボス共通仕様](BossCommonSpec.md)
- [タイトル仕様（仮）](TitleSpec.md)

## 担当

音の種類で、クリップを持つ場所が分かれる。

| 種類 | 持つ場所 | 例 |
|---|---|---|
| キャラクターの行動・被ダメージ・撃破 | そのキャラクターのプレハブ | ダッシュ、射撃、被ダメージ、撃破 |
| ステージの進行 | `ActionStageScene` の `StageManager` | BGM、ステージクリア |
| タイトルの開始 | `TitleScene` の `TitleManager` | ニューゲーム開始 |

キャラクター SE は `StageManager` に置かない。編集するプレイヤーは `Assets/Prefab/Player/Player.prefab`。敵は `Assets/Prefab/Enemies/Enemy` の各プレハブ。

クリップが未設定の再生は飛ばす。

## 再生の仕方

被ダメージと撃破は `SfxOneShot`（`Assets/Scripts/General/SfxOneShot.cs`）で鳴らす。再生のたびに非表示のオブジェクトを作り、2D（`spatialBlend` 0）で 1 回再生し、クリップの長さが過ぎたら消す。長さが 0.1 秒未満のときは 0.1 秒残す。

撃破と同時に敵を破棄しても、音はこのオブジェクト側に残る。シーンをロードし直すと、鳴り途中でも止まる。

ダッシュ、射撃、チャージショットは、プレイヤー自身の `AudioSource` で `PlayOneShot` する。これも 2D で、`playOnAwake` はオフ。ソースが無ければ実行時に足す。射撃とチャージは同じソースを使う。

ダッシュ開始時、そのソースの `pitch` を `dashSfxPitchRange` の範囲で変える。変えたピッチは次のダッシュまで残る。いまのプレハブは範囲が 1〜1 なので、ピッチは 1 のまま。

ポーズとヘルプは `Time.timeScale` を 0 にするが、鳴り始めている SE は止めない。

## 被ダメージと撃破

同じヒットで体力が 0 になったときは、撃破 SE だけを鳴らす。被ダメージ SE は鳴らさない。

体力が減って 0 より大きいときだけ、被ダメージ SE を鳴らす。すでに死亡しているとき、ダメージが 0 以下のとき、ダメージを受け付けなかったときは、どちらも鳴らさない。

別のキャラクターの SE は重ねて鳴る。止めるのは、その 1 ヒットの中で被ダメージと撃破が同時になる場合だけ。

### プレイヤー

担当: `PlayerLife`

| 用途 | フィールド | 音量の初期値 | 現在の割り当て |
|---|---|---|---|
| 被ダメージ | `damageSfx` | `damageSfxVolume` 0.9 | `Assets/Sounds/SE/Player/Hit.wav` |
| 撃破 | `defeatSfx` | `defeatSfxVolume` 0.9 | 未設定 |

鳴る経路は `PlayerLife.TakeDamage`。接触ダメージ、敵弾、ダメージ床はここを通る。

- 体力が減り、まだ 0 より大きい: 被ダメージ SE。
- そのダメージで体力が 0: 撃破 SE。操作停止、Game Over 表示、5 秒後のシーン再読み込みはそのまま。
- 落下救済は死亡ではない。撃破 SE は鳴らない。
- 回復では鳴らない。

### 敵

担当: `EnemyBase`。ボスの撃破だけ `BossBase` が差し替える。

| 用途 | フィールド | 音量の初期値 | 現在の割り当て |
|---|---|---|---|
| 被ダメージ（ボスも含む） | `EnemyBase.damageSfx` | `damageSfxVolume` 0.9 | `Assets/Sounds/SE/Enemies/Hit.wav` |
| 通常敵の撃破 | `EnemyBase.defeatSfx` | `defeatSfxVolume` 0.9 | `Assets/Sounds/SE/Enemies/Boom1.wav` |
| ボス撃破 | `BossBase.bossDefeatSfx` | `bossDefeatSfxVolume` 0.9 | 未設定 |

`Assets/Prefab/Enemies/Enemy` のうち、次の 9 個は上の被ダメージと通常撃破を設定済み。

`EnemyCanon`、`EnemyCrawler`、`EnemyCrawlerCannon`、`EnemyDrone`、`EnemyHelpDrone`、`EnemyTank`、`Bokujin`、`Shielder`、`Boss01`。

`EnemyShield.prefab` は盾だけで、この 2 項目は無い。盾ヒットでは本体の被ダメージ SE は鳴らない。

ボス撃破は `bossDefeatSfx` が設定されていればそれだけを鳴らす。未設定なら、そのボスに付いている `defeatSfx` を使う。`Boss01` は未設定なので、いまは `Boom1.wav` が鳴る。被ダメージはボスも `damageSfx`。

撃破 SE は `Die()` の中で、破棄の前に鳴らす。距離デスポーンや、ボス部屋へ入るときのフィールド片付けでは `Die()` を呼ばないので鳴らない。

## プレイヤーの行動 SE

担当と割り当ては `Assets/Prefab/Player/Player.prefab`。

| 用途 | 担当 | フィールド | 現在の割り当て |
|---|---|---|---|
| ダッシュ開始 | `PlayerController` | `dashSfx` | `Assets/Sounds/SE/Player/Dash.wav` |
| 通常射撃 | `PlayerAttackController` | `shootSfx` | `Assets/Sounds/SE/Player/Shoot.wav` |
| チャージショット | `GunChargeAttack` | `chargeShotSfx` | 未設定 |

ダッシュの音量は 0.9。再再生の間隔 `dashSfxCooldown` は 0.05 秒。地上でダッシュを開始したときだけ鳴らす。空中へ持ち込まれた継続では鳴らない。

射撃の音量は 0.9。通常の `FireGun` と、チャージガトリングの `FireUncountedBullet` で鳴らす。剣、パンチ、ビットには専用の SE は無い。

チャージショットの音量初期値は 1。弾を実際に出したときだけ鳴らす。未設定なので、いまは鳴らない。

## タイトルのニューゲーム開始 SE

担当は `TitleScene` の `TitleManager`。

| 用途 | フィールド | 音量 | 現在の割り当て |
|---|---|---|---|
| ニューゲーム開始 | `startGameSfx` | `startGameSfxVolume` 0.9 | 未設定。再配布できない音源はポートフォリオ用に外している |

`New Game` を決めたあと、`ActionStageScene` へ移る前に 1 回鳴らす。`TitleManager` が自身のオブジェクトへ SE 用 `AudioSource` を実行時に追加する。2D（`spatialBlend` 0）、ループしない、`playOnAwake` はオフ。クリップの長さだけ実時間で待ち、鳴り終わってからシーンを単発ロードする。`Time.timeScale` が 0 でもこの待ちは進む。

クリップが未設定のときは鳴らさず、待たずにロードする。BGM はシーンを出るまで止めない。画面の流れは [タイトル仕様（仮）](TitleSpec.md)。

## ステージクリア SE

担当は `StageManager` の `stageClearSfx`。音量 0.9。割り当ては未設定で、再配布できない音源はポートフォリオ用に外している。未設定のときは鳴らさず、3 秒後に遷移する。手順は [ステージBGM仕様](StageBgmSpec.md)。
