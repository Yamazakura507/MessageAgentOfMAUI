using LottieAgent.Models;
using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Resources.ExtensionResources;

namespace MessageAgent.Controls.Lists;

public partial class MessageTypeChipView : ContentView
{
    public event EventHandler? Selected;

    public MessageTypeChipView() => InitializeComponent();

    public void Configure(MessageType type, int count, bool selected, double width)
    {
        Color accent = type.AccentColor;
        Animation.Source = string.IsNullOrWhiteSpace(type.NotificationAnimationSource)
            ? MessageAgentResources.GetString(MessageAgentResources.DefaultNotificationLottieKey)
            : type.NotificationAnimationSource;
        Animation.Modifiers = type.NotificationAnimationModifiers ?? new LottieModifierCollection();
        Animation.AnimationLoaded += (_, _) =>
        {
            Animation.IsPlaying = false;
            Animation.Progress = 1;
        };

        CountText.Text = count.ToString();
        CountText.TextColor = accent;
        CountBadge.BackgroundColor = accent.WithAlpha(
            (float)MessageAgentResources.GetDouble(MessageAgentResources.TypeCountBadgeOpacityKey));

        TypeName.Text = type.DisplayName ?? type.Name;
        TypeName.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        TypeName.TextColor = accent;

        ChipBorder.WidthRequest = width;
        ChipBorder.HorizontalOptions = width < 0 ? LayoutOptions.Fill : LayoutOptions.Start;
        ChipBorder.Stroke = selected
            ? accent
            : accent.WithAlpha((float)MessageAgentResources.GetDouble(MessageAgentResources.TypeChipUnselectedStrokeOpacityKey));
        ChipBorder.StrokeThickness = (float)MessageAgentResources.GetDouble(selected
            ? MessageAgentResources.TypeChipSelectedStrokeThicknessKey
            : MessageAgentResources.TypeChipUnselectedStrokeThicknessKey);
        ChipBorder.BackgroundColor = selected
            ? accent.WithAlpha((float)MessageAgentResources.GetDouble(MessageAgentResources.TypeChipSelectedBackgroundOpacityKey))
            : Colors.Transparent;

        ToolTipProperties.SetText(ChipBorder, $"{type.DisplayName ?? type.Name}: {count}");
        ChipBorder.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => Selected?.Invoke(this, EventArgs.Empty))
        });
    }
}