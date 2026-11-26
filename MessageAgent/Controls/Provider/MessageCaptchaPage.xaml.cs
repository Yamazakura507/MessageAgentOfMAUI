using System.Security.Cryptography;
using LottieAgent.Interfaces;
using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Enums.Dialog;
using MessageAgent.Core.Models.Dialogs.Options;
using MessageAgent.Core.Models.Dialogs.Structs;
using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Resources.ExtensionResources;
using MessageAgent.Services;
using MessageAgent.Services.Extensions;
using MessageAgent.Services.Factory;
using MessageAgent.Services.Interactions;
using ResourcesLib.Models;
using SkiaSharp;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Text and image-puzzle CAPTCHA dialog. All visual controls are declared in XAML.
/// </summary>
public partial class MessageCaptchaPage : AnimatedModalPage
{
    private readonly MessageCaptchaOptions options;
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly LottiePlaybackInteraction closeAnimationInteraction;
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
    private TextGlyphTransform[] glyphTransforms = Array.Empty<TextGlyphTransform>();
    private TextNoiseLine[] textNoiseLines = Array.Empty<TextNoiseLine>();
    private TextNoiseDot[] textNoiseDots = Array.Empty<TextNoiseDot>();
    private float textOverlapFactor = 1f;
    private float textAdvanceGap = 5f;

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

    /// <summary>
    /// Gets the instruction text displayed above the CAPTCHA challenge.
    /// </summary>
    public string Prompt => options.Prompt;
    /// <summary>
    /// Gets the resource key of the CAPTCHA dialog icon.
    /// </summary>
    public string DialogIconKey => MessageAgentImageKeys.Captcha;
    /// <summary>
    /// Gets the message type used to resolve the dialog color palette.
    /// </summary>
    public MessageType DialogType { get; }
    /// <summary>
    /// Gets a value indicating whether the text CAPTCHA layout is active.
    /// </summary>
    public bool IsTextMode => options.Mode == MessageCaptchaMode.Text;
    /// <summary>
    /// Gets a value indicating whether the image-puzzle CAPTCHA layout is active.
    /// </summary>
    public bool IsImageMode => options.Mode == MessageCaptchaMode.Image;
    /// <summary>
    /// Gets a value indicating whether the text verification button should be displayed.
    /// </summary>
    public bool ShowVerifyButton => IsTextMode;
    /// <summary>
    /// Gets the caption of the verification button.
    /// </summary>
    public string VerifyText => options.VerifyText;
    /// <summary>
    /// Gets the caption of the cancel button.
    /// </summary>
    public string CancelText => options.CancelText;
    /// <summary>
    /// Gets a value indicating whether horizontal puzzle movement is enabled.
    /// </summary>
    public bool ShowHorizontalSlider => activePosition is MessageCaptchaPosition.Horizontal or MessageCaptchaPosition.FullArea;
    /// <summary>
    /// Gets a value indicating whether vertical puzzle movement is enabled.
    /// </summary>
    public bool ShowVerticalSlider => activePosition is MessageCaptchaPosition.Vertical or MessageCaptchaPosition.FullArea;
    /// <summary>
    /// Gets the localized description of the selected puzzle-piece shape and movement direction.
    /// </summary>
    public string PuzzleModeText => $"{GetShapeName(activeShape)} · {GetPositionName(activePosition)}";
    /// <summary>
    /// Gets the service used to load the close-button Lottie animation.
    /// </summary>
    public ILottieJsonLoader JsonLoader { get; }
    /// <summary>
    /// Gets the service used to process Lottie animation modifiers.
    /// </summary>
    public ILottieModifierProcessor ModifierProcessor { get; }
    /// <summary>
    /// Gets the accent color used by the CAPTCHA dialog.
    /// </summary>
    public Color AccentColor { get; }
    /// <summary>
    /// Gets the background color of the CAPTCHA dialog surface.
    /// </summary>
    public Color DialogSurface { get; }
    /// <summary>
    /// Gets the foreground color used for secondary CAPTCHA artwork.
    /// </summary>
    public Color ForegroundColor { get; }
    /// <summary>
    /// Gets the light outline color used to distinguish the puzzle piece and its target.
    /// </summary>
    public SKColor CaptchaOutlineColor { get; }
    /// <summary>
    /// Gets the shadow color rendered beneath the puzzle-piece outline.
    /// </summary>
    public SKColor CaptchaOutlineShadowColor { get; }
    /// <summary>
    /// Gets the asynchronous result of the challenge; <see langword="true"/> indicates successful verification.
    /// </summary>
    public Task<bool> Result => completion.Task;

