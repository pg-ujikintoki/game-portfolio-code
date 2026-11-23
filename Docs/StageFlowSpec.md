# ステージ進行仕様

`TitleScene` から `ActionStageScene` へ入り、クリアすると `EndingScene` を経てタイトルへ戻る。ステージデータは `Stage01` の 1 本。ファイルへのセーブとロードは無い。

関連ドキュメント:

- [タイトル仕様（仮）](TitleSpec.md)
- [エンディング仕様（仮）](EndingSpec.md)
- [ステージBGM仕様](StageBgmSpec.md)
- [ヘルプ仕様](HelpSpec.md)

## シーン

ビルドに入っているシーンは次の順。

1. `Assets/Scenes/TitleScene.unity`
2. `Assets/Scenes/ActionStageScene.unity`
3. `Assets/Scenes/EndingScene.unity`

`GameProgressManager` はタイトルにあり、`Awake` で `DontDestroyOnLoad` になる。ステージ一覧は `Assets/Prefab/Stage/Stages` のうち `StageDefinition` を持つプレハブで、いまは `Stage01` だけ。開始ステージ番号は 1。ニューゲームでは `ResetProgress()` によりステージ番号と通貨を初期値へ戻し、一度だけ表示したヘルプの表示済みも消す。次の入場では最初のヘルプがもう一度開く。手順は [タイトル仕様（仮）](TitleSpec.md) と [ヘルプ仕様](HelpSpec.md)。

`ActionStageScene` では、この一覧の現在ステージを生成する。

## ボス部屋への侵入

`StageManager` と `BossDoor`（`Assets/Scripts/Stage/BossDoor.cs`、プレハブ `Assets/Prefab/Stage/BossDoor.prefab`）。

ステージ読み込み時、ボスは `BossSpawnPoint` に非表示で生成される。`BossBase` はこの時点では無効。プレイヤーが `BossDoor` に触れると、一度だけ次の順で進む。

1. 移動、攻撃、チャージを止める。ポーズとヘルプも開けなくする。ヘルプ側の止まり方は [ヘルプ仕様](HelpSpec.md)。
2. フィールドを片付ける。
3. ドアを Inspector の上昇量だけ、上昇速度で上へ動かす。
4. プレイヤーが床に着くまで、その場で落下させる。すでに接地していれば、着地判定のあとすぐ次へ進む。このあいだは横に歩かせない。
5. プレイヤーを右へ、歩行量だけ歩かせる。速度はプレイヤーの `walkSpeed`。
6. カメラをボス部屋へ固定する。歩行が終わるまではプレイヤー追従のまま。
7. ドアを同じ速度で元の高さまで下ろす。
8. 警告 UI を 2 秒出す。表示と同時にボス BGM へ切り替える。
9. ボスを表示し、`BossBase` を有効化して `NotifyBossAppeared()` を呼ぶ。操作とポーズを戻す。

BGM とクリア SE の詳細は [ステージBGM仕様](StageBgmSpec.md)。

### ドア

- 見た目は `Shutter.png`。当たり判定はトリガーではなく、絵に合わせた固体の `BoxCollider2D`。
- 足もとを基準に上へ伸びる。`Rigidbody2D` は Kinematic。
- Inspector の初期値は、上昇量 `riseDistance` 3、上昇速度 `riseSpeed` 4、歩行量 `playerWalkDistance` 3。下降も `riseSpeed` を使う。速度または距離が 0 のときは、その場で切り替わる。
- ドアが開き切ってから、接地するまで横移動しない。空中なら `fallSpeed` でその場落下し、着地してから歩行を始める。着地判定は通常の `groundCheck`。侵入開始時に、ジャンプ直後の接地無視は捨てる。
- 歩行でほとんど進まない状態が 0.5 秒続くと、歩行を打ち切って続きに進む。

### 片付けるもの

- ボス以外の `EnemyBase`。スポナーが出したものも、ステージに直接置いた敵も含む。
- `EnemyBullet`、`EnemyCannonball`、`PunchedCorpse`。
- その時点で存在する `PickupItem`。ステージに置いたコインや、既に落ちているドロップが対象。
- `EnemySpawner` は `StopSpawning()` で止め、ボス戦中に再出現しない。
- この削除は `Die()` を通さない。ザコのドロップ抽選は走らない。非表示のボスは消さない。

### マップ上の名前

ステージプレハブの子を名前で探す。

- `BossDoor`
- `BossSpawnPoint`
- `BossRoomLevelBounds`（ボス戦のカメラ範囲）
- `LevelBounds`（通常時のカメラ範囲）
- `PlayerSpawnPoint`

カメラ固定後は、プレイヤー追従と `LevelBounds` へは戻さない。

## ステージクリア

`StageManager.OnBossDefeated` が、ボス撃破の瞬間にクリアする。ドアやアイテム取得ではクリアしない。

- 撃破と同時に BGM を止め、操作を止め、クリア表示とクリア SE を出す。撃破後にステージ BGM へは戻さない。
- そこから `stageClearSceneDelay` 秒（初期値 3、実時間）待って `EndingScene` へ単発ロードする。SE の鳴り終わりは待たない。画面とタイトルへ戻るまでの扱いは [エンディング仕様（仮）](EndingSpec.md)。音の手順は [ステージBGM仕様](StageBgmSpec.md)。

## 動作確認

- `BossDoor` に触れると、ザコと既存アイテムが消え、ドアが開く。プレイヤーは着地してから歩き、ドアが閉じてから警告が出る
- 空中の高い位置で `BossDoor` に触れても、着地前には歩かない。床に着いてから部屋へ入る
- 侵入演出中は移動、攻撃、ポーズが効かない
- 警告のあとボスが出現し、操作が戻る
- ボスを撃破した瞬間にステージクリアし、BGM が止まり、3 秒後に `EndingScene` へ入る
- アイテム取得やドアへの侵入ではステージクリアしない
