namespace MessageAgent.Services;

/// <summary>
/// Closed speech-bubble geometry used by Border.StrokeShape.
/// The tail is part of the same outline, so its fill and stroke remain continuous.
/// </summary>
public sealed class SpeechBubbleShape : IShape
{
    /// <inheritdoc />
    public PathF PathForBounds(Rect bounds)
    {
        float width = (float)Math.Max(1, bounds.Width);
        float height = (float)Math.Max(1, bounds.Height);
        float radius = Math.Min(10f, Math.Min(width, height) * 0.22f);
        float tailHeight = Math.Min(7f, height * 0.22f);
        float tailBase = Math.Min(18f, width * 0.22f);
        float bodyBottom = Math.Max(radius, height - tailHeight);

        PathF path = new();
        path.MoveTo(radius, 0);
        path.LineTo(width - radius, 0);
        path.QuadTo(width, 0, width, radius);
        path.LineTo(width, bodyBottom - radius);
        path.QuadTo(width, bodyBottom, width - radius, bodyBottom);
        path.LineTo(tailBase + 7f, bodyBottom);
        path.LineTo(4f, height);
        path.LineTo(7f, bodyBottom);
        path.LineTo(radius, bodyBottom);
        path.QuadTo(0, bodyBottom, 0, bodyBottom - radius);
        path.LineTo(0, radius);
        path.QuadTo(0, 0, radius, 0);
        path.Close();

        return path;
    }
}
