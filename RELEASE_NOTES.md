MEISHI Pop Exporter v0.2.6（公開テスト版）

- パッケージIDを `jp.pipipigiken.mpexporter` / `jp.pipipigiken.mpavatar-package` に変更しました。
- Destinationは初期状態・通常モードではiOS固定になりました。
- **MEISHI Pop > Exporter Developer Mode** をオンにすると、iOS / StandaloneOSX / StandaloneWindows64を選べます。フラグは初期状態でオフで、マシンのプロジェクトごとに保存します。
- 既存プロファイルのMac／Windows向けDestinationは保持されます。通常モードの実出力はiOSになり、開発モードで保存済みの選択が復元します。

**VCC / VPM：** 新IDのMEISHI Pop Exporterを追加してください。旧IDの2パッケージを置換するlegacyPackages設定を含みます。VCC上での実移行操作は未検証です。
**Unitypackage：** `MEISHI-Pop-Exporter-0.2.6.unitypackage` をインポートしてください。従来のフォルダーとGUIDを維持しています。VPM版を導入済みの場合はVCCから導入してください。

検証：Exporterはコンパイル成功、27件中25件成功（プロファイル11件はすべて成功）。既存シェーダー設定テスト2件はUnityのpersistent object保存エラーで失敗。アプリ側の関連76件、配布物の構造・GUID・依存関係・旧ID移行宣言・再現性の5件は成功。通常／開発モードの画面表示と切り替え後のConsole Error 0件を確認しました。

Unity 2022.3 / Built-in向け。iPhone用の出力にはiOS Build Supportが必要です。今回の変更を使ったアバターの全再出力・iPhone実機確認は未実施です。エクスポーターの配布ライセンスは検討中です。
