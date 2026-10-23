using Avalonia.Threading;
using Keirohi_App.Updates;
using Keirohi.Avalonia.Helpers;

namespace Keirohi.Avalonia.Services;

public static class OnlineUpdateUiHelper
{
    private static readonly OnlineUpdateCoordinator Coordinator = new();
    private static int busy;

    public static Task PromptOnStartupAsync() =>
        RunAsync(_ => { }, promptOnlyWhenAvailable: true);

    public static Task RunFromSettingsAsync(Action<string> setStatus) =>
        RunAsync(setStatus, promptOnlyWhenAvailable: false);

    private static async Task RunAsync(Action<string> setStatus, bool promptOnlyWhenAvailable)
    {
        if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
        {
            setStatus("更新処理が既に実行中です。");
            return;
        }

        try
        {
            var progress = new Progress<OnlineUpdateProgress>(p =>
            {
                if (!string.IsNullOrEmpty(p.Message))
                {
                    RunOnUi(() => setStatus(p.Message));
                }
            });

            var result = await Coordinator.RunAsync(
                message => DialogService.ShowConfirmAsync("新しいバージョン", message, "更新して再起動", "あとで"),
                progress,
                () => RunOnUi(() => Environment.Exit(0)),
                promptOnlyWhenAvailable,
                AppReleaseProfile.Avalonia).ConfigureAwait(false);

            RunOnUi(() => setStatus(result.Message));
        }
        catch (Exception ex)
        {
            RunOnUi(() => setStatus($"更新に失敗しました: {ex.Message}"));
        }
        finally
        {
            Interlocked.Exchange(ref busy, 0);
        }
    }

    private static void RunOnUi(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action);
    }
}
