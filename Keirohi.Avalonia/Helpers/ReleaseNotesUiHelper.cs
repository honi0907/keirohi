using Avalonia.Threading;
using Keirohi_App.Updates;
using Keirohi.Avalonia.Windows;

namespace Keirohi.Avalonia.Helpers;

public static class ReleaseNotesUiHelper
{
    public static async Task PromptOnStartupAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var owner = AppHost.MainWindowInstance;
            if (owner is null)
            {
                return;
            }

            var current = AppVersionReader.GetCurrentVersion();
            var ack = AppHost.State.Settings.ReleaseNotesAcknowledgedVersion;
            if (!ReleaseNotesCatalog.ShouldPromptOnStartup(current, ack))
            {
                return;
            }

            var entry = ReleaseNotesCatalog.GetForVersion(current);
            if (entry is null)
            {
                return;
            }

            var result = await new WhatsNewWindow(entry, current).ShowDialog<WhatsNewCloseResult>(owner);
            if (result == WhatsNewCloseResult.DoNotShowAgain)
            {
                AppHost.State.AcknowledgeReleaseNotes(current);
            }
        });
    }
}
