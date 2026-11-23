# チャージショット実装まとめ

## チャージショットの仕様

- 攻撃ボタン押下時に通常弾を即座に発射し、そのままボタンを押し続けることでチャージが開始される。
- チャージ時間は `IChargeableWeapon.ChargeDuration`（Gun では `GunChargeAttack` が管理）から取得し、2 秒経過でチャージ完了。
- チャージ完了後にボタンを離すと `TryFireChargedAttack` が呼び出され、検証成功時のみチャージ弾を発射する。チャージ弾は1発のみ。
- 同時発射上限は `PlayerAttackController` が保持する Projectile リストで管理され、通常弾/チャージ弾を合算して上限を判定。上限到達時はチャージ弾を発射せず状態を解除。
- チャージはメイン/サブスロットそれぞれ個別に計測される。ポーズ中はタイマー進行を停止し、ポーズ解除時に入力状態を再チェックして継続/キャンセルを決定する。
- チャージ中に能力喪失、武器変更、対象武器の無効化、プレイヤーの攻撃不能化などが発生した場合はチャージ状態を即座に解除する。
- チャージ弾は `ChargeProjectile` を使用し、地形や盾命中時には消滅。貫通設定が有効な敵を撃破した場合のみ弾を維持し、続けて飛行を継続する。
- `ChargeState` は `None / Charging / Charged` の3状態を取り、矛盾を避けるため `PlayerChargeController` が一元管理する。

## チャージ演出

担当: `ChargeEffectController`（何を出すか）、`PlayerChargeController`（いつ切り替えるか）

演出は Main / Sub のスロットに紐づく。装備中の武器種類では切り替わらない。Gun でも Sword でも、同じスロットなら同じ演出になる。

プレハブ `Assets/Prefab/Player/Player.prefab` の子:

- `MainChargeEffect`（`ChargeEffectController`）
  - `Stage1`（初期は非表示）
  - `Stage2`（初期は非表示）
- `SubChargeEffect`（`ChargeEffectController`）
  - `Stage1`
  - `Stage2`

| 段階 | `Stage Effects` | 出すタイミング | 初期アニメ |
|---|---|---|---|
| 段階1 | 要素 0 | チャージ開始から完了まで | `ChargeM1` |
| 段階2 | 要素 1 | チャージ完了後、ボタンを離すまで | `ChargeM2` |

アニメは `Assets/Animator/Effect/`。Main と Sub は別オブジェクトなので、スロットごとに差し替えられる。初期状態では両方とも同じクリップを使っている。

キャンセル、発射、武器変更、能力喪失では両方を消す。チャージ中と完了後は、その段階のオブジェクトを有効にしたまま維持する。

段階を増やすときは、`Stage Effects` に要素を足し、子に `Stage3` のように対応するオブジェクトを置く。表示の切り替えは `PlayerChargeController` が `ShowStage` で行う。現状のチャージ状態は「チャージ中」と「チャージ完了」の2つだけなので、使っている段階も2つである。

`Main Charge Effect` / `Sub Charge Effect` や `Stage Effects` が未設定のときは、起動時に子の名前（`MainChargeEffect`、`SubChargeEffect`、`Stage1`、`Stage2`）から拾う。

## チャージガトリング（W103）の仕様

- Equipment パーツ `W103`（ChargeGatling）が ON の場合、チャージショット解放後にボタンを離した際、チャージ弾の代わりに通常弾を16連発する。
- チャージ時間・チャージ中のキャンセル条件・エフェクトなど、チャージガトリング以外の挙動は通常のチャージショットと同一（`GunChargeAttack` / `PlayerChargeController` の既存ロジックをそのまま使用）。
- 16連射は `GunChargeAttack.GatlingRoutine` が担当し、1発ごとに `gatlingFireInterval`（既定 0.05 秒）待って `PlayerAttackController.FireUncountedBullet` を呼び出す。
- `FireUncountedBullet` は通常弾（`Bullet`）と全く同じ Prefab・性能で発射するが、`PlayerAttackController.activeProjectiles` に登録しない。そのため同時発射数制限（`bulletSimulLimit`）のカウントに含まれず、通常弾やチャージショットの発射可否に影響しない。
- 発射する弾 Prefab は `GunChargeAttack.gatlingBulletPrefab` で個別指定可能（未設定時は通常弾 Prefab を使用）。将来的に専用のガトリング弾へ差し替える余地を残している。
- 発射中（16発撃ち切るまでの間）に以下のいずれかが発生した場合、以降の未発射分をキャンセルする。すでに発射済みの弾はそのまま飛び続ける。
  - ロードアウト変更で対象スロットの武器が Gun 以外になった
  - `ChargeGatling`（W103）が OFF になった
  - チャージショット能力自体（W102）を喪失した
