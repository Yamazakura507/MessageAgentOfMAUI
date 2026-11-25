using System.Security.Cryptography;
using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Enums.Dialog;
using MessageAgent.Core.Models.Dialogs.Options;
using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Services.Extensions;
using SkiaSharp;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Text and image-puzzle CAPTCHA dialog. All visual controls are declared in XAML.
/// </summary>
public partial class MessageCaptchaPage : AnimatedModalPage
{
    private readonly MessageCaptchaOptions options;
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private SKBitmap? sourceBitmap;
    private SKBitmap? backgroundBitmap;
    private SKBitmap? pieceBitmap;
    private int targetX;
    private int targetY;
    private int pieceWidth;
    private int pieceHeight;
    private double candidateX;
    private double candidateY;
    private MessageCaptchaShape activeShape;
    private MessageCaptchaPosition activePosition;
    private float[] customShapeRadii = Array.Empty<float>();
    private bool horizontalSliderReleased;
    private bool verticalSliderReleased;
    private string answer = string.Empty;

    private static readonly MessageCaptchaPosition[] Positions =
    {
        MessageCaptchaPosition.Horizontal,
        MessageCaptchaPosition.Vertical,
        MessageCaptchaPosition.FullArea
    };
    private static readonly MessageCaptchaShape[] Shapes =
    {
        MessageCaptchaShape.Triangle, 
        MessageCaptchaShape.Square, 
        MessageCaptchaShape.Circle,
        MessageCaptchaShape.Star, 
        MessageCaptchaShape.Diamond, 
        MessageCaptchaShape.Custom
    };

    public string Prompt => options.Prompt;
    public Color AccentColor { get; }
    public Color AccentContentColor { get; }
    public Color DialogSurface { get; }
    public Color ForegroundColor { get; }
    public Color SecondaryColor { get; }
    public Task<bool> Result => completion.Task;

    private MessageCaptchaPage(MessageCaptchaOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        MessageType palette = MessageType.Info;

        AccentColor = palette.GetDialogAccent();
        AccentContentColor = palette.GetAccentContent();
        DialogSurface = palette.GetDialogSurface();
        ForegroundColor = palette.GetDialogForeground();
        SecondaryColor = palette.GetDialogSecondaryForeground();

        InitializeComponent();
    }

    public static async ValueTask<MessageCaptchaPage> CreateAsync(MessageCaptchaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        MessageCaptchaPage page = new(options);

        await page.InitializeChallengeAsync();

        return page;
    }

    private async ValueTask InitializeChallengeAsync()
    {
        TextChallenge.IsVisible = options.Mode == MessageCaptchaMode.Text;
        ImageChallenge.IsVisible = options.Mode == MessageCaptchaMode.Image;
        VerifyButton.IsVisible = options.Mode == MessageCaptchaMode.Text;
        VerifyButton.Text = options.VerifyText;
        CancelButton.Text = options.CancelText;

        if (options.Mode == MessageCaptchaMode.Text)
        {
            answer = GenerateAnswer(options.SourceText, options.TextLength);
            CaptchaCodeLabel.Text = string.Join(" ", answer.ToCharArray());

            return;
        }

        if (options.Image is null) throw new ArgumentException("Для CAPTCHA в режиме Image необходимо передать ImageSource.", nameof(options));
        if (options.Tolerance is < 0 or > 0.5) throw new ArgumentOutOfRangeException(nameof(options), "Tolerance должен находиться в диапазоне 0..0.5.");

        using Stream imageStream = await OpenImageStreamAsync(options.Image);

        sourceBitmap = SKBitmap.Decode(imageStream) ?? throw new InvalidOperationException("Не удалось декодировать изображение CAPTCHA.");
        
        if (sourceBitmap.Width < 80 || sourceBitmap.Height < 80) throw new ArgumentException("Изображение CAPTCHA должно иметь размер не менее 80 × 80 пикселей.", nameof(options));

        BuildPuzzle();
    }

    private static async ValueTask<Stream> OpenImageStreamAsync(ImageSource imageSource)
    {
        if (imageSource is IStreamImageSource streamSource)
            return await streamSource.GetStreamAsync(CancellationToken.None) ?? throw new InvalidOperationException("Источник изображения вернул пустой поток.");

        if (imageSource is FileImageSource fileSource)
        {
            if (File.Exists(fileSource.File)) return File.OpenRead(fileSource.File);

            return await FileSystem.OpenAppPackageFileAsync(fileSource.File);
        }

        throw new NotSupportedException("Для Image CAPTCHA используйте ImageSource.FromFile, ImageSource.FromUri или ImageSource.FromStream.");
    }

