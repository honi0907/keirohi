using Avalonia.Controls;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi.Avalonia.Helpers;

namespace Keirohi.Avalonia.Controls;

public partial class RoutePatternApplyPanel : UserControl
{
    private readonly RoutePatternDraftService draft = AppHost.RoutePatternDraft;

    public RoutePatternApplyPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => IsoDateHelper.EnsureTodayIfEmpty(ApplyDatePicker);
        draft.SelectionChanged += (_, _) => SyncDestinationFromSelection();
    }

    private void SyncDestinationFromSelection()
    {
        if (string.IsNullOrWhiteSpace(ApplyDestinationBox.Text) && draft.Selected is not null)
        {
            ApplyDestinationBox.Text = draft.Selected.Name.Trim();
        }
    }

    public async Task<string?> TryApplyToEntriesAsync()
    {
        draft.EnsureLoaded();
        draft.CommitEditorWorking();
        var pattern = draft.Selected;
        if (pattern is null)
        {
            await DialogService.ShowOkAsync("パターン", "左の一覧でパターンを選択してください。");
            return null;
        }

        if (string.IsNullOrWhiteSpace(pattern.Name))
        {
            await DialogService.ShowOkAsync("パターン", "パターン名が空です。名称を入力してください。");
            return null;
        }

        var date = IsoDateHelper.ToIsoString(ApplyDatePicker.SelectedDate);
        if (string.IsNullOrEmpty(date))
        {
            await DialogService.ShowOkAsync("入力エラー", "日付を選択してください。");
            return null;
        }

        if (await SuicaEntryLimitHelper.BlockIfAtLimitWithExportOfferAsync())
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
            AppHost.State.Settings,
            AppHost.State.GetFareIndex());

        if (expanded.Entries.Count == 0)
        {
            var missing = expanded.UnresolvedLegs.Count > 0
                ? string.Join("、", expanded.UnresolvedLegs)
                : "（区間が未入力）";
            await DialogService.ShowOkAsync(
                "パターン",
                $"登録できる区間がありません。運賃が分からない区間: {missing}");
            return null;
        }

        if (expanded.Entries.Count > room)
        {
            var confirm = await DialogService.ShowConfirmAsync(
                "登録件数の上限",
                $"このパターンは{expanded.Entries.Count}件ですが、あと{room}件しか登録できません。先頭{room}件だけ追加します。",
                $"{room}件だけ追加",
                "キャンセル");
            if (!confirm)
            {
                return null;
            }
        }

        var countBefore = AppHost.State.Entries.Count;
        var added = AppHost.State.AddEntries(expanded.Entries);
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
            && AppHost.State.Entries.Count >= AppStateService.MaxSuicaEntries)
        {
            await SuicaEntryLimitHelper.OfferExportAfterFillAsync(
                "54件に達しました。出力してから新しいセットで続けてください。");
        }

        return message;
    }
}
