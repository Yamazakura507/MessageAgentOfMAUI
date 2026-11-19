using MessageAgent.Core.Models;

namespace MessageAgent.Services.Models;

/// <summary>
/// Describes how one status is presented in the compact message label.
/// The title can be suppressed independently from the status visual.
/// </summary>
public sealed record StatusPresentationItem(Status Status, bool ShowTitle);
