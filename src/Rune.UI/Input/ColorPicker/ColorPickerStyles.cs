using Rune.Styles;

namespace Rune.UI;

/// <summary>取色器的部件（同 vue-tsx-pro 的 ColorPicker）。</summary>
public enum ColorPickerPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>饱和度 / 明度色板。</summary>
    Area,

    /// <summary>色板上的把手（role="slider"）。</summary>
    Thumb,

    /// <summary>吸管按钮（IconButton，支持 EyeDropper 的浏览器才显示）。</summary>
    EyeDropper,

    /// <summary>色相滑块（Slider）。</summary>
    Hue,

    /// <summary>不透明度滑块（Slider）。</summary>
    Alpha,

    /// <summary>当前颜色的预览块。</summary>
    Preview,

    /// <summary>写法切换（ToggleGroup）。</summary>
    Format,

    /// <summary>颜色输入框（TextField）。</summary>
    Input,

    /// <summary>一排颜色（预设、最近使用）。</summary>
    Swatches,

    /// <summary>每一格颜色（ButtonBase）。</summary>
    Swatch,
}

/// <summary>取色器的样式（同 vue-tsx-pro 的 colorPickerStyles）：色板的色相、把手的位置与颜色、不透明度轨道的颜色经变量给。</summary>
public static class ColorPickerStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    internal static readonly CssVar<CssColor> HueColor = CssVar.Color("rune-color-hue");
    internal static readonly CssVar<CssColor> CurrentColor = CssVar.Color("rune-color-current");
    internal static readonly CssVar<CssValue> Saturation = CssVar.Value("rune-color-s");
    internal static readonly CssVar<CssValue> Brightness = CssVar.Value("rune-color-v");

    /// <summary>各部件的默认层。</summary>
    public static PartLayers<ColorPickerPart> Of(ComponentStyles styles) =>
        Recipe.Of("color-picker", styles, static (ColorPickerPart part) => part switch
        {
            ColorPickerPart.Root => Root,
            ColorPickerPart.Area => Area,
            ColorPickerPart.Thumb => Sx.Of(Thumb, SharedStyles.FocusRing),
            ColorPickerPart.Preview => Sx.Of(SwatchStyles.Checkerboard, Preview),
            ColorPickerPart.Input => Input,
            ColorPickerPart.Swatches => Swatches,
            ColorPickerPart.Swatch => Sx.Of(SwatchStyles.Checkerboard, SwatchStyles.Interactive, Swatch, SharedStyles.FocusRing),
            _ => Sx.Of(),
        });

    internal static readonly Attrs Row = new() { Sx = Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Sm }) };

    internal static readonly Attrs Sliders = new()
    {
        Sx = Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Xs, FlexGrow = 1, MinWidth = 0 }),
    };

    internal static readonly Attrs Label = new() { Sx = Css.Define(static () => new Style { FontSize = FontSizes.Xs, Color = Colors.TextMuted }) };

    // 色相：彩虹轨道；渐变轨道上不要已选部分的填色
    internal static readonly Attrs HueTrack = new()
    {
        Sx = Sx.Of(Track, Css.Define(static () => new Style { BackgroundImage = "linear-gradient(to right, #f00, #ff0 17%, #0f0 33%, #0ff 50%, #00f 67%, #f0f 83%, #f00)" })),
    };

    // 不透明度：棋盘格上叠一层从透明到当前颜色的渐变
    internal static readonly Attrs AlphaTrack = new()
    {
        Sx = Sx.Of(Track, Css.Define(static () => new Style
        {
            BackgroundImage = $"linear-gradient(to right, transparent, {CurrentColor.Reference}), repeating-conic-gradient({Colors.Fill} 0 25%, {Colors.Surface} 0 50%)",
            BackgroundSize = "100% 100%, 0.5em 0.5em",
        })),
    };

    internal static readonly Attrs HiddenRange = new() { Sx = Css.Define(static () => new Style { BackgroundColor = CssColors.Transparent }) };

    /// <summary>色块里的颜色（盖在棋盘格上，带内描边）。</summary>
    internal static readonly Attrs Fill = new()
    {
        Sx = Sx.Of(Css.Define(static () => new Style { Display = Display.Block, Width = CssLength.Percent(100), Height = CssLength.Percent(100), BorderRadius = CssWide.Inherit, BackgroundColor = CurrentColor }), SwatchStyles.Ring),
    };

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Sm,
        Width = CssLength.Rem(16),
        MaxWidth = CssLength.Percent(100),
    });

    // 色板：上面压黑（明度），左白右纯色（饱和度）
    static Style Area => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        AspectRatio = "4 / 3",
        BorderRadius = Radii.Md,
        TouchAction = TouchAction.None,
        Cursor = Cursor.Crosshair,
        BackgroundImage = $"linear-gradient(to top, #000, transparent), linear-gradient(to right, #fff, {HueColor.Reference})",
    });

    static Style Thumb => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Left = $"calc({Saturation.Reference} * 100%)",
        Top = $"calc((1 - {Brightness.Reference}) * 100%)",
        Width = CssLength.Em(1),
        Height = CssLength.Em(1),
        Translate = "-50% -50%",
        BorderRadius = Radii.Full,
        BorderWidth = BorderWidths.Sm,
        BorderStyle = BorderStyle.Solid,
        BorderColor = "#fff",
        BoxShadow = "0 0 0 1px rgb(0 0 0 / 0.3), inset 0 0 0 1px rgb(0 0 0 / 0.3)",
        BackgroundColor = CurrentColor,
    });

    static Style Track => Css.Define(static () => new Style { Height = CssLength.Em(0.75), BorderRadius = Radii.Full });

    static Style Preview => Css.Define(static () => new Style { FlexShrink = 0, Width = CssLength.Em(2), Height = CssLength.Em(2), BorderRadius = Radii.Md });

    static Style Input => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0 });

    static Style Swatches => Css.Define(static () => new Style { Display = Display.Flex, FlexWrap = FlexWrap.Wrap, Gap = Space.Xs });

    static Style Swatch => Css.Define(static () => new Style { Width = CssLength.Em(1.5), Height = CssLength.Em(1.5), BorderRadius = Radii.Sm });
}

/// <summary>最近使用的颜色（同一个页面里的取色器共用，最新的在前，最多 8 个）：按作用域注册，Server 模式下每个连接一份。</summary>
public sealed class RecentColors
{
    const int Limit = 8;

    /// <summary>最近使用的颜色。</summary>
    public IReadOnlyList<string> Colors { get; private set; } = [];

    /// <summary>变了：取色器重新渲染。</summary>
    public event Action? Changed;

    /// <summary>记下一个颜色。</summary>
    public void Remember(string color)
    {
        Colors = [color, .. Colors.Where(item => item != color).Take(Limit - 1)];
        Changed?.Invoke();
    }
}
