# Avalonia（Mac / Windows）と WinUI のメモ

**WinUI 版は凍結**（2026-10 以降）。製品は **Avalonia「通勤経路費」** のみ（Win / Mac）。

WinUI にだけ残っている同等機能（出力の一部停止・運賃の履歴・外観）は **2026-10-25 時点で一度揃え済み**ですが、以降の変更は Avalonia のみ行います。

データ用 API（`Keirohi.Core`）は共通です。

## ファイル出力

- **楽楽精算 CSV を保存** のみ有効
- 次の3つは `IsEnabled="False"` + `Opacity="0.45"`（処理は残す）
  - モバイル Suica Excel を保存
  - CSV + Excel を一括保存（同じフォルダ）
  - 楽楽精算 CSV を取り込み（追加）

Avalonia: `Keirohi.Avalonia/Views/ExportView.axaml`  
WinUI: `Keirohi.WinUI/Pages/ExportPage.xaml`

## 運賃の履歴

設定の「運賃の自動入力」の下、**運賃の履歴…**

- 別ウィンドウで出発・到着・金額を編集、行追加・削除、**保存**で `fare-memory.json` へ
- API: `GetFareMemory()` / `ReplaceFareMemory(...)`

Avalonia: `Windows/FareMemoryWindow.*`  
WinUI: `FareMemoryWindow.*`

## 外観

設定の **外観**: システム / ライト / ダーク。選択時に反映、`settings.json` の `themePreference`。

Avalonia: `Helpers/AppThemeHelper.cs` + `SettingsView`  
WinUI: `Helpers/AppThemeHelper.cs` + `SettingsPage`

## Mac / Avalonia 専用（WinUI に移さない）

- `.dmg` / `Keirohi.app` / `AppIcon.icns`（「通経」アイコン）
- Gatekeeper（`xattr -cr`）、DMG 更新フロー
- 乗換案内の in-app WebView（Mac）。Windows Avalonia は WebView2 フォーム、WinUI は従来ウィンドウ
- Avalonia モーダル親指定（PDF 保存後 OK）。WinUI は ContentDialog