    private MessageCaptchaPage(MessageCaptchaOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        DialogType = MessageType.Info;
        AccentColor = DialogType.GetDialogAccent();
        DialogSurface = DialogType.GetDialogSurface();
        ForegroundColor = DialogType.GetDialogForeground();
        CaptchaOutlineColor = ToSkColor(MessageAgentResources.GetColor(MessageAgentColorKeys.CaptchaOutline));
        CaptchaOutlineShadowColor = ToSkColor(MessageAgentResources.GetColor(MessageAgentColorKeys.CaptchaOutlineShadow));

        JsonLoader = MauiServiceProvider.GetRequiredService<ILottieJsonLoader>();
        ModifierProcessor = MauiServiceProvider.GetRequiredService<ILottieModifierProcessor>();

        InitializeComponent();

        LottieResourceSettings closeSettings = MessageAgentLottieSettingsFactory.GetCloseSettings(AccentColor);

        MessageAgentLottieSettingsFactory.Apply(HeaderCloseAnimation, closeSettings);

        closeAnimationInteraction = new LottiePlaybackInteraction(
            HeaderCloseAnimation,
            HeaderCloseButton,
            closeSettings.RestingProgress!.Value,
            resetToRestingOnDeactivate: true);
    }

    /// <summary>
    /// Creates and initializes a CAPTCHA page using the supplied challenge options.
    /// </summary>
    /// <param name="options">The mode, content, and validation settings for the challenge.</param>
    /// <returns>The initialized CAPTCHA page.</returns>
    /// <exception cref="ArgumentNullException">The options argument is <see langword="null"/>.</exception>
    public static async ValueTask<MessageCaptchaPage> CreateAsync(MessageCaptchaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        MessageCaptchaPage page = new(options);

        await page.InitializeChallengeAsync();

        return page;
    }

