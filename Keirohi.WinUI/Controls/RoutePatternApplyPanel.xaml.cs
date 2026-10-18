using Keirohi_App.Helpers;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Keirohi_App.Controls;

public sealed partial class RoutePatternApplyPanel : UserControl
{
    private readonly RoutePatternDraftService draft = App.RoutePatternDraft;

    public RoutePatternApplyPanel()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            IsoDateHelper.EnsureTodayIfEmpty(ApplyDatePicker);
        };
        draft.SelectionChanged += (_, _) => SyncDestinationFromSelection();
    }

    private void SyncDestinationFromSelection()
    {
        if (string.IsNullOrWhiteSpace(ApplyDestinationBox.Text) && draft.Selected is not null)
        {
            ApplyDestinationBox.Text = draft.Selected.Name.Trim();
        }
    }

    /// <summary>右列の「明細に登録」から呼ばれる。成功時は表示用メッセージ、失敗時は null。</summary>
    public async Task<string?> TryApplyToEntriesAsync()
    {
        draft.EnsureLoaded();
        draft.CommitEditorWorking();
        var pattern = draft.Selected;
        if (pattern is null)
        {
            await ShowDialogAsync("パターン", "左の一覧でパターンを選択してください。");
            return null;
        }

        if (string.IsNullOrWhiteSpace(pattern.Name))
        {
            await ShowDialogAsync("パターン", "パターン名が空です。名称を入力してください。");
            return null;
        }

        var date = IsoDateHelper.ToIsoString(ApplyDatePicker.Date);
        if (string.IsNullOrEmpty(date))
        {
            await ShowDialogAsync("入力エラー", "日付を選択してください。");
            return null;
        }

        if (await SuicaEntryLimitHelper.BlockIfAtLimitWithExportOfferAsync(XamlRoot))
        {
            return null;
        }

        var room = SuicaEntryLimitHelper.Remaining;

        var destination = string.IsNullOrWhiteSpace(ApplyDestinationBox.Text)
            ? pattern.Name.Trim()
            : ApplyDestinationBox.Text.Trim();

        var patternForExpand = RoutePatternDraftService.ClonePattern(pattern);
        var expanded = await RoutePatternExpander.ExpandAsync(
            patternForExpand,
            date,
            destination,
            App.State.Settings,
            App.State.GetFareIndex());

        if (expanded.Entries.Count == 0)
        {
            var missing = expanded.UnresolvedLegs.Count > 0
                ? string.Join("、", expanded.UnresolvedLegs)
                : "（区間が未入力）";
            await ShowDialogAsync(
                "パターン",
                $"登録できる区間がありません。運賃が分からない区間: {missing}");
            return null;
        }

        if (expanded.Entries.Count > room)
        {
            var confirm = new ContentDialog
            {
                Title = "登録件数の上限",
                Content =
                    $"このパターンは{expanded.Entries.Count}件ですが、あと{room}件しか登録できません。先頭{room}件だけ追加します。",
                PrimaryButtonText = $"{room}件だけ追加",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot,
            };
            if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            {
                return null;
            }
        }

        var countBefore = App.State.Entries.Count;
        var added = App.State.AddEntries(expanded.Entries);
        ApplyDestinationBox.Text = destination;

        var message = $"{added}件を一覧に追加しました。";
        if (expanded.UnresolvedLegs.Count > 0)
        {
            message += $" 運賃不明のためスキップ: {string.Join("、", expanded.UnresolvedLegs)}";
        }

        if (added < expanded.Entries.Count)
        {
            message += $" {AppStateService.MaxSuicaEntries}件上限のため一部は追加されませんでした。";
        }

        if (countBefore < AppStateService.MaxSuicaEntries
            && App.State.Entries.Count >= AppStateService.MaxSuicaEntries)
        {
            await SuicaEntryLimitHelper.OfferExportAfterFillAsync(
                XamlRoot,
                "54件に達しました。出力してから新しいセットで続けてください。");
        }

        return message;
    }

    private static async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = App.MainWindowInstance!.Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }
}
