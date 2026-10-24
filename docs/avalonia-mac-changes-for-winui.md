# Avalonia（Mac）側の変更で、WinUI にまだ入っていないもの

Mac 用 Release（`honi0907/keirohi-avalonia`）に入っていて、WinUI の画面にはまだ反映していない操作です。Windows 版を揃えるときのメモです。

データ用の API（`Keirohi.Core`）は共通です。WinUI に足すのは主に画面です。

対象バージョン: Avalonia **1.1.4**（2026-10-24）。WinUI の csproj はこれとは別番号のままです。

## ファイル出力（v1.1.4）

場所: ファイル出力。Avalonia は `Keirohi.Avalonia/Views/ExportView.axaml`。WinUI は `Keirohi.WinUI/Pages/ExportPage.xaml`。

- **楽楽精算 CSV を保存** だけ押せる
- 次の3つは `IsEnabled="False"` かつ半透明（`Opacity="0.45"`）
  - モバイル Suica Excel を保存
  - CSV + Excel を一括保存（同じフォルダ）
  - 楽楽精算 CSV を取り込み（追加）

クリック処理はそのまま残し、ボタンを無効にしているだけです。

## 運賃の履歴（v1.1.3）

場所: 設定の「運賃の自動入力」の下。ボタン **運賃の履歴…**。

Avalonia:

- `Keirohi.Avalonia/Views/SettingsView.axaml`
- `Keirohi.Avalonia/Windows/FareMemoryWindow.axaml`

別ウィンドウで、覚えた運賃（出発・到着・金額）を一覧し、編集・行の追加・削除ができる。**保存**で `fare-memory.json` に書く。

共通 API（WinUI からも呼べる）:

- `AppStateService.GetFareMemory()`
- `AppStateService.ReplaceFareMemory(...)`

空行は無視。出発か到着が欠けている、金額が 1 円未満、同じ区間の重複は保存しない。

WinUI の設定（`SettingsPage.xaml`）にはこのボタンがまだない。

## 外観（v1.1.0〜1.1.2）

設定の **外観**: システムに合わせる / ライト / ダーク。選んだ時点で切り替わり、`settings.json` の `themePreference`（`System` / `Light` / `Dark`）に保存する。

Avalonia の実装:

- `Keirohi.Avalonia/Helpers/AppThemeHelper.cs`
- `Keirohi.Avalonia/Views/SettingsView.axaml` の ComboBox（並びは System, Light, Dark）

WinUI は見た目の切り替え UI はない。設定保存時に `ThemePreference` を消さないよう、いまの値を書き戻しているだけ（`SettingsPage.xaml.cs`）。

## Mac 専用で、WinUI に移さないもの

- `.dmg` / `Keirohi.app` / `AppIcon.icns`（「通経」アイコン）
- Gatekeeper 対策（`xattr -cr`）。署名・公証は未実施。配布 DMG は Intel `osx-x64` のみ
- PDF 保存後の OK ダイアログが押せない問題（Avalonia のモーダル親ウィンドウ）。WinUI は ContentDialog なので別件