- ポーズ中は他のチャージ処理と同様に進行が止まり、ポーズ解除後に残りの発射を再開する（`Time.timeScale` 制御による `WaitForSeconds` の自然停止を利用）。
- 能力解放判定は `PurchasedPartsAbilityProvider` が行う。`W103` を所持し、Equipment が ON のときだけチャージガトリングを使える。

## プレハブ上の割り当て

変更は `Assets/Prefab/Player/Player.prefab` に行う。`ActionStageScene` に置いた Player へコンポーネントを足したり、数値を上書きしたりしない。編集場所の詳細は [プレイヤー操作・行動仕様](PlayerControlsSpec.md) を参照。

プレハブに付いているもの:

- `PlayerAttackController` の `Gun Charge Attack` は、同じオブジェクトの `GunChargeAttack` を指す。
- `PlayerChargeController` の `Main Charge Effect` / `Sub Charge Effect` は、子の `MainChargeEffect` / `SubChargeEffect` を指す。能力判定は `PurchasedPartsAbilityProvider`（W102 / W103）。
- `GunChargeAttack` の `Charge Projectile Prefab` はチャージ弾、`Projectile Spawn Point` は `bulletSpawnPoint`。ガトリング弾は通常弾 Prefab。
- 各段階の見た目は、`ChargeEffectController` の `Stage Effects` で差し替える。段階オブジェクトの初期状態は非表示。

## チャージ弾 Prefab

チャージ弾 Prefab には `ChargeProjectile`、`Rigidbody2D`（Kinematic）、`Collider2D`（Trigger）を設定する。Tag は `Bullet`、Layer は `PlayerBullet` などプレイヤー弾に適合するものを使う。`terrainLayers` に地形レイヤーを、`destroyOnShieldHit` を必要に応じて設定する。ダメージとコンボポイントは `AttackStatsData`（または `PlayerProjectileBase`）で調整する。

## 敵側設定

チャージ弾で撃破したあと貫通を続けさせたい敵には、HurtBox か敵本体に `ChargeProjectilePierceTarget` を置き、`AllowPierceOnDeath` を有効にする。未設定でも現状は貫通する。敵ごとに止めたい場合は、このコンポーネントで明示する。

## 能力解放

判定は `PurchasedPartsAbilityProvider`。

- チャージショット（`W102`）は、そのパーツを所持しているとき使える。パーツの型が Equipment なら ON のときだけ、Passive なら所持しているだけで使える。
- プレイヤープレハブでは `forceUnlockChargeShotForDemo` がオンになっている。このフラグがオンのあいだは、所持パーツが無くてもチャージショットを使える。チャージガトリングには効かない。
- チャージガトリング（`W103`）は、所持かつ Equipment が ON のときだけ使える。
- タイトルからニューゲームすると所持パーツは空になる。シーンに `IntermissionProgressManager` の初期所持が無いため、上記フラグがオフだとチャージショットは出ない。

## 仮のままの挙動

- **ダメージ貫通**  
  `ChargeProjectile` は `ChargeProjectilePierceTarget` が未設定でも貫通する。敵ごとに止めたい場合は、そのコンポーネントで明示する。

## 未実装

- **敵貫通設定の使い分け**  
  現状はデフォルトで貫通する。ボスや特定のギミックで止めるかは、敵ごとの `ChargeProjectilePierceTarget` で決める。

段階1・段階2のスプライト演出は入っている。チャージ完了音、発射時エフェクト、キャンセル時演出は未実装。
