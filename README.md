# 交通費計算

WinUI 版と Avalonia 版のデスクトップアプリ（Web / npm は不要）。データはどちらも `%LocalAppData%\Keirohi\` を共有します。

## 起動

| 版 | BAT |
|----|-----|
| WinUI | `交通費WinUI版を開く.bat` |
| Avalonia | `交通費Avalonia版を開く.bat` |

## 開発

```powershell
dotnet build Keirohi.sln
```

要件: .NET 8 SDK、Windows 10/11。

## GitHub Release（リポジトリ別）

| 版 | GitHub リポジトリ | スクリプト | Setup ファイル名 |
|----|-------------------|------------|------------------|
| WinUI | [honi0907/keirohi](https://github.com/honi0907/keirohi) | `.\scripts\Build-Release.ps1` | `Keirohi-<version>-x64-Setup.exe` |
| Avalonia | [honi0907/keirohi-avalonia](https://github.com/honi0907/keirohi-avalonia) | `.\scripts\Build-Release-Avalonia.ps1` | `Keirohi.Avalonia-<version>-x64-Setup.exe` |

どちらも **Inno Setup 6** でインストーラ（`.exe`）を生成します。`-SkipGitHub` でローカルビルドのみ、`gh` ログイン済みなら `v<Version>` の Release を作成します。

バージョンは各 csproj の `Version`（WinUI: `Keirohi.App.csproj`、Avalonia: `Keirohi.Avalonia.csproj`）。インストール後は起動時または設定の「更新を確認」から、**それぞれのリポジトリ**の Release を参照します。

Avalonia 用 GitHub リポジトリは空で作成してから初回 Release してください。

### Mac 向け（Avalonia）

- プロジェクトは **`net8.0`（Mac/Linux）** と **`net8.0-windows`（Win + GDI 印刷）** のマルチターゲットです。Mac では **`Keirohi.WinUI` はビルド不要**（Avalonia + Core + Updates のみ）。
- Mac では乗換案内を **アプリ内 WebView ポップアップ**（Yahoo!乗換案内）で開き、PDF は **PDFsharp** と Avalonia の印刷位置確認ウィンドウを使います（Windows は WebView2 ポップアップ + GDI 印刷確認も利用可能）。
- データ保存先は Windows の `%LocalAppData%\Keirohi\` とは別です（Mac ではユーザー Application Support 等。`fare-memory.json` も別ファイル）。

**要件:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（`dotnet --version` で 8.x を確認）。

**注意（マルチ TFM）:** `Keirohi.Avalonia` は `net8.0-windows` も含むため、Mac で `dotnet build` / `dotnet publish` すると restore 時に **NETSDK1100** になることがあります。シェルスクリプトは `-p:TargetFrameworks=net8.0` を付けています。手動のときも同様に付けてください。

```bash
cd keirohi
export PATH="$HOME/.dotnet:$PATH"   # ユーザーインストールした場合

# Debug ビルド・起動
dotnet build Keirohi.Avalonia/Keirohi.Avalonia.csproj -c Debug -f net8.0 -p:TargetFrameworks=net8.0
dotnet run --project Keirohi.Avalonia/Keirohi.Avalonia.csproj -c Debug -f net8.0 -p:TargetFrameworks=net8.0
```

**Release publish → .app → .dmg（ローカル配布用、署名・notarization なし）**

アーキテクチャは自動判定（Intel → `osx-x64`、Apple Silicon → `osx-arm64`）。PowerShell がある場合は `.\scripts\Build-Publish-Avalonia-osx.ps1` でも publish できます（`-Runtime osx-x64` 等）。

```bash
chmod +x scripts/build-publish-avalonia-macos.sh \
  scripts/bundle-avalonia-macos-app.sh \
  scripts/create-avalonia-macos-dmg.sh

./scripts/build-publish-avalonia-macos.sh    # dist/publish-avalonia/<runtime>/
./scripts/bundle-avalonia-macos-app.sh     # dist/Keirohi.app
./scripts/create-avalonia-macos-dmg.sh     # dist/Keirohi-<version>-<runtime>.dmg

open dist/Keirohi.app
```

**DMG からインストールしたあと「壊れているため開けません」と出る場合**（GitHub から落とした未公証ビルドでよくある表示で、ファイル自体が壊れたわけではありません）:

1. **Applications** に `Keirohi.app` を入れたあと、ターミナルで  
   `xattr -cr /Applications/Keirohi.app`  
   を実行してから再度起動する  
2. または Finder で `Keirohi.app` を **右クリック →「開く」→「開く」**

開発用の `dist/Keirohi.app` でも同様に `xattr -cr` できます。公証（notarization）は後から追加可能です。

**GitHub Release に Mac 用 DMG を載せる**（[keirohi-avalonia](https://github.com/honi0907/keirohi-avalonia)）:

```bash
gh auth login     # 初回のみ（https://cli.github.com/）
./scripts/upload-avalonia-macos-release.sh
```
