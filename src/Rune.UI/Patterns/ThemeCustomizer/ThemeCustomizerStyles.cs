using Rune.Styles;

namespace Rune.UI;

/// <summary>主题编辑器的部件。</summary>
public enum ThemeCustomizerPart
{
    /// <summary>外层 div。</summary>
    Root,

    /// <summary>一个分组（section）。</summary>
    Group,

    /// <summary>分组的标题与说明。</summary>
    GroupHeader,

    /// <summary>一个旋钮（名称、当前取值、选项）。</summary>
    Field,

    /// <summary>旋钮的名称与当前取值。</summary>
    FieldHeader,

    /// <summary>选项（ToggleGroup）。</summary>
    Options,

    /// <summary>一个选项（ToggleItem）。</summary>
    Option,

    /// <summary>选项里示意与名字那一层。</summary>
    OptionContent,

    /// <summary>选项的名字。</summary>
    OptionLabel,

    /// <summary>色块。</summary>
    Swatch,

    /// <summary>圆角示意。</summary>
    Corner,

    /// <summary>字号、字体示意（Aa）。</summary>
    Sample,
}

/// <summary>主题编辑器的样式（同 vue-tsx-pro 的 themeCustomizerStyles）。</summary>
public static class ThemeCustomizerStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<ThemeCustomizerPart> Of(ComponentStyles styles) =>
        Recipe.Of("theme-customizer", styles, static (ThemeCustomizerPart part) => part switch
        {
            ThemeCustomizerPart.Root => Root,
            ThemeCustomizerPart.Group => Group,
            ThemeCustomizerPart.GroupHeader => GroupHeader,
            ThemeCustomizerPart.Field => Field,
            ThemeCustomizerPart.FieldHeader => FieldHeader,
            ThemeCustomizerPart.Options => Options,
            ThemeCustomizerPart.Option => Option,
            ThemeCustomizerPart.OptionContent => OptionContent,
            ThemeCustomizerPart.OptionLabel => OptionLabel,
            ThemeCustomizerPart.Swatch => Swatch,
            ThemeCustomizerPart.Corner => Corner,
            _ => Sample,
        });

    /// <summary>色板：只放色块，排成紧凑的网格（叠在 Options 之后）。</summary>
    public static Style Swatches => Css.Define(static () => new Style { GridTemplateColumns = "repeat(auto-fill, minmax(2em, 1fr))" });

    /// <summary>色板里的一项：色块居中（叠在 Option 之后）。</summary>
    public static Style SwatchOption => Css.Define(static () => new Style { JustifyContent = JustifyContent.Center, PaddingInline = 0 });

    /// <summary>“主题默认”：虚线圈，表示不覆盖、沿用主题自己的颜色（叠在 Swatch 之后）。</summary>
    public static Style SwatchDefault => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Dashed,
        BorderColor = Colors.BorderStrong,
    });

    /// <summary>圆角示意的四档（叠在 Corner 之后）。</summary>
    public static Style CornerOf(ThemeRadius radius) => radius switch
    {
        ThemeRadius.None => Css.Define(static () => new Style { BorderStartStartRadius = 0 }),
        ThemeRadius.Small => Css.Define(static () => new Style { BorderStartStartRadius = CssLength.Em(0.2) }),
        ThemeRadius.Large => Css.Define(static () => new Style { BorderStartStartRadius = CssLength.Em(0.8) }),
        _ => Css.Define(static () => new Style { BorderStartStartRadius = CssLength.Em(0.45) }),
    };

    /// <summary>无衬线、衬线的字体示意（叠在 Sample 之后）。</summary>
    public static Style FontOf(ThemeFont font) => font == ThemeFont.SerifHeadings
        ? Css.Define(static () => new Style { FontFamily = Fonts.Serif })
        : Css.Define(static () => new Style { FontFamily = Fonts.Sans });

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Xl });

    // 组与组之间一条分隔线；第一组上面不画
    static Style Group => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Lg,
        PaddingBlockStart = Space.Lg,
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderBlockStartColor = Colors.BorderMuted,
        [When.FirstChild] = new() { PaddingBlockStart = 0, BorderBlockStartWidth = 0 },
    });

    static Style GroupHeader => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Xs });

    static Style Field => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Sm });

    static Style FieldHeader => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Baseline,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Sm,
    });

    // 选项排成网格，窄栏里自动换行；ToggleGroup 默认按内容宽：这里占满整栏，auto-fill 才按栏宽排出几列
    static Style Options => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Width = CssLength.Auto,
        GridTemplateColumns = "repeat(auto-fill, minmax(6.5em, 1fr))",
    });

    // 每项左对齐，示意在前、名字在后
    static Style Option => Css.Define(static () => new Style { JustifyContent = JustifyContent.FlexStart, MinWidth = 0 });

    static Style OptionContent => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Sm, MinWidth = 0 });

    static Style OptionLabel => Css.Define(static () => new Style { Overflow = Overflow.Hidden, TextOverflow = TextOverflow.Ellipsis, WhiteSpace = WhiteSpace.Nowrap });

    static Style Swatch => Css.Define(static () => new Style { FlexShrink = 0, Width = CssLength.Em(1.25), Height = CssLength.Em(1.25), BorderRadius = Radii.Full });

    // 圆角示意：一个只有上边与左边的角
    static Style Corner => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        BoxSizing = BoxSizing.BorderBox,
        Width = CssLength.Em(1),
        Height = CssLength.Em(1),
        BorderBlockStartWidth = BorderWidths.Sm,
        BorderInlineStartWidth = BorderWidths.Sm,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderInlineStartStyle = BorderStyle.Solid,
        BorderColor = CssColors.CurrentColor,
    });

    static Style Sample => Css.Define(static () => new Style { FlexShrink = 0, LineHeight = 1, FontWeight = FontWeights.Semibold });
}
