using System.ComponentModel;
using LottieAgent.Models;
using MessageAgent.Resources.ExtensionResources;

namespace MessageAgent.Core.Models.ExtentionKinds;

/// <summary>
/// Resource-backed message category. The built-in values are defaults; applications
/// can define additional instances in their own ResourceDictionary.
/// </summary>
[TypeConverter(typeof(MessageTypeConverter))]
public sealed class MessageType
{
    private static readonly string DefaultNotificationAnimationSource =
        MessageAgentResources.GetString(MessageAgentResources.DefaultNotificationLottieKey);
    private const string DefaultNotificationAnimationTargetName = "info contornos";

    /// <summary>
    /// Gets the built-in alert message type.
    /// </summary>
    public static MessageType Alert { get; } = CreateBuiltIn(
        "Alert",
        MessageAgentResources.GetColor(MessageAgentResources.AlertAccentKey),
        MessageAgentResources.GetColor(MessageAgentResources.AlertBackgroundKey),
        "Предупреждение");

    /// <summary>
    /// Gets the built-in error message type.
    /// </summary>
    public static MessageType Error { get; } = CreateBuiltIn(
        "Error",
        MessageAgentResources.GetColor(MessageAgentResources.ErrorAccentKey),
        MessageAgentResources.GetColor(MessageAgentResources.ErrorBackgroundKey),
        "Ошибка");

    /// <summary>
    /// Gets the built-in informational message type.
    /// </summary>
    public static MessageType Info { get; } = CreateBuiltIn(
        "Info",
        MessageAgentResources.GetColor(MessageAgentResources.InfoAccentKey),
        MessageAgentResources.GetColor(MessageAgentResources.InfoBackgroundKey),
        "Информация");

    /// <summary>
    /// Initializes an application-defined message type with default visual values.
    /// </summary>
    public MessageType() : this("Custom", MessageAgentResources.GetColor(MessageAgentResources.InfoAccentKey), null, null)
    {

    }

    /// <summary>
    /// Initializes a message type with its name and color palette.
    /// </summary>
    /// <param name="name">A stable application-defined type identifier.</param>
    /// <param name="accentColor">The primary accent color.</param>
    /// <param name="backgroundColor">An optional label background color.</param>
    /// <param name="displayName">An optional localized display name.</param>
    public MessageType(string name, Color accentColor, Color? backgroundColor = null, string? displayName = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Custom" : name;
        AccentColor = accentColor;
        BackgroundColor = backgroundColor;
        DisplayName = displayName;
        NotificationAnimationSource = DefaultNotificationAnimationSource;
    }

    /// <summary>
    /// Stable application-defined identifier.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Optional localized name shown in the details dialog.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Main accent used by the label, status badges and actions.
    /// </summary>
    public Color AccentColor { get; set; }

    /// <summary>
    /// Optional label fill. When omitted, a translucent accent is used.
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    /// Optional explicit dialog surface; otherwise it is derived from AccentColor.
    /// </summary>
    public Color? DialogSurfaceColor { get; set; }

    /// <summary>
    /// Optional dialog foreground; otherwise a contrast-safe color is derived.
    /// </summary>
    public Color? DialogForegroundColor { get; set; }

    /// <summary>
    /// Optional dialog secondary foreground; otherwise a contrast-safe color is derived.
    /// </summary>
    public Color? DialogSecondaryForegroundColor { get; set; }

    /// <summary>
    /// Optional dialog accent; otherwise a contrast-safe accent is derived.
    /// </summary>
    public Color? DialogAccentColor { get; set; }

    /// <summary>
    /// Optional text color for controls filled with AccentColor.
    /// </summary>
    public Color? AccentContentColor { get; set; }

    /// <summary>
    /// Packaged Lottie asset used for the notification icon and details header.
    /// </summary>
    public string? NotificationAnimationSource { get; set; }

    /// <summary>
    /// Optional collection of overrides for the notification animation. Add one modifier
    /// for each named Lottie element that needs a different value.
    /// </summary>
    public LottieModifierCollection? NotificationAnimationModifiers { get; set; }

    /// <summary>
    /// Returns the explicitly configured modifiers. Custom message types do not assume
    /// any layer names because those names belong to the selected Lottie asset.
    /// </summary>
    internal LottieModifierCollection GetEffectiveNotificationAnimationModifiers() => NotificationAnimationModifiers ?? new LottieModifierCollection();

    private static MessageType CreateBuiltIn(string name, Color accentColor, Color backgroundColor, string displayName)
    {
        MessageType type = new(name, accentColor, backgroundColor, displayName)
        {
            NotificationAnimationModifiers = 
                new LottieModifierCollection().SetColor(DefaultNotificationAnimationTargetName, accentColor)
        };

        return type;
    }
}
