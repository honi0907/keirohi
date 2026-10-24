namespace Keirohi_App.Updates;

public sealed record AppReleaseProfile(
    string GitHubRepo,
    string ReleaseTagPrefix,
    string AssetNamePattern,
    string DisplayName)
{
    public const string WinUiRepo = "honi0907/keirohi";

    public const string AvaloniaRepo = "honi0907/keirohi-avalonia";

    public static AppReleaseProfile WinUi { get; } = new(
        WinUiRepo,
        "v",
        "Keirohi-",
        "交通費入力");

    public static AppReleaseProfile Avalonia { get; } = new(
        AvaloniaRepo,
        "v",
        "Keirohi",
        "通勤経路費");

    /// <summary>WinUI 向け（後方互換）。</summary>
    public static AppReleaseProfile Default => WinUi;
}
