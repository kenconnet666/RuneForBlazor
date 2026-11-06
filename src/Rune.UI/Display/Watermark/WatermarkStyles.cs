using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rune.Styles;

namespace Rune.UI;

/// <summary>水印的样式（同 vue-tsx-pro 的 watermarkStyles）：包着内容的一层相对定位，水印那一层盖在上面、不挡操作。</summary>
public static class WatermarkStyles
{
    internal static readonly Attrs Root = new() { Sx = RootStyle };
    internal static readonly Attrs Layer = new() { Sx = LayerStyle };
    internal static readonly Attrs FullscreenLayer = new() { Sx = Sx.Of(LayerStyle, FullscreenStyle) };

    static Style RootStyle => Css.Define(static () => new Style { Position = Position.Relative });

    static Style LayerStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        ZIndex = 9,
        PointerEvents = PointerEvents.None,
        BackgroundRepeat = BackgroundRepeat.Repeat,
        // Windows 高对比下系统会去掉背景图：水印本来就是装饰，不需要
        ForcedColorAdjust = ForcedColorAdjust.Auto,
    });

    static Style FullscreenStyle => Css.Define(static () => new Style { Position = Position.Fixed, ZIndex = 1000 });
}

/// <summary>交给 JS 画水印的选项（data-rune-watermark 的 JSON）。</summary>
internal sealed record WatermarkOptions(
    IReadOnlyList<string> Lines, string? Image, int Width, int Height, int Rotate, int[] Gap, int FontSize, string? Color);

// 裁剪友好：源生成的序列化，不靠反射
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WatermarkOptions))]
internal sealed partial class WatermarkJson : JsonSerializerContext
{
    // 文字原样写进属性（Blazor 会转义属性值），不转成 \uXXXX
    internal static WatermarkJson Relaxed { get; } = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
}
