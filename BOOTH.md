# Booth配布手順

## 公開版をアップロードする

1. [GitHub Releases](https://github.com/orange3134/meishi-pop-exporter/releases) で配布するバージョンを開きます。
2. Assetsの `MEISHI-Pop-Exporter-X.Y.Z-Booth.zip` をダウンロードします。
3. Boothの商品編集画面で、そのZIPを展開せずにダウンロード商品として登録・更新します。
4. 商品説明のバージョンと更新内容を確認して、手動で公開します。

`vX.Y.Z` タグ公開時にUnitypackageとBooth用ZIPを自動生成します。VPM用の `jp.pipipigiken.*.zip` とは別のファイルです。Boothへのログイン・アップロード・公開は自動化しません。

## 公開せずに生成する

[Actions → Validate Packages](https://github.com/orange3134/meishi-pop-exporter/actions/workflows/validate.yml) の **Run workflow** で対象ブランチを選択して実行します。成功後、Artifactsの **booth-zip** をダウンロードし、Actionsの外側のZIPを1回展開してください。中の `MEISHI-Pop-Exporter-X.Y.Z-Booth.zip` をそのままBoothに登録します。artifactの保存期間は30日です。

ローカルでも生成できます。

```bash
python3 -m unittest discover -s scripts -p 'test_*.py' -v
python3 scripts/build_release.py
```

出力先は `dist/MEISHI-Pop-Exporter-X.Y.Z-Booth.zip` です。

## ZIPの内容

- `MEISHI-Pop-Exporter-X.Y.Z.unitypackage`
- `説明書.url` — Windows用のインターネットショートカット。指定のNotion説明書を開きます。

説明書URLは `distribution.json` の `booth.manualUrl` で管理します。ショートカット本文はASCII・CRLF、ZIP内の日本語ファイル名はUTF-8で記録します。Macでのダブルクリック動作は保証しません。Macでは `.url` をテキストエディターで開き、`URL=` の後ろのアドレスをブラウザーへコピーできます。

CIではWindowsでZIPを展開し、日本語ファイル名・WindowsのショートカットAPIが解釈するURL・Unitypackageのハッシュを確認します。ブラウザー起動や説明書ページの閲覧権限まではこのテストの対象にしません。

## 商品説明に記載する導入手順

1. Unity 2022.3のBuilt-inプロジェクトに、VRCSDK AvatarsとNDMFを導入します。アバターで使用するModular Avatarやシェーダーも先に導入します。
2. iPhone向けに書き出す場合は、Unity Hubで同じEditorの **iOS Build Support** を追加します。
3. ダウンロードしたBooth用ZIPを展開します。`説明書.url` から説明書を開けます。
4. **Assets → Import Package → Custom Package…** から中のUnitypackageを選び、すべてインポートします。
5. Hierarchyでアバタールートを選択して **MEISHI Pop → Export Avatar** を開きます。通常のDestinationは **iOS** です。

Exporterと共通形式の両方、およびApache 2.0のLICENSE.txt・NOTICE・LICENSE_SCOPE.mdを含みます。SDK・NDMF・アバター・シェーダーは同梱しません。VPM版が入っているプロジェクトへの重複インポートは避けてください。

通常のUnitypackage更新は既存ファイルに上書きインポートします。削除・移動されたファイルを含む版では、そのリリースの移行手順に従ってください。

## 導入テスト

0.2.10の生成Unitypackageを実際にインポートし、プレビュー・iOS書き出し・Mac Bundle読込を確認しています。[検証条件と結果](UNITYPACKAGE_VALIDATION.md)を参照してください。
