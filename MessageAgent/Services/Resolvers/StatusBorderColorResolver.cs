using System.Text.Json;
using MessageAgent.Core.Models;
using MessageAgent.Core.Models.VisualValues;
using MessageAgent.Services.Interfaces;
using SkiaSharp;

namespace MessageAgent.Services.Resolvers;

/// <summary>
/// Extracts a representative color from image pixels or the first solid Lottie fill/stroke.
/// Unsupported sources return null so the view can use its random-color fallback.
/// </summary>
public sealed class StatusBorderColorResolver : IStatusBorderColorResolver
{
    private static readonly HttpClient HttpClient = new();

    /// <inheritdoc />
    public async ValueTask<Color?> ResolveVisualColorAsync(Status status, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(status);

        switch (status.Value)
        {
            case ImageVisualValue { Source: FontImageSource fontSource } when fontSource.Color is not null: return fontSource.Color;
            case ImageVisualValue { Source: not null } image:
            {
                byte[]? bytes = await ReadImageBytesAsync(image.Source, cancellationToken);

                return bytes is null ? null : ExtractDominantImageColor(bytes);
            }
            case LottieVisualValue lottie:
            {
                byte[]? bytes = await ReadLottieBytesAsync(lottie.Source, cancellationToken);

                return bytes is null ? null : ExtractLottieColor(bytes);
            }
            default: return null;
        }
    }

