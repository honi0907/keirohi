namespace Keirohi_App.Updates;

public sealed record AppReleaseProfile(
    string GitHubRepo,
    string ReleaseTagPrefix,
    string AssetNamePattern,
    string DisplayName)
{
    public const string DefaultRepo = "honi0907/keirohi";

    public static AppReleaseProfile Default { get; } = new(
        DefaultRepo,
        "v",
        "Keirohi",
        "交通費入力");
}
