using System.ComponentModel;
using System.Diagnostics;

namespace Keirohi_App.Updates;

public sealed class AppUpdateService
{
    private readonly GitHubReleaseClient github = new();

    public async Task<AppUpdateCheckResult> CheckForUpdateAsync(
        AppReleaseProfile profile,
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profile.GitHubRepo) || !profile.GitHubRepo.Contains('/'))
        {
            return AppUpdateCheckResult.Fail("GitHub リポジトリが不正です。");
        }

        var current = VersionComparer.Normalize(currentVersion) ?? currentVersion;
        GitHubReleaseDto? release;
        try
        {
            release = await FindLatestReleaseAsync(profile, cancellationToken);
            if (release is null)
            {
                return AppUpdateCheckResult.Fail("GitHub Release が見つかりません。");
            }
        }
        catch (Exception ex)
        {
            return AppUpdateCheckResult.Fail($"GitHub の確認に失敗しました: {ex.Message}");
        }

        var latestVersion = ResolveReleaseVersion(release, profile);
        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            return AppUpdateCheckResult.Fail("リリースのバージョンを判別できません。");
        }

        var asset = PickInstallerAsset(release.Assets, profile.AssetNamePattern);
        if (asset is null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
        {
            return AppUpdateCheckResult.Fail("Release に Setup .exe がありません。");
        }

        if (VersionComparer.Compare(latestVersion, current) <= 0)
        {
            return AppUpdateCheckResult.UpToDate(current, latestVersion, release.TagName, release.HtmlUrl);
        }

        return AppUpdateCheckResult.UpdateAvailable(
            current,
            latestVersion,
            release.TagName ?? latestVersion,
            release.HtmlUrl,
            asset.Name ?? "Keirohi-Setup.exe",
            asset.BrowserDownloadUrl);
    }

    public async Task<string> DownloadInstallerAsync(
        AppUpdateCheckResult check,
        IProgress<OnlineUpdateProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        if (!check.Ok || !check.Available || string.IsNullOrWhiteSpace(check.DownloadUrl))
        {
            throw new InvalidOperationException(check.Error ?? "更新情報がありません。");
        }

        var assetName = Path.GetFileName(check.AssetName ?? "Keirohi-Setup.exe");
        var targetPath = Path.Combine(
            Path.GetTempPath(),
            $"keirohi-update-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{assetName}");
        progress?.Report(new OnlineUpdateProgress("ダウンロード準備中...", 0));
        await github.DownloadAsync(check.DownloadUrl, targetPath, progress, cancellationToken);
        return targetPath;
    }

    public static bool TryLaunchInstaller(string installerPath, out string? error)
    {
        error = null;
        if (!File.Exists(installerPath))
        {
            error = "更新ファイルが見つかりません。";
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS",
                UseShellExecute = true,
                Verb = "runas",
            });
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            error = "更新を中止しました（管理者の許可が拒否されました）。";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private async Task<GitHubReleaseDto?> FindLatestReleaseAsync(
        AppReleaseProfile profile,
        CancellationToken cancellationToken)
    {
        var releases = await github.ListReleasesAsync(profile.GitHubRepo, cancellationToken);
        return releases
            .Where(r => !string.IsNullOrWhiteSpace(r.TagName))
            .Select(r => new
            {
                Release = r,
                Version = ResolveReleaseVersion(r, profile),
                PublishedAt = r.PublishedAt ?? DateTimeOffset.MinValue,
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Version))
            .OrderByDescending(x => VersionComparer.Compare(x.Version, "0.0.0"))
            .ThenByDescending(x => x.PublishedAt)
            .Select(x => x.Release)
            .FirstOrDefault();
    }

    private static string? ResolveReleaseVersion(GitHubReleaseDto release, AppReleaseProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(release.TagName))
        {
            var fromTag = VersionComparer.ExtractFromTag(release.TagName, profile.ReleaseTagPrefix);
            var normalized = VersionComparer.Normalize(fromTag);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return VersionComparer.Normalize(release.Name);
    }

    private static GitHubReleaseAssetDto? PickInstallerAsset(
        List<GitHubReleaseAssetDto>? assets,
        string assetPattern)
    {
        if (assets is null || assets.Count == 0)
        {
            return null;
        }

        var exeAssets = assets
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Where(a => a.Name!.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exeAssets.Count == 0)
        {
            return null;
        }

        var pattern = assetPattern.Trim();
        var filtered = string.IsNullOrWhiteSpace(pattern)
            ? exeAssets
            : exeAssets.Where(a => a.Name!.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
        if (filtered.Count == 0)
        {
            filtered = exeAssets;
        }

        return filtered.FirstOrDefault(a => a.Name!.Contains("Setup", StringComparison.OrdinalIgnoreCase))
            ?? filtered[0];
    }
}
