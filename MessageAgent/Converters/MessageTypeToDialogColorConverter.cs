using System.Globalization;
using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Services.Extensions;

namespace MessageAgent.Converters;

/// <summary>
/// Selects a contrast-aware dialog color. Supported parameters:
/// Surface, Foreground, SecondaryForeground, Accent and AccentContent.
/// </summary>
public sealed class MessageTypeToDialogColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        MessageType type = value is MessageType messageType ? messageType : MessageType.Info;
        string mode = parameter?.ToString() ?? "Surface";
        AppTheme theme = Application.Current?.RequestedTheme ?? AppTheme.Light;

        return mode switch
        {
            "Surface" => type.GetDialogSurface(theme),
            "Foreground" => type.GetDialogForeground(theme),
            "SecondaryForeground" => type.GetDialogSecondaryForeground(theme),
            "Accent" => type.GetDialogAccent(theme),
            "AccentContent" => type.GetAccentContent(),
            _ => type.GetDialogSurface(theme)
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