    private async ValueTask InitializeChallengeAsync()
    {
        if (options.Mode == MessageCaptchaMode.Text)
        {
            answer = GenerateAnswer(options.SourceText, options.TextLength);
            PrepareTextCaptchaArtwork();
            CaptchaTextCanvas.InvalidateSurface();

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

    private void PrepareTextCaptchaArtwork()
    {
        (int lineCount, int dotCount, float maxRotation, float maxOffset, float maxSkew,
            float verticalScale, float overlapFactor, float advanceGap, float strokeWidth) = options.DistortionLevel switch
        {
            MessageCaptchaDistortionLevel.None => (0, 0, 0f, 0f, 0f, 0f, 1f, 7f, 0f),
            MessageCaptchaDistortionLevel.Low => (1, 8, 4f, 2f, 0.05f, 0.04f, 0.98f, 5f, 1f),
            MessageCaptchaDistortionLevel.Medium => (3, 18, 8f, 4f, 0.12f, 0.08f, 0.92f, 3f, 1.3f),
            MessageCaptchaDistortionLevel.High => (6, 34, 14f, 7f, 0.22f, 0.14f, 0.84f, 1f, 1.7f),
            _ => (10, 54, 20f, 11f, 0.34f, 0.22f, 0.74f, -1f, 2.1f)
        };

        static float RandomBetween(float minimum, float maximum) => minimum + (float)Random.Shared.NextDouble() * (maximum - minimum);

        textOverlapFactor = overlapFactor;
        textAdvanceGap = advanceGap;

        glyphTransforms = Enumerable.Range(0, answer.Length)
            .Select(_ => new TextGlyphTransform(
                RandomBetween(-maxRotation, maxRotation),
                RandomBetween(-maxOffset, maxOffset),
                RandomBetween(-maxSkew, maxSkew),
                RandomBetween(1f - verticalScale, 1f + verticalScale)))
            .ToArray();

        textNoiseLines = Enumerable.Range(0, lineCount)
            .Select(index => new TextNoiseLine(
                RandomBetween(0f, 1f),
                RandomBetween(0f, 1f),
                RandomBetween(0f, 1f),
                RandomBetween(0f, 1f),
                RandomBetween(Math.Max(0.7f, strokeWidth * 0.65f), Math.Max(0.8f, strokeWidth * 1.35f)),
                index % 3 == 0,
                (byte)Random.Shared.Next(75, 165)))
            .ToArray();

        textNoiseDots = Enumerable.Range(0, dotCount)
            .Select(index => new TextNoiseDot(
                RandomBetween(0f, 1f),
                RandomBetween(0f, 1f),
                RandomBetween(0.8f, 2.2f + strokeWidth),
                index % 4 == 0,
                (byte)Random.Shared.Next(65, 155)))
            .ToArray();
    }

    private void CaptchaTextCanvasOnPaintSurface(object? sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
        if (string.IsNullOrEmpty(answer)) return;

        SKCanvas canvas = e.Surface.Canvas;
        float canvasWidth = e.Info.Width;
        float canvasHeight = e.Info.Height;

        canvas.Clear(ToSkColor(DialogSurface));

        DrawTextCaptchaArtifacts(canvas, canvasWidth, canvasHeight, drawOverText: false);

        using SKFont textFont = new(SKTypeface.Default, Math.Clamp(canvasHeight * 0.62f, 22f, 60f))
        {
            Embolden = true,
            Subpixel = true,
            Edging = SKFontEdging.Antialias
        };
        using SKPaint textPaint = new()
        {
            Color = ToSkColor(AccentColor),
            IsAntialias = true
        };

        float[] glyphWidths = answer.Select(character => textFont.MeasureText(character.ToString(), textPaint)).ToArray();
        float maxTextWidth = Math.Max(40f, canvasWidth - 28f);

        float CalculateTotalWidth()
        {
            float total = 0f;

            for (int index = 0; index < glyphWidths.Length; index++)
                total += glyphWidths[index] * textOverlapFactor + (index == glyphWidths.Length - 1 ? 0f : textAdvanceGap);

            return total;
        }

        float totalWidth = CalculateTotalWidth();

        if (totalWidth > maxTextWidth)
        {
            textFont.Size *= maxTextWidth / totalWidth;

            for (int index = 0; index < answer.Length; index++)
                glyphWidths[index] = textFont.MeasureText(answer[index].ToString(), textPaint);

            totalWidth = CalculateTotalWidth();
        }

        float cursor = (canvasWidth - totalWidth) / 2f;
        float normalBaseline = canvasHeight * 0.66f;

        for (int index = 0; index < answer.Length; index++)
        {
            string glyph = answer[index].ToString();
            TextGlyphTransform transform = glyphTransforms[index];
            float glyphWidth = glyphWidths[index];

            textFont.SkewX = transform.SkewX;

            canvas.Save();
            canvas.Translate(cursor + glyphWidth / 2f, normalBaseline + transform.BaselineOffset);
            canvas.RotateDegrees(transform.RotationDegrees);
            canvas.Scale(1f, transform.VerticalScale);
            canvas.DrawText(glyph, -glyphWidth / 2f, 0f, SKTextAlign.Left, textFont, textPaint);
            canvas.Restore();

            cursor += glyphWidth * textOverlapFactor + textAdvanceGap;
        }

        DrawTextCaptchaArtifacts(canvas, canvasWidth, canvasHeight, drawOverText: true);
    }

    private void DrawTextCaptchaArtifacts(SKCanvas canvas, float width, float height, bool drawOverText)
    {
        SKColor accent = ToSkColor(AccentColor);
        SKColor secondary = ToSkColor(ForegroundColor);

        using SKPaint linePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true
        };

        foreach (TextNoiseLine line in textNoiseLines)
        {
            if (line.DrawOverText != drawOverText) continue;

            linePaint.Color = new SKColor(accent.Red, accent.Green, accent.Blue, line.Alpha);
            linePaint.StrokeWidth = Math.Max(0.7f, line.StrokeWidth);
            canvas.DrawLine(
                line.StartX * width,
                line.StartY * height,
                line.EndX * width,
                line.EndY * height,
                linePaint);
        }

        using SKPaint dotPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };

        foreach (TextNoiseDot dot in textNoiseDots)
        {
            if (dot.DrawOverText != drawOverText) continue;

            dotPaint.Color = new SKColor(secondary.Red, secondary.Green, secondary.Blue, dot.Alpha);

            canvas.DrawCircle(dot.X * width, dot.Y * height, dot.Radius, dotPaint);
        }
    }

