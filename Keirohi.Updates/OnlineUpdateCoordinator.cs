namespace Keirohi_App.Updates;

public sealed class OnlineUpdateCoordinator
{
    private readonly AppUpdateService service = new();

    public async Task<OnlineUpdateRunResult> RunAsync(
        Func<string, Task<bool>> confirmUpdateAsync,
        IProgress<OnlineUpdateProgress>? progress,
        Action requestExit,
        bool promptOnlyWhenAvailable,
        AppReleaseProfile? releaseProfile = null,
        CancellationToken cancellationToken = default)
    {
        var profile = releaseProfile ?? AppReleaseProfile.Default;
        var current = AppVersionReader.GetCurrentVersion();
        try
        {
            progress?.Report(new OnlineUpdateProgress("更新を確認中...", 0));
            var check = await service.CheckForUpdateAsync(profile, current, cancellationToken);
            if (!check.Ok)
            {
                return promptOnlyWhenAvailable
                    ? OnlineUpdateRunResult.UpToDate(string.Empty)
                    : OnlineUpdateRunResult.Failed(check.Error ?? "更新確認に失敗しました。");
            }

            if (!check.Available)
            {
                return OnlineUpdateRunResult.UpToDate(
                    $"最新です（現在 {check.CurrentVersion} / 最新 {check.LatestVersion}）");
            }

            var message =
                $"新しいバージョンがあります。\n{check.CurrentVersion} → {check.LatestVersion}\n更新するとアプリは自動で再起動します。";
            if (!await confirmUpdateAsync(message))
            {
                return OnlineUpdateRunResult.Cancelled("更新をあとで行います。");
            }

            if (!PackagedAppDetector.CanApplyOnlineUpdate())
            {
                return OnlineUpdateRunResult.Failed(
                    $"開発ビルドでは自己更新できません。インストールした {profile.DisplayName} から起動してください。");
            }

            var installerPath = await service.DownloadInstallerAsync(check, progress, cancellationToken);
            progress?.Report(new OnlineUpdateProgress("更新を適用して再起動します...", 100));
            if (!AppUpdateService.TryLaunchInstaller(installerPath, out var launchError))
            {
                return OnlineUpdateRunResult.Failed(launchError ?? "インストーラーの起動に失敗しました。");
            }

            requestExit();
            return OnlineUpdateRunResult.Applied("更新を開始しました。再起動します。");
        }
        catch (OperationCanceledException)
        {
            return OnlineUpdateRunResult.Cancelled("更新をキャンセルしました。");
        }
        catch (Exception ex)
        {
            return promptOnlyWhenAvailable
                ? OnlineUpdateRunResult.UpToDate(string.Empty)
                : OnlineUpdateRunResult.Failed(ex.Message);
        }
    }
}

public sealed class OnlineUpdateRunResult
{
    public OnlineUpdateOutcome Outcome { get; init; }
    public string Message { get; init; } = "";

    public static OnlineUpdateRunResult UpToDate(string message) =>
        new() { Outcome = OnlineUpdateOutcome.UpToDate, Message = message };

    public static OnlineUpdateRunResult Cancelled(string message) =>
        new() { Outcome = OnlineUpdateOutcome.Cancelled, Message = message };

    public static OnlineUpdateRunResult Failed(string message) =>
        new() { Outcome = OnlineUpdateOutcome.Failed, Message = message };

    public static OnlineUpdateRunResult Applied(string message) =>
        new() { Outcome = OnlineUpdateOutcome.Applied, Message = message };
}

public enum OnlineUpdateOutcome
{
    UpToDate,
    Cancelled,
    Failed,
    Applied,
}
