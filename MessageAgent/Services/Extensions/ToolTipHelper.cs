namespace MessageAgent.Services.Extensions;

/// <summary>
/// Applies a MAUI tooltip and, on Windows, mirrors it to the native WinUI element.
/// </summary>
internal static class ToolTipHelper
{
    public static void SetText(VisualElement element, string? text)
    {
        ArgumentNullException.ThrowIfNull(element);

        string value = text ?? string.Empty;

        ToolTipProperties.SetText(element, value);

        #if WINDOWS
        if (element.Handler?.PlatformView is Microsoft.UI.Xaml.DependencyObject nativeElement)
        {
            Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(nativeElement, string.IsNullOrWhiteSpace(value) ? null : value);
        }
        #endif
    }
}
