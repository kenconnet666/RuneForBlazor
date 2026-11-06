using Rune.Styles;

namespace Rune.UI;

/// <summary>截断的文字的样式（同 vue-tsx-pro 的 ellipsisStyles）：单行省略或按行数截断；截断了（JS 写 data-truncated）才显示展开按钮、能聚焦。</summary>
internal static class EllipsisStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它
    static readonly Marker RootMarker = Css.Marker("rune-ellipsis");
    static readonly Condition Truncated = When.Inside(When.Attribute("[data-truncated]"), RootMarker);

    internal static readonly Attrs Root = Attrs.Layer(new Attrs { Sx = RootStyle }, RootMarker.Attrs);
    internal static readonly Attrs MultilineRoot = Attrs.Layer(new Attrs { Sx = Sx.Of(RootStyle, MultilineStyle) }, RootMarker.Attrs);

    /// <summary>展开后的“收起”：一直显示。</summary>
    internal static readonly Attrs Toggle = new() { Sx = ToggleStyle };

    /// <summary>没展开时的“展开”：截断了才显示。</summary>
    internal static readonly Attrs ToggleWhenTruncated = new() { Sx = Sx.Of(ToggleStyle, HiddenUntilTruncated) };

    static readonly Attrs Full = new() { Sx = Sx.Of(TextStyle, SharedStyles.FocusRing) };
    static readonly Attrs Single = new() { Sx = Sx.Of(TextStyle, TruncateStyle, SharedStyles.FocusRing) };

    /// <summary>截断的那段：0 是不截断（展开后），1 是单行省略，多行按行数。</summary>
    internal static Attrs Text(int lines) => lines switch
    {
        <= 0 => Full,
        1 => Single,
        _ => new Attrs { Sx = Sx.Of(TextStyle, ClampStyle(lines), SharedStyles.FocusRing) },
    };

    static Style RootStyle => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Baseline,
        Gap = Space.Xs,
        MinWidth = 0,
        MaxWidth = CssLength.Percent(100),
    });

    // 多行时块级：段落里的描述
    static Style MultilineStyle => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.FlexEnd });

    static Style TextStyle => Css.Define(static () => new Style { MinWidth = 0, BorderRadius = Radii.Xs });

    static Style TruncateStyle => Css.Define(static () => new Style
    {
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
    });

    static Style ClampStyle(int lines) => Css.Define(lines, static lines => new Style
    {
        Display = Display.WebkitBox,
        WebkitBoxOrient = WebkitBoxOrient.Vertical,
        WebkitLineClamp = lines,
        Overflow = Overflow.Hidden,
    });

    // 展开按钮：链接样式，不换行
    static Style ToggleStyle => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        Padding = 0,
        BorderWidth = 0,
        BackgroundColor = CssColors.Transparent,
        Color = Tones.Accent.Text,
        Font = CssWide.Inherit,
        Cursor = Cursor.Pointer,
        WhiteSpace = WhiteSpace.Nowrap,
        TextDecorationLine = TextDecorationLine.None,
        [When.Hover] = new() { TextDecorationLine = TextDecorationLine.Underline },
    });

    static Style HiddenUntilTruncated => Css.Define(static () => new Style
    {
        Display = Display.None,
        [Truncated] = new() { Display = Display.Inline },
    });
}
