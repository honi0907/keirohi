using System.Runtime.InteropServices;

namespace Keirohi_App.Updates;

public static class UpdatePlatform
{
    public static bool IsMacOs => OperatingSystem.IsMacOS();

    public static bool IsWindows => OperatingSystem.IsWindows();

    /// <summary>GitHub Release の Mac DMG 名に含まれる RID（osx-x64 / osx-arm64）。</summary>
    public static string MacOsDmgRuntimeSuffix =>
        RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "osx-arm64",
            Architecture.X64 => "osx-x64",
            _ => "osx-x64",
        };

    public static string InstallerNotFoundMessage =>
        IsMacOs
            ? "Release に Mac 用 .dmg がありません。"
            : "Release に Setup .exe がありません。";
}
