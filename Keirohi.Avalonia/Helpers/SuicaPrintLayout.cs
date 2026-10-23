using Keirohi_App.Services;

namespace Keirohi.Avalonia.Helpers;

internal readonly record struct SuicaPrintLayout(
    float ImageScale,
    float OffsetXmm,
    float OffsetYmm,
    int MarginLeftHundredthsInch,
    int MarginRightHundredthsInch,
    int MarginTopHundredthsInch,
    int MarginBottomHundredthsInch)
{
    public static SuicaPrintLayout FromSettings()
    {
        var saved = AppHost.State.Settings;
        return new SuicaPrintLayout(
            Math.Clamp(saved.PrintImageScale <= 0 ? 1f : saved.PrintImageScale, 0.4f, 1.6f),
            saved.PrintOffsetXmm,
            saved.PrintOffsetYmm,
            saved.PrintMarginLeft,
            saved.PrintMarginRight,
            saved.PrintMarginTop,
            saved.PrintMarginBottom);
    }

    public void Persist()
    {
        var settings = AppHost.State.Settings;
        settings.PrintImageScale = ImageScale;
        settings.PrintOffsetXmm = OffsetXmm;
        settings.PrintOffsetYmm = OffsetYmm;
        settings.PrintMarginLeft = MarginLeftHundredthsInch;
        settings.PrintMarginRight = MarginRightHundredthsInch;
        settings.PrintMarginTop = MarginTopHundredthsInch;
        settings.PrintMarginBottom = MarginBottomHundredthsInch;
        AppHost.State.UpdateSettings(settings);
    }
}
