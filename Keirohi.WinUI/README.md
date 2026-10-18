# 交通費入力（WinUI デスクトップ版）

左ペインで **入力・出力・パターン・設定**、右ペインで **登録内容の確認**（編集・並べ替え）と Suica 明細プレビューを行います。Suica の見た目は **WinUI ネイティブ描画**（Web / npm 不要）です。

## 起動

```bat
..\交通費WinUI版を開く.bat
```

BAT はビルド後に `Keirohi.App.exe` を直接起動します（Windows App SDK を PC 全体に入れなくても動く設定）。

### 起動しないとき

1. **.NET 8 SDK** が入っているか（コマンド: `dotnet --version`）
2. 以前起動した **Keirohi.App が残っていないか**（タスクマネージャーで終了）
3. BAT ウィンドウに **赤いエラー** が出ていないか（出た場合はそのまま読み取れるよう `pause` します）

または:

```powershell
cd Keirohi.WinUI
dotnet build
dotnet run
```

## データ保存場所

`%LocalAppData%\Keirohi\entries.json` / `settings.json` / `patterns.json`

## 出力

- 楽楽精算 CSV（Shift_JIS）
- モバイル Suica Excel（ClosedXML）
- Suica 印刷レイアウト確認: 右ペイン **Suica PDF プレビュー…** → **印刷…**

運賃シード: 同梱 `Assets/fare-seed.json`
