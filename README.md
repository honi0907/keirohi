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
