# スマホでオフライン利用する

入力・パターン・登録は **オフライン** で使えます。PC や Drive とのデータ受け渡しは **JSON 共有**（手動）です。

## 方法 A: ZIP をスマホにコピー（PC サーバー不要）

1. PC で:

   ```powershell
   .\scripts\Pack-MobileWebOffline.ps1
   ```

2. `dist\keirohi-mobile-web.zip` をスマホへ送る
3. 解凍して **Chrome** で `index.html` を開く

## データの受け渡し

1. 右上 **共有** → **JSON を共有…** → Drive / Files 等を選択  
2. 別端末や PC から JSON を受け取ったら **JSON を取り込む…**  
3. 取り込み直前のデータは **取り込み前のデータに戻す** で復元可能

## 方法 B: PWA（HTTPS / localhost）

`web` を HTTPS 配信し、ホーム画面に追加。Service Worker でオフライン起動。

## 制限

- 共有・取り込み時のみファイル操作（Drive API 自動同期は使いません）
- Yahoo!乗換案内リンクはオンライン時のみ
