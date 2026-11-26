namespace MessageAgent.Core.Models.Dialogs.Structs
{
    internal readonly record struct TextNoiseDot(
        float X,
        float Y,
        float Radius,
        bool DrawOverText,
        byte Alpha);
}
