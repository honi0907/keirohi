# 交通費計算

**WinUI デスクトップ版のみ**（Web / npm は不要）。

## 起動

`交通費WinUI版を開く.bat` をダブルクリック。

## 開発

```powershell
cd Keirohi.WinUI
dotnet build
```

要件: .NET 8 SDK、Windows 10/11。

## 配布

バージョンは `Keirohi.WinUI/Keirohi.App.csproj` の `Version`。

```powershell
.\scripts\Build-Release.ps1
```

GitHub Release `v*` に `Keirohi-<version>-x64-Setup.exe` を載せます。インストール後は起動時、または設定の「更新を確認」から更新できます。
