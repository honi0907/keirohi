# 通勤経路費（Keirohi）

Win / Mac 共通の **Avalonia** デスクトップアプリ（Web / npm は不要）。

- **製品名:** 通勤経路費
- **アイコン:** 「通経」（`Keirohi.Avalonia/Assets/`）
- **WinUI 版:** [凍結](./Keirohi.WinUI/README.md)（新規開発・Release 対象外）
- **Win / Mac の Release 揃え:** [docs/RELEASE-SYNC.md](./docs/RELEASE-SYNC.md)

## 起動（Windows 開発）

ダブルクリック: **`通勤経路費を開く.bat`**

```powershell
dotnet run --project Keirohi.Avalonia/Keirohi.Avalonia.csproj -c Debug -f net8.0-windows
```

要件: .NET 8 SDK、Windows 10/11（Windows 版）。

データ（Windows）: `%LocalAppData%\Keirohi\`（旧 WinUI 版と同じフォルダを共有できます）。

## 開発

```powershell
.\scripts\Build-Debug-Avalonia.ps1
```

（起動中の通勤経路費があれば自動終了してからビルド）

共有ライブラリ: `Keirohi.Core` / `Keirohi.Updates`。WinUI プロジェクトはビルド不要です。

### Windows アイコン（.ico）

インストーラ・exe 用の `AppIcon.ico` は `AppIcon-1024.png`（通経）から生成します。

```powershell
.\scripts\Build-AppIcon-Windows.ps1
```

Release ビルド（`Build-Release-Avalonia.ps1`）では自動実行されます。Release 表 [docs/RELEASE-SYNC.md](./docs/RELEASE-SYNC.md) も同スクリプト／Mac の upload スクリプトで自動更新されます。

**Windows で Debug ビルド:** 通勤経路費が起動中だと DLL がロックされて失敗します。`.\scripts\Build-Debug-Avalonia.ps1` を使う（起動中のアプリを自動終了してからビルド）。Cursor エージェントも [`.cursor/rules/build-avalonia.mdc`](./.cursor/rules/build-avalonia.mdc) に従い、ユーザーに終了を頼まず Stop スクリプトを先に実行する。

## GitHub Release

| プラットフォーム | リポジトリ | スクリプト | 成果物 |
|------------------|------------|------------|--------|
| Windows | [honi0907/keirohi-avalonia](https://github.com/honi0907/keirohi-avalonia) | `.\scripts\Build-Release-Avalonia.ps1` | `Keirohi.Avalonia-<version>-x64-Setup.exe` |
| Mac | 同上 | `scripts/upload-avalonia-macos-release.sh` 等 | `.dmg` |

**Inno Setup 6** で Windows インストーラを生成。`-SkipGitHub` でローカルのみ、`gh` ログイン済みなら `v<Version>` の Release を作成。

バージョン: `Keirohi.Avalonia/Keirohi.Avalonia.csproj` の `Version`。更新確認は設定から **keirohi-avalonia** の Release を参照します。

旧 WinUI 用 [honi0907/keirohi](https://github.com/honi0907/keirohi) の Release は今後使いません。

### Mac 向け（Avalonia）

- **`net8.0`**（Mac）と **`net8.0-windows`**（Windows + GDI 印刷）のマルチターゲット。
- Mac: 乗換案内は **アプリ内 WebView**、PDF は PDFsharp + 印刷位置確認。
- Windows: WebView2 ポップアップ + GDI 印刷確認も利用可能。
- Mac のデータは Windows の `%LocalAppData%\Keirohi\` とは別パスです。

**要件:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

**注意（マルチ TFM）:** Mac で build/publish するときは `-f net8.0 -p:TargetFrameworks=net8.0` を付けてください（シェルスクリプトは既に付与済み）。

```bash
dotnet build Keirohi.Avalonia/Keirohi.Avalonia.csproj -c Debug -f net8.0 -p:TargetFrameworks=net8.0
dotnet run --project Keirohi.Avalonia/Keirohi.Avalonia.csproj -c Debug -f net8.0 -p:TargetFrameworks=net8.0
```

**Release publish → .app → .dmg**

```bash
chmod +x scripts/build-publish-avalonia-macos.sh \
  scripts/bundle-avalonia-macos-app.sh \
  scripts/create-avalonia-macos-dmg.sh \
  scripts/build-app-icon-macos.sh

./scripts/build-publish-avalonia-macos.sh
./scripts/bundle-avalonia-macos-app.sh
./scripts/create-avalonia-macos-dmg.sh
```

**Gatekeeper（未公証ビルド）:** `xattr -cr /Applications/Keirohi.app` または右クリック →「開く」。

**GitHub Release に Mac DMG:**

```bash
gh auth login
./scripts/upload-avalonia-macos-release.sh
```

Mac アイコン: `scripts/build-app-icon-macos.sh`（`AppIcon.icns` / `AppIcon.png`）。

## ドキュメント

- [Release 同期（Win Setup / Mac DMG）](./docs/RELEASE-SYNC.md)
- [Avalonia / WinUI 機能差分メモ（WinUI 凍結後）](./docs/avalonia-mac-changes-for-winui.md)
