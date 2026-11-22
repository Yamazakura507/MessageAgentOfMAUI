namespace MessageAgent.Core.Enums.Status;

/// <summary>
/// Fields available for sorting a <see cref="Models.Status"/> collection.
/// </summary>
public enum StatusSortParametr
{
    /// <summary>
    /// Explicit display order, used by default when no rules are configured.
    /// </summary>
    SortOrder,
    /// <summary>
    /// Status name.
    /// </summary>
    Name,
    /// <summary>
    /// Whether the status title is displayed.
    /// </summary>
    ShowTitle,
    /// <summary>
    /// Optional status description.
    /// </summary>
    Description,
    /// <summary>
    /// Border color selection mode.
    /// </summary>
    BorderColorMode,
    /// <summary>
    /// Runtime type name of the assigned visual value.
    /// </summary>
    VisualType
}
