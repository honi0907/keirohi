namespace Keirohi_App.Helpers;

public static class DataPersistenceStatusHelper
{
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Keirohi");

    public static string FormatLastSavedLabel(DateTimeOffset? at) =>
        at is null
            ? "保存: —"
            : $"保存: {at.Value.LocalDateTime:yyyy/MM/dd HH:mm:ss}";

    public static string DataFolderToolTip =>
        $"データ保存先: {DataDirectory}";
}
