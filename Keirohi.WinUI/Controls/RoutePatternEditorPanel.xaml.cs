using Keirohi_App.Helpers;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Keirohi_App.Controls;

public sealed partial class RoutePatternEditorPanel : UserControl
{
    private readonly RoutePatternDraftService draft = App.RoutePatternDraft;
    private bool suppressNameSync;

    public RoutePatternEditorPanel()
    {
        InitializeComponent();
        draft.EditorWorkingChanged += (_, _) => SyncFromWorkingCopy();
        draft.StructureChanged += (_, _) => SyncFromWorkingCopy();
        draft.SelectionChanged += (_, _) => SyncFromWorkingCopy();
        Loaded += (_, _) =>
        {
            draft.EnsureLoaded();
            SyncFromWorkingCopy();
        };
    }

    private RoutePattern? EditorWorking => draft.EditorWorking;

    private RoutePattern? Selected => draft.Selected;

    private void SyncFromWorkingCopy()
    {
        var working = EditorWorking;
        if (working is null)
        {
            suppressNameSync = true;
            PatternNameBox.Text = "";
            suppressNameSync = false;
            LegsPanel.Children.Clear();
            return;
        }

        suppressNameSync = true;
        PatternNameBox.Text = working.Name;
        suppressNameSync = false;
        RebuildLegCards();
    }

    private void PatternNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (suppressNameSync || EditorWorking is null)
        {
            return;
        }

