namespace Keirohi_App.Services;

public static class SuicaExportNames
{
    public static string SuggestDocumentBaseName(string? entrySetName)
    {
        var trimmed = entrySetName?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            return SanitizeFileName(trimmed);
        }

        return $"モバイルSuica明細_{DateTime.Today:yyyyMMdd}";
    }

    public static string SanitizeFileName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return "モバイルSuica明細";
        }

        var invalid = Path.GetInvalidFileNameChars();
        Span<char> buffer = stackalloc char[trimmed.Length];
        for (var i = 0; i < trimmed.Length; i++)
        {
            buffer[i] = trimmed.AsSpan(i, 1).IndexOfAny(invalid) >= 0 ? '_' : trimmed[i];
        }

        var result = new string(buffer).TrimEnd('.', ' ');
        return result.Length > 0 ? result : "モバイルSuica明細";
    }
}
