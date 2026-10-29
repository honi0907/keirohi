# モバイル Web の更新の仕組み

## ZIP 版（`file://`）

- ブラウザの安全制限で **アプリ自身が ZIP 内の HTML を書き換えられません**
- 更新 = **新 ZIP の入手** が必要（自動では不可）
- データは **JSON 共有** で退避可能

## URL 版（GitHub Pages 推奨）

1. GitHub リポジトリで **Settings → Pages → GitHub Actions** を有効化
2. `main` に `web/` を push すると [deploy-mobile-web.yml](../.github/workflows/deploy-mobile-web.yml) が公開
3. 公開 URL（例）: `https://honi0907.github.io/keirohi/`
4. スマホ Chrome で開き **ホーム画面に追加**
5. 以降: オンラインで開く → **更新バナー** → **再読み込み** だけでソフト更新（ZIP 差し替え不要）

`web/version.json` と `index.html` 内の `APP_VERSION` を上げて push すると更新が検出されます。

Release 用 ZIP は [Pack-MobileWebOffline.ps1](../scripts/Pack-MobileWebOffline.ps1) で引き続き作成できます（オフライン専用向け）。
