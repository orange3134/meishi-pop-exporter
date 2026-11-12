# Unitypackage実インポート検証

検証日: 2026-11-12。対象: `MEISHI-Pop-Exporter-0.2.10.unitypackage`。

SHA-256: `a4e24d6f3273aff60521f473190541f1b16a12d869d6b6dd0b79a7f00fe3d1cd`

## 環境と導入

macOS、Unity 2022.3.22f1、Built-in Render Pipeline、VRCSDK 3.10.5、NDMF 1.14.8、Modular Avatar 1.18.7、lilToon 2.3.4。

既存の検証用アバタープロジェクトから専用コピーを作成し、Exporter／共通形式のUPM参照とtestablesを外した。以前のAsset Database・ScriptAssemblies・Beeキャッシュは引き継いでいない。SDK等の依存パッケージは導入済み。

1. `scripts/build_release.py --tag v0.2.10` でUnitypackageを生成し、GitHub Releaseの公開ファイルとバイト単位で一致することを確認。
2. MEISHI Popのアセンブリが未ロードの状態で、Unityの `AssetDatabase.ImportPackage(path, false)` を実行。
3. `Assets/MEISHIPop/Exporter` と `Assets/MEISHIPop/PackageFormat` に導入され、VPM／UPM版2パッケージが登録されていないことを確認。
4. 全28項目のGUIDと、22ファイルの内容が生成アーカイブと一致。LICENSE.txt・NOTICE・LICENSE_SCOPE.mdも両フォルダーに存在。
5. コンパイルError 0／Warning 0。コンパイラーの入力が `Assets/MEISHIPop/` 内のスクリプトであることも確認。

## 実際の操作・書き出し

- メニュー **MEISHI Pop → Export Avatar** から画面を開けた。
- 通常モードのDestinationが **iOS**。Kipfelの表情クリップで、こだわり設定オフのプレビューを表示して画像を確認。
- インポートされた `AvatarExporter.Export`（画面のExportボタンと同じ処理）で、追加Smile表情を含むiOS向け `.mpavatar` を書き出した。
- iOS出力: exporterVersion 0.2.10、Metal、Bundle 12,680,158 bytes、揺れ物46件・Collider16件・表情28件。共通形式リーダーで読み戻し、JSON・BundleのSHA-256を別途照合。追加Smileも含まれている。
- Mac向けにも出力し、AssetBundleを実際にロード・インスタンス化。Humanoid、Renderer 24個、コンポーネント331個、表情28件を確認。欠損コンポーネント／マテリアル／シェーダーがなく、許可されたSDK非依存コンポーネントだけであることを検証。
- iOS／Macの書き出し・Mac Bundle読込区間のConsole Error 0。

## 回帰テスト

インポート完了後にテストコードだけを専用コピーへ追加し、インポート済み本体を参照して `AvatarNamecard.Exporter.Tests` を実行。コンパイル成功、全33件成功（失敗0・スキップ0）。表情抽出・プレビュー・プロファイル・サムネイル・iOS向けShader設定の復元を含む。

既存のShader設定テスト2件は、persistentな設定オブジェクトを `SaveToSerializedFileAndForget` に渡す処理がUnity 2022.3でエラーになるため修正した。設定ファイルへ有効なYAMLコメントを書き込み、正常終了・例外時の両方で元のバイト列へ復元されることを検証する。本体コードは変更していない。

配布テスト7件成功。構造、GUID、移行先、ライセンス文書、ハッシュ、再現性を検証。

## 検証範囲

Unitypackageのインポート、Editorの画面・表情プレビュー、iOS／Macの書き出しとアーカイブ検証、Mac AssetBundleのロードを確認した。iPhone実機での表示・揺れ物やWindows上のUnitypackage新規導入は今回未検証。

検証環境の起動／Domain Reload時には、既存のNDMF／Modular AvatarのHarmony初期化由来の `mprotect returned EACCES` が発生する。起動ログと上記の操作区間を分けて確認した。検証用アバター、出力された.mpavatar、検証プロジェクトは配布物・公開リポジトリに含めない。

## Booth用ZIP

Booth用ZIPには、上記と同じUnitypackageと、指定URLを保存したWindows用の `説明書.url` を同梱する。配布テストでは内包Unitypackageのバイト一致、ショートカットのURL・改行、日本語ファイル名、ZIPのSHA-256・再現性を検証する。Windows CIではExpand-Archiveで展開し、Windows ExplorerのShell.Application（System.Link.TargetUrl）で日本語ファイル名のショートカットからURLを読み取る。Unitypackage本体は変わらないため、上記インポート・書き出し検証の対象と同一。
