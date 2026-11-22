using System.ComponentModel;
using System.Runtime.CompilerServices;
using MessageAgent.Core.Enums.Status;
using MessageAgent.Core.Models.VisualValues.Abstract;
using MessageAgent.Services;

namespace MessageAgent.Core.Models;

/// <summary>
/// A named message status with a pluggable visual value. The nested
/// <see cref="Value"/> element is the content property in MAUI XAML.
/// </summary>
[ContentProperty(nameof(Value))]
public sealed record Status : INotifyPropertyChanged
{
    private string name = string.Empty;
    private int sortOrder;
    private VisualValue? value;
    private bool showTitle;
    private string? description;
    private StatusBorderColorMode borderColorMode = StatusBorderColorMode.Label;

    /// <summary>
    /// Gets or sets the status name.
    /// </summary>
    public string Name
    {
        get => name;
        set => SetField(ref name, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the display order. Lower values appear first when no explicit sorting
    /// rules are configured. Equal values preserve their current order.
    /// </summary>
    public int SortOrder
    {
        get => sortOrder;
        set => SetField(ref sortOrder, value);
    }

    /// <summary>
    /// Gets or sets the image, Lottie or custom element shown for this status.
    /// </summary>
    public VisualValue? Value
    {
        get => value;
        set => SetField(ref this.value, value);
    }

    /// <summary>
    /// Gets or sets whether the status name is shown next to its visual value.
    /// </summary>
    public bool ShowTitle
    {
        get => showTitle;
        set => SetField(ref showTitle, value);
    }

    /// <summary>
    /// Gets or sets the optional explanatory text for the status.
    /// </summary>
    public string? Description
    {
        get => description;
        set => SetField(ref description, value);
    }

    /// <summary>
    /// Gets or sets the color source for this status outline and title.
    /// Changing the mode invalidates its cached random color.
    /// </summary>
    public StatusBorderColorMode BorderColorMode
    {
        get => borderColorMode;
        set
        {
            if (borderColorMode == value) return;

            borderColorMode = value;

            this.Reset();

            if (value == StatusBorderColorMode.Random) this.GetOrCreate();
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes an empty status for XAML object construction.
    /// </summary>
    public Status() { }

    /// <summary>
    /// Initializes a status with its main display properties.
    /// </summary>
    public Status(
        string name,
        VisualValue? value = null,
        string? description = null,
        bool showTitle = false,
        StatusBorderColorMode borderColorMode = StatusBorderColorMode.Label,
        int sortOrder = 0)
    {
        this.name = name ?? string.Empty;
        this.value = value;
        this.description = description;
        this.showTitle = showTitle;
        this.borderColorMode = borderColorMode;
        this.sortOrder = sortOrder;

        if (borderColorMode == StatusBorderColorMode.Random) StatusRandomColorStore.GetOrCreate(this);
    }

    private bool SetField<TValue>(ref TValue field, TValue value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TValue>.Default.Equals(field, value)) return false;

        field = value;

        OnPropertyChanged(propertyName);

        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
