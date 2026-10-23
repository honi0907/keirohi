using Keirohi_App.Updates;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Keirohi_App.Services;

public static class OnlineUpdateUiHelper
{
    private static readonly OnlineUpdateCoordinator Coordinator = new();
    private static int busy;

    public static Task PromptOnStartupAsync(XamlRoot? xamlRoot) =>
        RunAsync(xamlRoot, _ => { }, promptOnlyWhenAvailable: true);

    public static Task RunFromSettingsAsync(XamlRoot? xamlRoot, Action<string> setStatus) =>
        RunAsync(xamlRoot, setStatus, promptOnlyWhenAvailable: false);

    private static async Task RunAsync(
        XamlRoot? xamlRoot,
        Action<string> setStatus,
        bool promptOnlyWhenAvailable)
    {
        if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
        {
            setStatus("更新処理が既に実行中です。");
            return;
        }

        var dispatcher = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("DispatcherQueue が取得できません。");

        void SetStatusSafe(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            RunOnUi(dispatcher, () => setStatus(message));
        }

        try
        {
            if (xamlRoot is null)
            {
                SetStatusSafe("画面の準備が完了していません。");
                return;
            }

            var progress = new Progress<OnlineUpdateProgress>(p => SetStatusSafe(p.Message));
            var result = await Coordinator.RunAsync(
                message => ConfirmOnUiAsync(dispatcher, xamlRoot, message),
                progress,
                () => RunOnUi(dispatcher, () => Application.Current.Exit()),
                promptOnlyWhenAvailable,
                AppReleaseProfile.WinUi).ConfigureAwait(false);

            SetStatusSafe(result.Message);
        }
        catch (Exception ex)
        {
            SetStatusSafe($"更新に失敗しました: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref busy, 0);
        }
    }

    private static void RunOnUi(DispatcherQueue dispatcher, Action action)
    {
        if (dispatcher.HasThreadAccess)
        {
            action();
            return;
        }

        _ = dispatcher.TryEnqueue(() => action());
    }

    private static Task<bool> ConfirmOnUiAsync(DispatcherQueue dispatcher, XamlRoot xamlRoot, string message)
    {
        if (dispatcher.HasThreadAccess)
        {
            return ConfirmAsync(xamlRoot, message);
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dispatcher.TryEnqueue(() => _ = CompleteConfirmAsync(tcs, xamlRoot, message)))
        {
            tcs.TrySetResult(false);
        }

        return tcs.Task;
    }

    private static async Task CompleteConfirmAsync(TaskCompletionSource<bool> tcs, XamlRoot xamlRoot, string message)
    {
        try
        {
            tcs.TrySetResult(await ConfirmAsync(xamlRoot, message));
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }

    private static async Task<bool> ConfirmAsync(XamlRoot xamlRoot, string message)
    {
        var dialog = new ContentDialog
        {
            Title = "新しいバージョン",
            Content = message,
            PrimaryButtonText = "更新して再起動",
            CloseButtonText = "あとで",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
