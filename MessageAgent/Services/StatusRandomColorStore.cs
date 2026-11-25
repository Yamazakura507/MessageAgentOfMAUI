using System.Runtime.CompilerServices;
using MessageAgent.Core.Models;
using MessageAgent.Resources.ExtensionResources;

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

    /// <summary>
    /// Gets the existing random color for a status or generates one on first use.
    /// </summary>
    /// <param name="status">The status used as the weak-keyed cache key.</param>
    /// <returns>The stable random color associated with the status instance.</returns>
    public static Color GetOrCreate(this Status status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return ColorsByStatus.GetValue(status, static _ => new ColorHolder(MessageAgentResources.CreateRandomAccentColor())).Value;
    }

    /// <summary>
    /// Removes the cached random color so a new one is generated on next access.
    /// </summary>
    /// <param name="status">The status whose color should be reset.</param>
    public static void Reset(this Status status)
    {
        ArgumentNullException.ThrowIfNull(status);

        ColorsByStatus.Remove(status);
    }
}
