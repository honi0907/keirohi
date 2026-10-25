using Avalonia.Controls;
using Avalonia.Threading;
using Keirohi_App.Updates;
using Keirohi.Avalonia.Helpers;
using Keirohi.Avalonia.Windows;

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

        OnlineUpdateProgressWindow? progressWindow = null;
        try
        {
            var progress = new Progress<OnlineUpdateProgress>(p =>
            {
                RunOnUi(() =>
                {
                    if (!string.IsNullOrEmpty(p.Message))
                    {
                        setStatus(p.Message);
                    }

                    if (!OnlineUpdateProgressWindow.ShouldShowForProgress(p))
                    {
                        return;
                    }

                    progressWindow ??= ShowProgressWindow();
                    progressWindow?.Apply(p);
                });
            });

            var confirmPrimary = UpdatePlatform.IsMacOs ? "DMG を開く" : "更新して再起動";
            var result = await Coordinator.RunAsync(
                message => DialogService.ShowConfirmAsync("新しいバージョン", message, confirmPrimary, "あとで"),
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
            RunOnUi(() =>
            {
                if (progressWindow is not null)
                {
                    progressWindow.Close();
                    progressWindow = null;
                }
            });
            Interlocked.Exchange(ref busy, 0);
        }
    }

    private static OnlineUpdateProgressWindow? ShowProgressWindow()
    {
        var owner = DialogService.Owner ?? AppHost.MainWindowInstance;
        if (owner is null)
        {
            return null;
        }

        var window = new OnlineUpdateProgressWindow();
        window.Show(owner);
        return window;
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
