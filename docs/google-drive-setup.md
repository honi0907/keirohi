# Google Drive 同期のセットアップ

通勤経路費の **デスクトップ（Avalonia）** と **モバイル Web** で同じ Drive フォルダ内の JSON を同期します。

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
3. `web/google-config.js` を作成（`web/google-config.sample.js` をコピー）し、`clientId` を記入

```javascript
window.KEIROHI_GOOGLE = {
  clientId: "xxxx.apps.googleusercontent.com",
};
```

`google-config.js` は `.gitignore` 済み（秘密情報をコミットしないため）。

## 4. 同期フォルダ

- 設定で付けたフォルダ名（既定: `通勤経路費`）の Drive フォルダを作成または再利用します
- 中身: `entry-sets.json`, `patterns.json`, `fare-memory.json`, `settings.json`（PC 形式）

## 5. 競合と復元

- **今すぐ同期** の直前にローカルデータを自動退避します
- 取り込み結果が期待と違う場合は **同期前のローカルデータに戻す** を使用してください
- ファイル単位で **更新日時が新しい方** を採用します（v1）