    private static string GenerateAnswer(string? sourceText, int requestedLength)
    {
        const string fallback = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string source = new((sourceText ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

        if (source.Length == 0) source = fallback;

        int length = Math.Clamp(requestedLength, 4, 10);
        char[] result = new char[length];

        for (int index = 0; index < result.Length; index++) result[index] = source[RandomNumberGenerator.GetInt32(source.Length)];

        return new string(result);
    }

    private void BuildPuzzle()
    {
        if (sourceBitmap is null) return;

        backgroundBitmap?.Dispose();
        pieceBitmap?.Dispose();

        activeShape = options.Shape == MessageCaptchaShape.Random ? PickRandomShape() : options.Shape;
        customShapeRadii = activeShape == MessageCaptchaShape.Custom
            ? Enumerable.Range(0, Random.Shared.Next(6, 10)).Select(_ => (float)(Random.Shared.NextDouble() * 0.28 + 0.7)).ToArray()
            : Array.Empty<float>();
        activePosition = options.Position == MessageCaptchaPosition.Random ? PickRandomPosition() : options.Position;

        int minDimension = Math.Min(sourceBitmap.Width, sourceBitmap.Height);

        pieceWidth = pieceHeight = Math.Clamp((int)(minDimension * 0.22), 52, 116);

        int maxX = Math.Max(1, sourceBitmap.Width - pieceWidth);
        int maxY = Math.Max(1, sourceBitmap.Height - pieceHeight);

        targetX = Random.Shared.Next(0, maxX + 1);
        targetY = Random.Shared.Next(0, maxY + 1);

        SKImageInfo backgroundInfo = new(sourceBitmap.Width, sourceBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

        backgroundBitmap = new SKBitmap(backgroundInfo);

        using (SKCanvas canvas = new(backgroundBitmap))
        using (SKPaint clearPaint = new() { Color = SKColors.Transparent, BlendMode = SKBlendMode.Clear, IsAntialias = true })
        using (SKPath targetPath = CreateShapePath(activeShape, new SKRect(targetX, targetY, targetX + pieceWidth, targetY + pieceHeight)))
        {
            canvas.DrawBitmap(sourceBitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            canvas.DrawPath(targetPath, clearPaint);
        }

        pieceBitmap = new SKBitmap(new SKImageInfo(pieceWidth, pieceHeight, SKColorType.Rgba8888, SKAlphaType.Premul));

        using (SKCanvas pieceCanvas = new(pieceBitmap))
        using (SKPath localPath = CreateShapePath(activeShape, new SKRect(0, 0, pieceWidth, pieceHeight)))
        {
            pieceCanvas.Clear(SKColors.Transparent);
            pieceCanvas.Save();
            pieceCanvas.ClipPath(localPath, SKClipOperation.Intersect, antialias: true);
            pieceCanvas.DrawBitmap(
                sourceBitmap,
                new SKRect(targetX, targetY, targetX + pieceWidth, targetY + pieceHeight),
                new SKRect(0, 0, pieceWidth, pieceHeight),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            pieceCanvas.Restore();
        }

        candidateX = activePosition == MessageCaptchaPosition.Vertical ? targetX : PickStartPosition(targetX, maxX);
        candidateY = activePosition == MessageCaptchaPosition.Horizontal ? targetY : PickStartPosition(targetY, maxY);
        horizontalSliderReleased = false;
        verticalSliderReleased = false;
        HorizontalSlider.Value = maxX == 0 ? 0 : candidateX / maxX * 100;
        VerticalSlider.Value = maxY == 0 ? 0 : candidateY / maxY * 100;
        HorizontalSliderRow.IsVisible = activePosition is MessageCaptchaPosition.Horizontal or MessageCaptchaPosition.FullArea;
        VerticalSliderRow.IsVisible = activePosition is MessageCaptchaPosition.Vertical or MessageCaptchaPosition.FullArea;
        PuzzleModeLabel.Text = $"{GetShapeName(activeShape)} · {GetPositionName(activePosition)}";
        FeedbackLabel.IsVisible = false;

        PuzzleCanvas.InvalidateSurface();
    }

    private static double PickStartPosition(int target, int maximum)
    {
        if (maximum <= 1) return 0;

        int candidate = Random.Shared.Next(maximum + 1);

        if (Math.Abs(candidate - target) < maximum * 0.25) candidate = target < maximum / 2 ? maximum : 0;

        return candidate;
    }

    private static MessageCaptchaShape PickRandomShape() => Shapes[Random.Shared.Next(Shapes.Length)];

    private static MessageCaptchaPosition PickRandomPosition() => Positions[Random.Shared.Next(Positions.Length)];

    private static string GetShapeName(MessageCaptchaShape shape) => shape switch
    {
        MessageCaptchaShape.Triangle => "Треугольник",
        MessageCaptchaShape.Square => "Квадрат",
        MessageCaptchaShape.Circle => "Круг",
        MessageCaptchaShape.Star => "Звезда",
        MessageCaptchaShape.Diamond => "Ромб",
        _ => "Произвольная форма"
    };

    private static string GetPositionName(MessageCaptchaPosition position) => position switch
    {
        MessageCaptchaPosition.Horizontal => "По горизонтали",
        MessageCaptchaPosition.Vertical => "По вертикали",
        _ => "По двум осям"
    };

    private SKPath CreateShapePath(MessageCaptchaShape shape, SKRect bounds)
    {
        using SKPathBuilder builder = new();

        float centerX = bounds.MidX;
        float centerY = bounds.MidY;
        float radiusX = bounds.Width * 0.48f;
        float radiusY = bounds.Height * 0.48f;

        switch (shape)
        {
            case MessageCaptchaShape.Square:
                builder.AddRect(bounds);
                break;
            case MessageCaptchaShape.Circle:
                builder.AddOval(bounds);
                break;
            case MessageCaptchaShape.Triangle:
                builder.MoveTo(centerX, bounds.Top);
                builder.LineTo(bounds.Right, bounds.Bottom);
                builder.LineTo(bounds.Left, bounds.Bottom);
                builder.Close();
                break;
            case MessageCaptchaShape.Diamond:
                builder.MoveTo(centerX, bounds.Top);
                builder.LineTo(bounds.Right, centerY);
                builder.LineTo(centerX, bounds.Bottom);
                builder.LineTo(bounds.Left, centerY);
                builder.Close();
                break;
            case MessageCaptchaShape.Star:
                AddRadialPolygon(builder, centerX, centerY, radiusX, radiusY,
                    Enumerable.Range(0, 10).Select(index => index % 2 == 1 ? 0.43f : 1f).ToArray());
                break;
            default:
                AddRadialPolygon(builder, centerX, centerY, radiusX, radiusY, customShapeRadii);
                break;
        }

        return builder.Detach();
    }

    private static void AddRadialPolygon(
        SKPathBuilder builder,
        float centerX,
        float centerY,
        float radiusX,
        float radiusY,
        IReadOnlyList<float> radii)
    {
        int points = radii.Count;

        for (int index = 0; index < points; index++)
        {
            double angle = -Math.PI / 2 + index * Math.PI * 2 / points;
            float x = centerX + (float)(Math.Cos(angle) * radiusX * radii[index]);
            float y = centerY + (float)(Math.Sin(angle) * radiusY * radii[index]);

            if (index == 0) builder.MoveTo(x, y);
            else builder.LineTo(x, y);
        }

        builder.Close();
    }

    private void PuzzleCanvasOnPaintSurface(object? sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
        SKCanvas canvas = e.Surface.Canvas;

        canvas.Clear(SKColors.Transparent);

        if (backgroundBitmap is null || pieceBitmap is null) return;

        float scale = Math.Min(e.Info.Width / (float)backgroundBitmap.Width, e.Info.Height / (float)backgroundBitmap.Height);
        float drawWidth = backgroundBitmap.Width * scale;
        float drawHeight = backgroundBitmap.Height * scale;
        float left = (e.Info.Width - drawWidth) / 2;
        float top = (e.Info.Height - drawHeight) / 2;
        SKRect destination = new(left, top, left + drawWidth, top + drawHeight);
        SKSamplingOptions sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

        canvas.DrawBitmap(backgroundBitmap, destination, sampling);

        float pieceLeft = left + (float)candidateX * scale;
        float pieceTop = top + (float)candidateY * scale;
        SKRect pieceDestination = new(pieceLeft, pieceTop, pieceLeft + pieceWidth * scale, pieceTop + pieceHeight * scale);

        canvas.DrawBitmap(pieceBitmap, pieceDestination, sampling);
    }

    private void HorizontalSliderOnValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (activePosition is MessageCaptchaPosition.Horizontal or MessageCaptchaPosition.FullArea && sourceBitmap is not null)
            candidateX = e.NewValue / 100d * Math.Max(0, sourceBitmap.Width - pieceWidth);

        PuzzleCanvas.InvalidateSurface();
    }

    private void VerticalSliderOnValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (activePosition is MessageCaptchaPosition.Vertical or MessageCaptchaPosition.FullArea && sourceBitmap is not null)
            candidateY = e.NewValue / 100d * Math.Max(0, sourceBitmap.Height - pieceHeight);

        PuzzleCanvas.InvalidateSurface();
    }

    private async void SliderOnDragCompleted(object? sender, EventArgs e)
    {
        if (activePosition == MessageCaptchaPosition.FullArea)
        {
            if (ReferenceEquals(sender, HorizontalSlider)) horizontalSliderReleased = true;
            if (ReferenceEquals(sender, VerticalSlider)) verticalSliderReleased = true;
            if (!horizontalSliderReleased || !verticalSliderReleased)
            {
                FeedbackLabel.Text = "Настройте обе оси, затем отпустите второй ползунок.";
                FeedbackLabel.IsVisible = true;

                return;
            }
        }

        if (IsPuzzleSolved())
        {
            FeedbackLabel.Text = "Проверка пройдена.";

            await CompleteAsync(true);

            return;
        }

        FeedbackLabel.Text = "Фрагмент не совпал с вырезом. Положение сброшено.";
        FeedbackLabel.IsVisible = true;
        BuildPuzzle();
    }

    private bool IsPuzzleSolved()
    {
        if (sourceBitmap is null || pieceWidth == 0 || pieceHeight == 0) return false;

        double overlapWidth = Math.Max(0, Math.Min(candidateX + pieceWidth, targetX + pieceWidth) - Math.Max(candidateX, targetX));
        double overlapHeight = Math.Max(0, Math.Min(candidateY + pieceHeight, targetY + pieceHeight) - Math.Max(candidateY, targetY));
        double overlap = overlapWidth * overlapHeight / (pieceWidth * (double)pieceHeight);

        return overlap >= 1d - Math.Clamp(options.Tolerance, 0, 0.5);
    }

    private async void VerifyButtonOnClicked(object? sender, EventArgs e)
    {
        string entered = new((CaptchaAnswerEntry.Text ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        
        if (string.Equals(entered, answer, StringComparison.Ordinal))
        {
            await CompleteAsync(true);

            return;
        }

        FeedbackLabel.Text = "Код не совпадает. Попробуйте ещё раз.";
        FeedbackLabel.IsVisible = true;
        CaptchaAnswerEntry.Text = string.Empty;

        answer = GenerateAnswer(options.SourceText, options.TextLength);

        CaptchaCodeLabel.Text = string.Join(" ", answer.ToCharArray());
    }

    private async void CancelButtonOnClicked(object? sender, EventArgs e) => await CompleteAsync(false);

    private async ValueTask CompleteAsync(bool result)
    {
        if (!completion.TrySetResult(result)) return;

        await CloseAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CompleteAsync(false);

        return true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Overlay.Opacity = 0;
        DialogPanel.Opacity = 0;
        DialogPanel.Scale = 0.94;

        await Task.WhenAll(
            Overlay.FadeToAsync(1, 160, Easing.CubicOut),
            DialogPanel.FadeToAsync(1, 210, Easing.CubicOut),
            DialogPanel.ScaleToAsync(1, 230, Easing.CubicOut));
    }

    protected override void OnDisappearing()
    {
        sourceBitmap?.Dispose();
        backgroundBitmap?.Dispose();
        pieceBitmap?.Dispose();

        base.OnDisappearing();
    }

    protected override Task AnimateOutAsync() => Task.WhenAll(
        Overlay.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.ScaleToAsync(0.96, 150, Easing.CubicIn));
}