#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace MessageAgent.Services.Extensions;

/// <summary>
/// Applies a MAUI tooltip and, on Windows, mirrors it to the native WinUI element.
/// </summary>
internal static class ToolTipHelper
{
    /// <summary>
    /// Sets or clears a platform tooltip on the supplied visual element.
    /// </summary>
    /// <param name="element">The visual element receiving the tooltip.</param>
    /// <param name="text">The tooltip text, or null to clear the tooltip.</param>
    public static void SetText(VisualElement element, string? text)
    {
        ArgumentNullException.ThrowIfNull(element);

        string value = text ?? string.Empty;

        ToolTipProperties.SetText(element, value);

        #if WINDOWS
        if (element.Handler?.PlatformView is DependencyObject nativeElement)
        {
            ToolTipService.SetToolTip(nativeElement, string.IsNullOrWhiteSpace(value) ? null : value);
        }
        #endif
    }
}
