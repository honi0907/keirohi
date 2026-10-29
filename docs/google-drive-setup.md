# Google Drive 同期のセットアップ

通勤経路費の **デスクトップ（Avalonia）** と **モバイル Web** で、同じ Drive フォルダ内の JSON を同期します（**推奨**）。手動 JSON 共有は予備です。

## 1. Google Cloud プロジェクト

1. [Google Cloud Console](https://console.cloud.google.com/) でプロジェクトを作成
2. **API とサービス → ライブラリ** で **Google Drive API** を有効化
3. **OAuth 同意画面** を構成（テストユーザーに自分の Google アカウントを追加）

## 2. デスクトップ用 OAuth クライアント

1. **認証情報 → 認証情報を作成 → OAuth クライアント ID**
2. アプリケーションの種類: **デスクトップアプリ**
3. JSON をダウンロードし、次のファイル名で保存:

```
%LocalAppData%\Keirohi\google-oauth-client.json
```

（例: `C:\Users\<あなた>\AppData\Local\Keirohi\google-oauth-client.json`）

4. アプリ **設定 → Google Drive 同期 → Google でログイン**

## 3. モバイル Web 用 OAuth クライアント

1. 同じプロジェクトで **OAuth クライアント ID** を **Web アプリケーション** として追加
2. **承認済みの JavaScript 生成元** にローカル開発 URL を追加（例: `http://localhost:8765`）
3. `web/google-config.js` の `clientId` を記入（リポジトリ同梱の空ファイルを編集するか、`google-config.sample.js` をコピー）
4. **承認済みの JavaScript 生成元** に GitHub Pages の URL も追加（例: `https://honi0907.github.io`）

```javascript
window.KEIROHI_GOOGLE = {
  clientId: "xxxx.apps.googleusercontent.com",
};
```

`clientId` は公開してよい値ですが、リポジトリにコミットするかは任意です（GitHub Pages では `web/google-config.js` がそのまま配信されます）。

## 4. 同期フォルダ

- 設定で付けたフォルダ名（既定: `通勤経路費`）の Drive フォルダを作成または再利用します
- 中身: `entry-sets.json`, `patterns.json`, `fare-memory.json`（共通）、`settings.json`（**PC のみ**）
- モバイル: アプリ上部 **共有** → **Google でログイン** → **今すぐ同期**（フォルダ名は PC と同じ「通勤経路費」など）

## 5. 競合と復元

- **今すぐ同期** の直前にローカルデータを自動退避します
- 取り込み結果が期待と違う場合は **同期前のローカルデータに戻す** を使用してください
- ファイル単位で **更新日時が新しい方** を採用します（v1）
