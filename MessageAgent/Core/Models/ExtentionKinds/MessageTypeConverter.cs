using System.ComponentModel;
using System.Globalization;

namespace MessageAgent.Core.Models.ExtentionKinds;

/// <summary>
/// Resolves Type="Error" against application resources first, then falls back to
/// the library defaults. A ResourceDictionary can override or add message types.
/// </summary>
public sealed class MessageTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

    /// <inheritdoc />
    public override MessageType ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string name) throw new NotSupportedException($"Cannot convert {value.GetType().Name} to a message type.");

        return Resolve(name);
    }

    /// <summary>
    /// Resolves an application resource named MessageType.{name}, falling back
    /// to the built-in Error, Alert and Info definitions.
    /// </summary>
    public static MessageType Resolve(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new FormatException("A message type name cannot be empty.");

        name = name.Trim();

        ResourceDictionary? resources = Application.Current?.Resources;

        if (resources is not null 
            && TryFindResource(resources, $"MessageType.{name}", out object resource) 
            && resource is MessageType resourceType)
        {
            return resourceType;
        }

        return name.ToUpperInvariant() switch
        {
            "ERROR" => MessageType.Error,
            "ALERT" => MessageType.Alert,
            "INFO" => MessageType.Info,
            _ => throw new FormatException($"'{name}' is not a built-in message type and no 'MessageType.{name}' resource was found.")
        };
    }

    private static bool TryFindResource(ResourceDictionary dictionary, string key, out object resource)
    {
        if (dictionary.TryGetValue(key, out object directValue))
        {
            resource = directValue;

            return true;
        }

        foreach (ResourceDictionary mergedDictionary in dictionary.MergedDictionaries)
        {
            if (TryFindResource(mergedDictionary, key, out resource)) return true;
        }

        resource = null!;

        return false;
    }
}
