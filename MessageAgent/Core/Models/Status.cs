using System.ComponentModel;
using MessageAgent.Core.Enums;
using MessageAgent.Services;

namespace MessageAgent.Core.Models;

/// <summary>
/// A named message status with a pluggable visual value.
/// The nested ValueStatus instance is the element content in MAUI XAML.
/// </summary>
[ContentProperty(nameof(Value))]
public sealed record Status : INotifyPropertyChanged
{
    private StatusBorderColorMode borderColorMode = StatusBorderColorMode.Label;
    private int sortOrder;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Determines the display order. Lower values are displayed first.
    /// Equal values preserve their insertion order in the collection.
    /// </summary>
    public int SortOrder
    {
        get => sortOrder;
        set
        {
            if (sortOrder == value) return;

            sortOrder = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortOrder)));
        }
    }
    public ValueStatus? Value { get; set; }
    public bool IsImportant { get; set; }
    public bool ShowTitle { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Selects the color source for this status' outline and title.
    /// Changing the mode invalidates its cached random color.
    /// </summary>
    public StatusBorderColorMode BorderColorMode
    {
        get => borderColorMode;
        set
        {
            if (borderColorMode == value) return;

            borderColorMode = value;
            StatusRandomColorStore.Reset(this);

            if (value == StatusBorderColorMode.Random) StatusRandomColorStore.GetOrCreate(this);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BorderColorMode)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Status() { }

    public Status(
        string name,
        ValueStatus? value = null,
        bool isImportant = false,
        string? description = null,
        bool showTitle = false,
        StatusBorderColorMode borderColorMode = StatusBorderColorMode.Label,
        int sortOrder = 0)
    {
        Name = name;
        Value = value;
        IsImportant = isImportant;
        Description = description;
        ShowTitle = showTitle;
        this.borderColorMode = borderColorMode;
        this.sortOrder = sortOrder;

        if (borderColorMode == StatusBorderColorMode.Random) StatusRandomColorStore.GetOrCreate(this);
    }
}