    private static async ValueTask<byte[]?> ReadImageBytesAsync(ImageSource source, CancellationToken cancellationToken)
    {
        try
        {
            switch (source)
            {
                case FontImageSource: return null;
                case FileImageSource file:
                {
                    string fileName = file.File;

                    if (string.IsNullOrWhiteSpace(fileName)) return null;
                    if (Path.IsPathRooted(fileName) && File.Exists(fileName)) return await File.ReadAllBytesAsync(fileName, cancellationToken);

                    string normalized = fileName.Replace('/', '\\');
                    string scaledName = $"{Path.GetFileNameWithoutExtension(normalized)}.scale-100{Path.GetExtension(normalized)}";
                    string[] diskCandidates =
                    [
                        Path.Combine(AppContext.BaseDirectory, normalized),
                        Path.Combine(AppContext.BaseDirectory, scaledName),
                        Path.Combine(AppContext.BaseDirectory, "Images", normalized),
                        Path.Combine(AppContext.BaseDirectory, "Resources", "Images", normalized)
                    ];

                    foreach (string candidate in diskCandidates.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (File.Exists(candidate)) return await File.ReadAllBytesAsync(candidate, cancellationToken);
                    }

                    string[] packageCandidates =
                    [
                        normalized,
                        scaledName,
                        Path.Combine("Images", normalized),
                        Path.Combine("Resources", "Images", normalized)
                    ];

                    foreach (string candidate in packageCandidates.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        try
                        {
                            await using Stream stream = await FileSystem.OpenAppPackageFileAsync(candidate);
                            using MemoryStream memory = new();

                            await stream.CopyToAsync(memory, cancellationToken);

                            return memory.ToArray();
                        }
                        catch (FileNotFoundException)
                        {

                        }
                        catch (DirectoryNotFoundException)
                        {

                        }
                    }

                    return null;
                }
                case UriImageSource uriSource when uriSource.Uri is not null: return await HttpClient.GetByteArrayAsync(uriSource.Uri, cancellationToken);
                case StreamImageSource streamSource when streamSource.Stream is not null:
                {
                    await using Stream? stream = await streamSource.Stream(cancellationToken);

                    if (stream is null) return null;

                    using MemoryStream memory = new();

                    await stream.CopyToAsync(memory, cancellationToken);

                    return memory.ToArray();
                }
                default: return null;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static Color? ExtractDominantImageColor(byte[] bytes)
    {
        try
        {
            using SKBitmap? bitmap = SKBitmap.Decode(bytes);

            if (bitmap is null || bitmap.Width == 0 || bitmap.Height == 0) return null;

            int step = Math.Max(1, Math.Max(bitmap.Width, bitmap.Height) / 160);
            Dictionary<int, (int Count, long Red, long Green, long Blue)> buckets = new();

            for (int y = 0; y < bitmap.Height; y += step)
            {
                for (int x = 0; x < bitmap.Width; x += step)
                {
                    SKColor pixel = bitmap.GetPixel(x, y);

                    if (pixel.Alpha < 96 
                            || (pixel.Red > 246 && pixel.Green > 246 && pixel.Blue > 246)
                            || (pixel.Red < 9 && pixel.Green < 9 && pixel.Blue < 9)) continue;

                    int key = ((pixel.Red >> 4) << 8) | ((pixel.Green >> 4) << 4) | (pixel.Blue >> 4);

                    buckets[key] = buckets.TryGetValue(key, out (int Count, long Red, long Green, long Blue) bucket)
                        ? (bucket.Count + 1, bucket.Red + pixel.Red, bucket.Green + pixel.Green, bucket.Blue + pixel.Blue)
                        : (1, pixel.Red, pixel.Green, pixel.Blue);
                }
            }

            if (buckets.Count == 0) return null;

            (int Count, long Red, long Green, long Blue) dominant = buckets.Values.OrderByDescending(value => value.Count).First();

            return Color.FromRgb(
                (byte)(dominant.Red / dominant.Count),
                (byte)(dominant.Green / dominant.Count),
                (byte)(dominant.Blue / dominant.Count));
        }
        catch
        {
            return null;
        }
    }

    private static async ValueTask<byte[]?> ReadLottieBytesAsync(string source, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;

        string normalized = source.Replace('/', '\\').TrimStart('\\');
        string diskPath = Path.Combine(AppContext.BaseDirectory, normalized);

        if (File.Exists(diskPath)) return await File.ReadAllBytesAsync(diskPath, cancellationToken);

        string[] candidates =
        [
            normalized,
            Path.Combine("Lotties", Path.GetFileName(normalized))
        ];

        foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await using Stream stream = await FileSystem.OpenAppPackageFileAsync(candidate);
                using MemoryStream memory = new();

                await stream.CopyToAsync(memory, cancellationToken);

                return memory.ToArray();
            }
            catch (FileNotFoundException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }
        }

        return null;
    }

    private static Color? ExtractLottieColor(byte[] bytes)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(bytes);

            return FindLottieColor(document.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static Color? FindLottieColor(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("c", out JsonElement colorObject) &&
                colorObject.ValueKind == JsonValueKind.Object &&
                colorObject.TryGetProperty("k", out JsonElement keyframes) &&
                TryReadColor(keyframes, out Color? color)) return color;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                Color? nested = FindLottieColor(property.Value);

                if (nested is not null) return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                Color? nested = FindLottieColor(child);

                if (nested is not null) return nested;
            }
        }

        return null;
    }

    private static bool TryReadColor(JsonElement value, out Color? color)
    {
        color = null;

        if (value.ValueKind != JsonValueKind.Array) return false;

        JsonElement candidate = value;

        if (candidate.GetArrayLength() > 0 &&
            candidate[0].ValueKind == JsonValueKind.Object &&
            candidate[0].TryGetProperty("s", out JsonElement startValue)) candidate = startValue;
        else if (candidate.GetArrayLength() > 0 && candidate[0].ValueKind == JsonValueKind.Array) candidate = candidate[0];

        if (candidate.ValueKind != JsonValueKind.Array 
            || candidate.GetArrayLength() < 3 
            || !candidate[0].TryGetDouble(out double red) 
            || !candidate[1].TryGetDouble(out double green) 
            || !candidate[2].TryGetDouble(out double blue)) return false;

        color = Color.FromRgb(
            (byte)(Math.Clamp(red, 0, 1) * 255),
            (byte)(Math.Clamp(green, 0, 1) * 255),
            (byte)(Math.Clamp(blue, 0, 1) * 255));

        return true;
    }
}