        EditorWorking.Name = PatternNameBox.Text;
    }

    private void RebuildLegCards()
    {
        LegsPanel.Children.Clear();

        var working = EditorWorking;
        if (working is null)
        {
            return;
        }

        var index = 0;
        foreach (var leg in working.Legs)
        {
            index++;
            LegsPanel.Children.Add(CreateLegCard(index, leg));
        }
    }

    private Border CreateLegCard(int index, RoutePatternLeg leg)
    {
        var card = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = (Thickness)Application.Current.Resources["KeirohiCardBorderThickness"],
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Tag = leg,
        };

        var root = new StackPanel { Spacing = 10 };
        var title = new TextBlock
        {
            Tag = leg,
            Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style,
            Text = FormatLegLabel(index, leg),
            TextWrapping = TextWrapping.WrapWholeWords,
        };
        root.Children.Add(title);

        var body = new Grid { ColumnSpacing = 12 };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });

        var editor = CreateEditorLegCell(leg);
        editor.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(editor, 0);
        body.Children.Add(editor);

        var divider = new Border
        {
            Width = 1,
            Margin = new Thickness(0, 4, 0, 4),
            Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
        };
        Grid.SetColumn(divider, 1);
        body.Children.Add(divider);

        var fare = CreateFareLegSide(leg);
        Grid.SetColumn(fare, 2);
        fare.VerticalAlignment = VerticalAlignment.Top;
        body.Children.Add(fare);

        root.Children.Add(body);
        card.Child = root;
        return card;
    }

    private Grid CreateEditorLegCell(RoutePatternLeg leg)
    {
        var row = new Grid { RowSpacing = 6 };
        row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var stationRow = new Grid { ColumnSpacing = 4 };
        stationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });
        stationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        stationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });

        var depBox = new TextBox
        {
            PlaceholderText = "出発",
            Text = leg.Departure,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        depBox.TextChanged += (_, _) =>
        {
            leg.Departure = depBox.Text;
            UpdateLegCardTitle(leg);
        };
        Grid.SetColumn(depBox, 0);
        stationRow.Children.Add(depBox);

        var arrow = new TextBlock
        {
            Text = "→",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0),
        };
        Grid.SetColumn(arrow, 1);
        stationRow.Children.Add(arrow);

        var arrBox = new TextBox
        {
            PlaceholderText = "到着",
            Text = leg.Arrival,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        arrBox.TextChanged += (_, _) =>
        {
            leg.Arrival = arrBox.Text;
            UpdateLegCardTitle(leg);
        };
        Grid.SetColumn(arrBox, 2);
        stationRow.Children.Add(arrBox);

        var toolRow = new Grid { ColumnSpacing = 8 };
        toolRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });
        toolRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var modeBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 100,
        };
        modeBox.Items.Add(new ComboBoxItem { Content = "電車", Tag = "train" });
        modeBox.Items.Add(new ComboBoxItem { Content = "バス", Tag = "bus" });
        modeBox.SelectedIndex = leg.Mode == TransportMode.Bus ? 1 : 0;
        modeBox.SelectionChanged += (_, _) =>
        {
            if (modeBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                leg.Mode = tag == "bus" ? TransportMode.Bus : TransportMode.Train;
                UpdateLegCardTitle(leg);
            }
        };
        Grid.SetColumn(modeBox, 0);
        toolRow.Children.Add(modeBox);

        var remove = new Button { Content = "この区間を削除", Tag = leg };
        remove.Click += RemoveLeg_Click;
        Grid.SetColumn(remove, 1);
        toolRow.Children.Add(remove);

        Grid.SetRow(stationRow, 0);
        Grid.SetRow(toolRow, 1);
        row.Children.Add(stationRow);
        row.Children.Add(toolRow);

        return row;
    }

    private Grid CreateFareLegSide(RoutePatternLeg leg)
    {
        var grid = new Grid { RowSpacing = 6 };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var amountBox = new TextBox
        {
            PlaceholderText = "運賃（円）· 0=自動推定",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
        };
        amountBox.Text = leg.Amount > 0 ? leg.Amount.ToString() : "";
        amountBox.TextChanged += (_, _) =>
        {
            if (int.TryParse(amountBox.Text.Trim(), out var value) && value > 0)
            {
                leg.Amount = value;
                App.State.RememberFare(leg.Departure, leg.Arrival, value);
            }
            else
            {
                leg.Amount = 0;
            }

            draft.NotifyDraftChanged();
        };
        AmountKeypadAttach.Enable(amountBox);
        Grid.SetRow(amountBox, 0);
        grid.Children.Add(amountBox);

        var search = new Button
        {
            Content = "Yahoo!乗換案内で調べる",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Tag = leg,
        };
        search.Click += SearchLeg_Click;
        Grid.SetRow(search, 1);
        grid.Children.Add(search);

        return grid;
    }

    private void UpdateLegCardTitle(RoutePatternLeg leg)
    {
        var working = EditorWorking;
        if (working is null)
        {
            return;
        }

        var index = working.Legs.IndexOf(leg);
        if (index < 0)
        {
            return;
        }

        foreach (var child in LegsPanel.Children)
        {
            if (child is not Border { Tag: RoutePatternLeg tagged, Child: StackPanel root } ||
                !ReferenceEquals(tagged, leg))
            {
                continue;
            }

            if (root.Children.FirstOrDefault() is TextBlock title && title.Tag is RoutePatternLeg titleLeg &&
                ReferenceEquals(titleLeg, leg))
            {
                title.Text = FormatLegLabel(index + 1, leg);
            }

            return;
        }
    }

    private static string FormatLegLabel(int index, RoutePatternLeg leg)
    {
        var dep = string.IsNullOrWhiteSpace(leg.Departure) ? "（出発）" : leg.Departure.Trim();
        var arr = string.IsNullOrWhiteSpace(leg.Arrival) ? "（到着）" : leg.Arrival.Trim();
        var mode = leg.Mode == TransportMode.Bus ? "バス" : "電車";
        return $"区間 {index} · {dep} → {arr}（{mode}）";
    }

    private async void SearchLeg_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RoutePatternLeg leg })
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(leg.Departure) || string.IsNullOrWhiteSpace(leg.Arrival))
        {
            await ShowDialogAsync("Yahoo!乗換案内", "この区間の出発・到着を入力してください。");
            return;
        }

        await TransitSearchLauncher.OpenYahooTransitAsync(
            XamlRoot,
            leg.Departure,
            leg.Arrival,
            leg.Mode);
    }

    private async Task ShowDialogAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        };
        await dialog.ShowAsync();
    }

    private void RemoveLeg_Click(object sender, RoutedEventArgs e)
    {
        var working = EditorWorking;
        if (working is null || sender is not Button { Tag: RoutePatternLeg leg })
        {
            return;
        }

        working.Legs.Remove(leg);
        if (working.Legs.Count == 0)
        {
            working.Legs.Add(new RoutePatternLeg());
        }

        RebuildLegCards();
        draft.NotifyEditorWorkingLegsChanged();
    }

    private void AddLeg_Click(object sender, RoutedEventArgs e)
    {
        EditorWorking?.Legs.Add(new RoutePatternLeg());
        RebuildLegCards();
        draft.NotifyEditorWorkingLegsChanged();
    }

    private async void CreateReverse_Click(object sender, RoutedEventArgs e)
    {
        var working = EditorWorking;
        if (working is null)
        {
            return;
        }

        var hasLeg = working.Legs.Any(l =>
            !string.IsNullOrWhiteSpace(l.Departure) && !string.IsNullOrWhiteSpace(l.Arrival));
        if (!hasLeg)
        {
            var warn = new ContentDialog
            {
                Title = "逆パターン",
                Content = "区間（出発・到着）が1つ以上必要です。",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot,
            };
            await warn.ShowAsync();
            return;
        }

        RoutePatternReverseHelper.ApplyInPlace(working);
        suppressNameSync = true;
        PatternNameBox.Text = working.Name;
        suppressNameSync = false;
        RebuildLegCards();
        draft.NotifyEditorWorkingLegsChanged();
    }

    private async void SavePatterns_Click(object sender, RoutedEventArgs e)
    {
        var working = EditorWorking;
        var overwriteNames = Selected is not null && working is not null
            ? draft.GetOverwriteSavePatternNames(working, Selected)
            : draft.GetOverwriteSavePatternNames();
        if (overwriteNames.Count > 0)
        {
            var message = BuildOverwriteSaveMessage(overwriteNames);
            var confirm = new ContentDialog
            {
                Title = "パターンの上書き保存",
                Content = message,
                PrimaryButtonText = "上書き保存",
                SecondaryButtonText = "別パターンとして保存",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot,
            };
            var choice = await confirm.ShowAsync();
            if (choice == ContentDialogResult.None)
            {
                return;
            }

            if (choice == ContentDialogResult.Secondary)
            {
                draft.CommitEditorWorking();
                var (splitCount, lastAddedId) = draft.SaveChangedPatternsAsSeparate();
                var count = draft.SaveToDisk(lastAddedId);
                SaveMessageBanner.Show(
                    splitCount == 0
                        ? "保存できるパターンがありません。名称または区間を入力してください。"
                        : $"{splitCount} 件を別パターンとして追加し、合計 {count} 件を保存しました。",
                    InfoBarSeverity.Success);
                return;
            }

            if (choice != ContentDialogResult.Primary)
            {
                return;
            }
        }

        draft.CommitEditorWorking();
        var savedCount = draft.SaveToDisk();
        SaveMessageBanner.Show(
            savedCount == 0
                ? "保存できるパターンがありません。名称または区間を入力してください。"
                : $"{savedCount} 件のパターンを保存しました。",
            InfoBarSeverity.Success);
    }

    private static string BuildOverwriteSaveMessage(IReadOnlyList<string> names)
    {
        if (names.Count == 1)
        {
            return $"「{names[0]}」を上書き保存しますか？\n\n「別パターンとして保存」では、元のパターンはそのまま残し、編集内容を新しいパターンとして追加します。";
        }

        const int maxLines = 12;
        var lines = names.Take(maxLines).Select(n => $"・{n}");
        var body = string.Join('\n', lines);
        if (names.Count > maxLines)
        {
            body += $"\n…他 {names.Count - maxLines} 件";
        }

        return $"{names.Count} 件のパターンを上書き保存しますか？\n\n{body}\n\n「別パターンとして保存」では、元のパターンはそのまま残し、編集内容を新しいパターンとして追加します。";
    }
}
