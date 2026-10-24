namespace Keirohi_App.Updates;

public static class PackagedAppDetector
{
    /// <summary>開発ビルド（bin/obj）では自己更新を適用しない。</summary>
    public static bool CanApplyOnlineUpdate()
    {
        var dir = AppContext.BaseDirectory;
        if (dir.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || dir.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (dir.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || dir.Contains(@"\obj\", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
