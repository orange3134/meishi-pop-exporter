# Booth配布手順

## 公開版をアップロードする

1. [GitHub Releases](https://github.com/orange3134/meishi-pop-exporter/releases) で配布するバージョンを開きます。
2. Assetsの `MEISHI-Pop-Exporter-X.Y.Z.unitypackage` をダウンロードします。
3. Boothの商品編集画面で、そのファイルをダウンロード商品として登録・更新します。
4. 商品説明のバージョンと更新内容を確認して、手動で公開します。

`vX.Y.Z` タグ公開時にUnitypackageは自動生成されます。VPM用ZIPはBoothのUnitypackageとして登録しません。Boothへのログイン・アップロード・公開は自動化しません。

## 公開せずに生成する

[Actions → Validate Packages](https://github.com/orange3134/meishi-pop-exporter/actions/workflows/validate.yml) の **Run workflow** で対象ブランチを選択して実行します。成功後、Artifactsの **booth-unitypackage** をダウンロードし、ZIPを展開してください。中の `.unitypackage` が配布対象です。artifactの保存期間は30日です。

ローカルでも生成できます。

```bash
python3 -m unittest discover -s scripts -p 'test_*.py' -v
python3 scripts/build_release.py
```

出力先は `dist/MEISHI-Pop-Exporter-X.Y.Z.unitypackage` です。

## 商品説明に記載する導入手順

1. Unity 2022.3のBuilt-inプロジェクトに、VRCSDK AvatarsとNDMFを導入します。アバターで使用するModular Avatarやシェーダーも先に導入します。
2. iPhone向けに書き出す場合は、Unity Hubで同じEditorの **iOS Build Support** を追加します。
3. **Assets → Import Package → Custom Package…** からUnitypackageを選び、すべてインポートします。
4. Hierarchyでアバタールートを選択して **MEISHI Pop → Export Avatar** を開きます。通常のDestinationは **iOS** です。

Exporterと共通形式の両方、およびApache 2.0のLICENSE.txt・NOTICE・LICENSE_SCOPE.mdを含みます。SDK・NDMF・アバター・シェーダーは同梱しません。VPM版が入っているプロジェクトへの重複インポートは避けてください。

通常のUnitypackage更新は既存ファイルに上書きインポートします。削除・移動されたファイルを含む版では、そのリリースの移行手順に従ってください。

## 導入テスト

0.2.10の生成Unitypackageを実際にインポートし、プレビュー・iOS書き出し・Mac Bundle読込を確認しています。[検証条件と結果](UNITYPACKAGE_VALIDATION.md)を参照してください。
