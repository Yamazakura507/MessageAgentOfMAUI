using System.Runtime.CompilerServices;
using MessageAgent.Core.Models;

namespace MessageAgent.Services;

/// <summary>
/// Keeps one random color per Status object, shared between the label and details views.
/// Weak keys allow colors to be collected together with their status models.
/// </summary>
public static class StatusRandomColorStore
{
    private sealed class ColorHolder(Color color)
    {
        public Color Value { get; } = color;
    }

    private static readonly ConditionalWeakTable<Status, ColorHolder> ColorsByStatus = new();

    public static Color GetOrCreate(Status status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return ColorsByStatus.GetValue(status, static _ => new ColorHolder(Color.FromHsla(Random.Shared.NextDouble(), 0.72, 0.45))).Value;
    }

    public static void Reset(Status status)
    {
        ArgumentNullException.ThrowIfNull(status);

        ColorsByStatus.Remove(status);
    }
}
