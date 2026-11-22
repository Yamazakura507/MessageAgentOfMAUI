using MessageAgent.Core.Models;

namespace MessageAgent.Services.Interfaces;

/// <summary>
/// Resolves a representative color from a status visual value.
/// Returns null when the value has no extractable color.
/// </summary>
public interface IStatusBorderColorResolver
{
    /// <summary>
    /// Attempts to resolve a representative color from the status visual value.
    /// </summary>
    /// <param name="status">The status containing the visual value.</param>
    /// <param name="cancellationToken">A token that can cancel asynchronous asset reads.</param>
    /// <returns>The extracted color, or null when no color can be resolved.</returns>
    ValueTask<Color?> ResolveVisualColorAsync(Status status, CancellationToken cancellationToken = default);
}
