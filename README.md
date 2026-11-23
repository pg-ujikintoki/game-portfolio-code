# product01
個人開発中の2D横スクロールアクションゲーム<br>
ポートフォリオ用にスクリプトとドキュメントのみをまとめたリポジトリになっています。

[作品紹介ページ](https://pg-ujikintoki.github.io/game-portfolio/)

[ダウンロードはこちら](https://github.com/pg-ujikintoki/game-portfolio/releases/download/portfolio/GamePortfolio.zip)<br>
解凍後に「Product01.exe」を実行してください。

## プレイ方法
ゲームパッドはXboxコントローラーの使用を想定しています。

| 行動 | キーボード | ゲームパッド |
|---|---|---|
| 左右移動 | ← / → | 左スティック / 十字 |
| ジャンプ | Space | South（A / ×） |
| ダッシュ | Right Shift | LB（Left Shoulder） |
| メイン攻撃 | Z | West（X / □） |
| サブ攻撃 | X | RB（Right Shoulder） |
| ポーズ | 1 | Start |
| ヘルプ | 2 / B | East（B / ○） |

詳細は [プレイヤー操作・行動仕様](Docs/PlayerControlsSpec.md)。

## ドキュメント

### プレイヤー

- [プレイヤー操作・行動仕様](Docs/PlayerControlsSpec.md)

### バトル

- [敵共通仕様（EnemyBase）](Docs/EnemyBaseSpec.md)
- [壁沿い敵仕様](Docs/WallEnemySpec.md)
- [ボス共通仕様（BossBase）](Docs/BossCommonSpec.md)
- [Boss01 仕様](Docs/Boss01Spec.md)
- [チャージショット仕様](Docs/ChargeShotSpec.md)
- [ビット攻撃仕様](Docs/BitAttackSpec.md)

### システム

- [通貨・アイテムシステム](Docs/CurrencyAndItemSystem.md)
- [ポーズシステム](Docs/PauseSystem.md)
- [SE仕様](Docs/SeSpec.md)
- [ステージBGM仕様](Docs/StageBgmSpec.md)

### 進行

- [ステージ進行仕様](Docs/StageFlowSpec.md)
- [タイトル仕様（仮）](Docs/TitleSpec.md)
- [エンディング仕様（仮）](Docs/EndingSpec.md)
- [ヘルプ仕様](Docs/HelpSpec.md)
