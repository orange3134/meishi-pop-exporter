# MEISHI Pop Exporter

VRChatアバターのUnityプロジェクトから、MEISHI Pop!用の `.mpavatar` を書き出すEditor拡張です。公開テスト版です。

**[配布ページ](https://vpm.pipipigiken.jp/)** · **[Unitypackage](https://github.com/orange3134/meishi-pop-exporter/releases/latest)**

## インストール

### VCC / VPM（おすすめ）

1. VRCSDK Avatars導入済みのプロジェクトを用意し、Unityを閉じます。VCCでの導入・更新後にUnityを開き直します。
2. [Modular Avatar / NDMFの公式配布元](https://modular-avatar.nadena.dev/ja/docs/intro)をVCCに追加します。NDMFが必要です。Modular Avatarはアバターが使用している場合に必要です。
3. [配布ページ](https://vpm.pipipigiken.jp/)の「VCCに追加する」を押します。手動の場合はVCCの Settings → Packages → Add Repository に下記URLを指定します。

   `https://vpm.pipipigiken.jp/vpm.json`

4. 対象プロジェクトの Manage Project で **MEISHI Pop Exporter** を追加します。**MEISHI Pop Package Format** は依存関係として自動導入されます。

### Unitypackage

VRCSDKとNDMFを先に導入し、[Releases](https://github.com/orange3134/meishi-pop-exporter/releases)から `MEISHI-Pop-Exporter-x.y.z.unitypackage` をダウンロードしてUnityへドラッグします。Exporterと共通パッケージをまとめて導入できます。

- Unitypackage版は `Assets/MEISHIPop/Exporter` と `Assets/MEISHIPop/PackageFormat` に入ります。
- VPM版を導入済みならVCCから更新してください。Unitypackageを重ねて入れないでください。
- Unitypackage → VPMの移行では、上記2フォルダーをVPMの `legacyFolders` により置換します。自分でコードを変更した場合は先にバックアップしてください。
- 以前のZIPを「Add package from disk」で追加していた場合は、そのローカルパッケージ2つを外してからVCCで導入します。

## 使い方

1. UnityのEdit Modeで、Hierarchyからアバタールートを選択します。
2. **MEISHI Pop → Export Avatar** を開きます。
3. 「表情を自動抽出する」は初期状態でオンです。母音・瞬き・名前からの表情候補が不要ならオフにします。追加の表情が必要なら表情名とAnimationClipを指定します。追加クリップは自動抽出がオフでも出力されます。フレーム選択は自動です。「こだわり設定」で時刻を調整できます。
4. iPhone用は **iOS**、Mac Editor用は **StandaloneOSX** を選び、`.mpavatar` を書き出します。
5. MEISHI Pop!でアバターを変更し、新しいファイルを読み込みます。元の出力ファイルの上書きだけでは、取り込み済みの名刺は更新されません。

Unity **2022.3 / Built-in Render Pipeline** 向けです。iPhone用の出力には、そのUnity Editorと同じバージョンの **iOS Build Support** をUnity Hubから追加してください。プラットフォームごとに別ファイルが必要です。

検証済みの書き出し環境：Unity 2022.3.22f1、VRCSDK 3.10.5、NDMF 1.14.8、Modular Avatar 1.18.7。SDK依存範囲は3.10.5以上、3.11未満です。iOS用Bundleの最終表示は実機で確認してください。

## 任意のサムネイル画像

「サムネイル画像（任意）」にUnityへインポートした画像を指定すると、`.mpavatar`に同梱し、MEISHI Pop!のアバター一覧で使用します。未指定（None）の場合は従来どおり、取り込み時にアバターを自動撮影します。名刺全体のサムネイルはこれまでどおり名刺から撮影します。

画像は縦横比と透過を保ち、長辺512px以内のPNGに変換します。Read/Writeをオンにする必要はありません。画像の参照はプロファイルにも保存でき、Undo / Redoに対応します。

画像を変更したら再エクスポートし、アプリで新しいファイルを読み直してください。出力ファイルの上書きだけでは、取り込み済みのサムネイルは更新されません。

## 書き出しプロファイル

表情やDestinationを設定したら、エクスポーター上部の「新規保存」でプロジェクト内に `.asset` として保存できます。次回は「プロファイル」欄で選択すると設定を復元できます。前回選択したプロファイルは、Unityやウィンドウを開き直したときも復元されます。

- 保存対象：表情の自動抽出のオン／オフ、追加表情の名前・クリップ・並び順、自動／手動フレームと時刻、Destination、サムネイル画像。
- 選択中の変更は自動保存され、Undo / Redoに対応します。衣装ごとに設定を分けたい場合は「複製」してから編集します。
- Avatarは保存しません。書き出すアバタールートはHierarchyから選択してください。
- プロファイルは元クリップを参照します。Unity上での移動・名前変更には追従しますが、別プロジェクトに移す際はクリップなどの元素材も必要です。
- 未選択でも従来どおり書き出せます。既存の `.mpavatar` から編集設定を復元する機能ではありません。

## 変換範囲

- NDMF / Modular Avatarの処理結果、Humanoid、Mesh、Material、Shaderを使用します。
- PhysBoneの角度制限、重力、Colliderを中立形式へ変換します。アプリでの動的応答は近似です。
- viseme、瞬き、眼ボーン、一部BlendShape名と追加AnimationClipから静止表情を抽出します。
- 衣装は書き出し時点の状態です。FX Controller / Expressions Menuの動作、Contacts、OSC、掴み、Stretch/Squishなどは再現しません。
- VRC RotationConstraintは一部対応。他の未対応Constraintはレポートを表示して書き出しを停止します。
- シェーダーは元のものを保持します。Metal非対応の記述やVRChat固有の照明など、完全一致は保証できません。

出力元のUnityプロジェクトと、利用できるアバター・素材が必要です。VRChatゲーム内で利用できるだけのアバターや、VRChatサーバーから取得したデータは対象外です。

## 配布構成

現在このリポジトリが直接収録するソースは、MEISHI PopのエクスポーターとSDK非依存の共通形式のみです。MEISHI Pop!アプリ本体、アバター、VRCSDK、NDMF、シェーダーなどの第三者パッケージは同梱しません。

| 内容 | 場所 |
|---|---|
| Exporter | `Packages/com.avatarnamecard.exporter` |
| 共通形式 | `Packages/com.avatarnamecard.avatar-package` |
| 配布用メタデータ・移行GUID | `distribution.json` |

VCCの共通配布一覧は [orange3134/vpm](https://github.com/orange3134/vpm) が管理します。VCC追加URLは `https://vpm.pipipigiken.jp/vpm.json` のままです。このリポジトリには一覧生成・Pages公開の処理を置きません。

## MEISHI Popの開発・リリース

MEISHI Popのコード修正は非公開アプリ側の元パッケージで行い、このリポジトリへ必要なファイルだけ同期します。アプリの履歴やAssets全体をコピーしません。

```bash
python3 scripts/sync_from_app.py --source /path/to/private-app-checkout
python3 -m unittest discover -s scripts -p 'test_*.py' -v
python3 scripts/build_release.py
```

`dist/` にVPM ZIP 2つ、Unitypackage 1つ、`vpm-release.json`、`SHA256SUMS.txt` ができます。Python 3.10以降の標準ライブラリだけで動作し、UnityライセンスやSDKの再配布は不要です。Unitypackageは元のmeta/GUIDを保持し、Runtime・Editorのみを同梱します。

公開手順：

1. 元パッケージ2つの `package.json` のversionを同じ新しい番号へ更新し、上記同期を行います。ランタイムのExporterVersion定数なども必要に応じて更新します。
2. `RELEASE_NOTES.md` を更新して、差分を確認・コミットし、mainをpushします。
3. 同じコミットに `vX.Y.Z` タグを付けてpushします。

```bash
git push origin main
git tag vX.Y.Z
git push origin vX.Y.Z
```

**Release Exporter** が両形式を生成してGitHub Releaseへ公開します。共通一覧の **Build Repo Listing** が約1時間ごとに検出し、配布一覧とサイトを更新します。タグとバージョンが異なる場合は停止します。公開済みの同じバージョンは上書きせず、番号を上げてください。失敗したドラフトの再実行は可能です。

PR / mainへのpushでは **Validate Packages** が構造・GUID・ハッシュ・移行先・再現性を確認し、配布ファイルをActionsのartifactとして保存します。

## VPM一覧への反映

Release公開後、[vpmリポジトリ](https://github.com/orange3134/vpm) が約1時間ごとに新しいバージョンを取り込みます（GitHubの混雑等で遅れる場合があります）。

すぐ反映したい場合は、[Build Repo Listing](https://github.com/orange3134/vpm/actions/workflows/build-listing.yml) の **Run workflow** を実行してください。GitHub CLIでも実行できます。

```bash
gh workflow run build-listing.yml --repo orange3134/vpm
```

Release ExporterのActions結果画面にも、この案内を表示します。ドメイン・一覧への他ツール追加は [vpmのREADME](https://github.com/orange3134/vpm#readme) を参照してください。

## ライセンス

公開テスト中です。エクスポーターの配布ライセンスは検討中で、現時点ではMIT等のオープンソースライセンスを付与していません。アバターや素材の権利・利用条件は各提供元に従ってください。
