using Rune.Styles;

namespace Rune.UI;

/// <summary>滚动区域的部件。</summary>
public enum ScrollAreaPart
{
    /// <summary>外层（决定尺寸，裁掉超出的部分）。</summary>
    Root,

    /// <summary>视口：真正滚动的元素。</summary>
    Viewport,

    /// <summary>内容层（视口里包着内容的 div）。</summary>
    Content,

    /// <summary>滚动条的轨道（两个方向都用它，data-axis 区分）。</summary>
    Scrollbar,

    /// <summary>滑块；大小与位置由 JS 写在行内样式上。</summary>
    Thumb,
}

/// <summary>可以滚动的方向。</summary>
public enum ScrollAxis
{
    /// <summary>纵向（默认），横向超出的部分裁掉。</summary>
    Y,

    /// <summary>横向，纵向超出的部分裁掉。</summary>
    X,

    /// <summary>两个方向。</summary>
    Both,
}

/// <summary>滚动条什么时候显示。</summary>
public enum ScrollbarVisibility
{
    /// <summary>指针在区域上方或正在滚动时（默认）。</summary>
    Hover,

    /// <summary>只在滚动时，停下后淡出。</summary>
    Scroll,

    /// <summary>内容超出时一直显示。</summary>
    Always,
}

/// <summary>
/// 滚动区域的样式（配方，同 vue-tsx-pro 的 scrollAreaStyles、scrollAreaBarStyles）：原生滚动条藏起来，
/// 换成半透明、绝对定位浮在内容上的滚动条，出现与消失都不占位。轨道平时透明、不接指针，JS 写 data-state="visible" 时显示。
/// </summary>
public static class ScrollAreaStyles
{
    // 滚动条的粗细：0.5em 的滑块加两侧各一个边框宽的留白
    static readonly string BarSize = $"calc({Space.Sm} + 2 * {BorderWidths.Sm})";

    /// <summary>某个方向、某套定制下各部件的默认层。</summary>
    public static PartLayers<ScrollAreaPart> Of(ScrollAxis axis, ComponentStyles styles) =>
        Recipe.Of("scroll-area", axis, styles, static (ScrollAxis axis, ScrollAreaPart part) => part switch
        {
            ScrollAreaPart.Root => Root,
            ScrollAreaPart.Viewport => Sx.Of(Viewport, SharedStyles.FocusRing, axis switch
            {
                ScrollAxis.X => ScrollX,
                ScrollAxis.Both => ScrollBoth,
                _ => ScrollY,
            }),
            ScrollAreaPart.Content => axis == ScrollAxis.Y ? Sx.Of() : WideContent,
            ScrollAreaPart.Scrollbar => Track,
            _ => Thumb,
        });

    // 轨道与滑块按方向的一层（组件管理的层，不随渲染新建）
    static readonly Attrs[] bars =
    [
        new() { Sx = Sx.Of(TrackX) },
        new() { Sx = Sx.Of(TrackX, CornerX) },
        new() { Sx = Sx.Of(TrackY) },
        new() { Sx = Sx.Of(TrackY, CornerY) },
    ];

    static readonly Attrs thumbX = new() { Sx = ThumbX };
    static readonly Attrs thumbY = new() { Sx = ThumbY };

    /// <summary>某个方向的轨道；两个方向都有时让出右下角，两条滚动条不重叠。</summary>
    internal static Attrs Bar(bool vertical, bool corner) => bars[(vertical ? 2 : 0) + (corner ? 1 : 0)];

    /// <summary>滑块在轨道里的方向。</summary>
    internal static Attrs ThumbOn(bool vertical) => vertical ? thumbY : thumbX;

    // flex 纵向：使用方只给 max-height 时视口也能被限制住（视口 min-height: 0 可以收缩）
    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Overflow = Overflow.Hidden,
    });

    static Style Viewport => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinHeight = 0,
        // 隐藏原生滚动条（标准属性：Chromium 121、Firefox、Safari 18.2 起）
        ScrollbarWidth = ScrollbarWidth.None,
        // 焦点环画在里面：外层 overflow: hidden 会裁掉画在外面的
        [When.FocusVisible] = new() { OutlineOffset = $"calc(-1 * {BorderWidths.Sm})" },
    });

    static Style ScrollY => Css.Define(static () => new Style { OverflowX = Overflow.Hidden, OverflowY = Overflow.Auto });

    static Style ScrollX => Css.Define(static () => new Style { OverflowX = Overflow.Auto, OverflowY = Overflow.Hidden });

    static Style ScrollBoth => Css.Define(static () => new Style { Overflow = Overflow.Auto });

    // 横向滚动时内容按自己的宽度撑开，宽度变化也能被 ResizeObserver 观察到
    static Style WideContent => Css.Define(static () => new Style { Display = Display.Table, MinWidth = CssLength.Percent(100) });

    static Style Track => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        ZIndex = 1,
        BoxSizing = BoxSizing.BorderBox,
        TouchAction = TouchAction.None,
        UserSelect = UserSelect.None,
        Opacity = 0,
        PointerEvents = PointerEvents.None,
        Transition = $"opacity {Durations.Md} {Easings.Standard}",
        [When.Attribute("""[data-state="visible"]""")] = new() { Opacity = 1, PointerEvents = PointerEvents.Auto },
    });

    static Style TrackY => Css.Define(static () => new Style { InsetBlock = 0, InsetInlineEnd = 0, Width = BarSize });

    static Style TrackX => Css.Define(static () => new Style { InsetInline = 0, Bottom = 0, Height = BarSize });

    static Style CornerY => Css.Define(static () => new Style { Bottom = BarSize });

    static Style CornerX => Css.Define(static () => new Style { InsetInlineEnd = BarSize });

    // 半透明：浮在内容上也不完全遮住下面的文字
    static Style Thumb => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        BorderRadius = Radii.Full,
        BackgroundColor = $"color-mix(in srgb, {Colors.TextMuted} 50%, transparent)",
        Transition = $"background-color {Durations.Sm} {Easings.Standard}",
        [When.Hover] = new() { BackgroundColor = $"color-mix(in srgb, {Colors.TextMuted} 75%, transparent)" },
    });

    static Style ThumbY => Css.Define(static () => new Style { Top = 0, InsetInline = BorderWidths.Sm });

    static Style ThumbX => Css.Define(static () => new Style { InsetInlineStart = 0, InsetBlock = BorderWidths.Sm });
}
