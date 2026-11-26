namespace MessageAgent.Core.Models.Dialogs.Structs
{
    internal readonly record struct TextNoiseLine(
        float StartX,
        float StartY,
        float EndX,
        float EndY,
        float StrokeWidth,
        bool DrawOverText,
        byte Alpha);
}