    private static SKColor ToSkColor(Color color) => new(
        (byte)Math.Clamp((int)Math.Round(color.Red * 255f), 0, 255),
        (byte)Math.Clamp((int)Math.Round(color.Green * 255f), 0, 255),
        (byte)Math.Clamp((int)Math.Round(color.Blue * 255f), 0, 255),
        (byte)Math.Clamp((int)Math.Round(color.Alpha * 255f), 0, 255));

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
        OnPropertyChanged(nameof(ShowHorizontalSlider));
        OnPropertyChanged(nameof(ShowVerticalSlider));
        OnPropertyChanged(nameof(PuzzleModeText));

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
            DrawCaptchaOutline(canvas, targetPath);
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
            DrawCaptchaOutline(pieceCanvas, localPath);
        }

        candidateX = activePosition == MessageCaptchaPosition.Vertical ? targetX : PickStartPosition(targetX, maxX);
        candidateY = activePosition == MessageCaptchaPosition.Horizontal ? targetY : PickStartPosition(targetY, maxY);
        horizontalSliderReleased = false;
        verticalSliderReleased = false;
        HorizontalSlider.Value = maxX == 0 ? 0 : candidateX / maxX * 100;
        VerticalSlider.Value = maxY == 0 ? 0 : (1d - candidateY / maxY) * 100;
        FeedbackLabel.IsVisible = false;

        PuzzleCanvas.InvalidateSurface();
    }

    private void DrawCaptchaOutline(SKCanvas canvas, SKPath path)
    {
        using SKPaint shadowPaint = new()
        {
            Color = CaptchaOutlineShadowColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 4f,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true
        };
        using SKPaint outlinePaint = new()
        {
            Color = CaptchaOutlineColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true
        };

        canvas.DrawPath(path, shadowPaint);
        canvas.DrawPath(path, outlinePaint);
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
            candidateY = (1d - e.NewValue / 100d) * Math.Max(0, sourceBitmap.Height - pieceHeight);

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
        PrepareTextCaptchaArtwork();
        CaptchaTextCanvas.InvalidateSurface();
    }

    private async void CancelButtonOnClicked(object? sender, EventArgs e) => await CompleteAsync(false);
    private async void CloseButtonOnClicked(object? sender, TappedEventArgs e) => await CompleteAsync(false);

    private async ValueTask CompleteAsync(bool result)
    {
        if (!completion.TrySetResult(result)) return;

        await CloseAsync();
    }

    /// <summary>
    /// Completes the challenge as cancelled when the user navigates back.
    /// </summary>
    /// <returns><see langword="true"/> to indicate that the page handled the back action.</returns>
    protected override bool OnBackButtonPressed()
    {
        _ = CompleteAsync(false);

        return true;
    }

    /// <summary>
    /// Starts the entrance animation when the CAPTCHA page becomes visible.
    /// </summary>
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

    /// <summary>
    /// Releases the close-animation interaction and decoded CAPTCHA bitmaps when the page disappears.
    /// </summary>
    protected override void OnDisappearing()
    {
        closeAnimationInteraction.Dispose();
        sourceBitmap?.Dispose();
        backgroundBitmap?.Dispose();
        pieceBitmap?.Dispose();

        base.OnDisappearing();
    }

    /// <summary>
    /// Runs the exit animation for the CAPTCHA overlay and dialog panel.
    /// </summary>
    /// <returns>A task that completes when all exit animations finish.</returns>
    protected override Task AnimateOutAsync() => Task.WhenAll(
        Overlay.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.ScaleToAsync(0.96, 150, Easing.CubicIn));
}