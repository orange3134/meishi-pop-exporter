MEISHI Pop Exporter v0.2.3（公開テスト版）

- 「表情を自動抽出する」チェックボックスを追加しました（初期値オン）。
- オフにすると、母音5種・瞬き・happy / smile / angry / sadを含むBlendShape名からの自動抽出を停止します。
- 手動で追加したAnimationClipの表情は、オフでも出力されます。眼球ボーンの変換も維持します。

ファイル形式は変更していないため、MEISHI Pop!アプリの更新は不要です。設定を反映するには再エクスポートして、アプリで新しいファイルを読み直してください。

**VCC / VPM：** Manage ProjectからMEISHI Pop Exporterを0.2.3へ更新してください。
**Unitypackage：** `MEISHI-Pop-Exporter-0.2.3.unitypackage` をインポートしてください。VRCSDKとNDMFは事前に導入が必要です。VPM版を導入済みの場合はVCCから更新してください。

検証：Unity 2022.3.22f1でコンパイル成功、表情関連9テスト成功。今回の変更に対するiPhone実機確認は未実施です。別の既存シェーダー設定テスト2件はテスト内の設定保存APIで失敗し、サンプル環境ではNDMF previewのHarmony初期化エラーも確認しています。

Unity 2022.3 / Built-in向け。エクスポーターの配布ライセンスは検討中です。
