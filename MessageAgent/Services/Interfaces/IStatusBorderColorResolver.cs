using MessageAgent.Core.Models;

namespace MessageAgent.Services.Interfaces;

/// <summary>
/// Resolves a representative color from a status visual value.
/// Returns null when the value has no extractable color.
/// </summary>
public interface IStatusBorderColorResolver
{
    ValueTask<Color?> ResolveVisualColorAsync(Status status, CancellationToken cancellationToken = default);
}
