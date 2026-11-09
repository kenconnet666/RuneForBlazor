using Rune.Styles;

namespace Rune.UI;

/// <summary>轮播的部件（同 vue-tsx-pro 的 Carousel）。</summary>
public enum CarouselPart
{
    /// <summary>section。</summary>
    Root,

    /// <summary>滚动区域。</summary>
    Viewport,

    /// <summary>每一张的外层。</summary>
    Slide,

    /// <summary>上一张按钮（IconButton）。</summary>
    Previous,

    /// <summary>下一张按钮（IconButton）。</summary>
    Next,

    /// <summary>底部的控制条（暂停按钮与页点）。</summary>
    Controls,

    /// <summary>自动播放时的“暂停 / 播放”按钮（IconButton）。</summary>
    Pause,

    /// <summary>页点（ButtonBase）。</summary>
    Dot,
}

/// <summary>轮播的样式（同 vue-tsx-pro 的 carouselStyles）：一次看几张与间距经变量给。</summary>
public static class CarouselStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    internal static readonly CssVar<CssLength> GapVar = CssVar.Length("rune-carousel-gap", "0px");
    internal static readonly CssVar<CssValue> PerViewVar = CssVar.Value("rune-carousel-per-view");
    static readonly Condition Paused = When.Inside(When.Attribute("[data-paused]"), Css.Marker("rune-carousel"));

    /// <summary>各部件的默认层。</summary>
    public static PartLayers<CarouselPart> Of(ComponentStyles styles) =>
        Recipe.Of("carousel", styles, static (CarouselPart part) => part switch
        {
            CarouselPart.Root => Root,
            CarouselPart.Viewport => Sx.Of(Viewport, SharedStyles.FocusRingInset),
            CarouselPart.Slide => Slide,
            CarouselPart.Previous => Sx.Of(Arrow, Previous),
            CarouselPart.Next => Sx.Of(Arrow, Next),
            CarouselPart.Controls => Controls,
            CarouselPart.Pause => Sx.Of(),
            _ => Dot,
        });

    /// <summary>根元素的标记：暂停按钮按它（data-paused）换图标。</summary>
    internal static readonly Marker RootMarker = Css.Marker("rune-carousel");

    /// <summary>暂停时显示的“播放”图标、播放时显示的“暂停”图标。</summary>
    internal static readonly Attrs PlayIcon = new() { Sx = Css.Define(static () => new Style { Display = Display.None, [Paused] = new() { Display = Display.Inline } }) };

    internal static readonly Attrs PauseIcon = new() { Sx = Css.Define(static () => new Style { [Paused] = new() { Display = Display.None } }) };

    static Style Root => Css.Define(static () => new Style { Position = Position.Relative, MinWidth = 0 });

    static Style Viewport => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Flex,
        Gap = GapVar,
        OverflowX = Overflow.Auto,
        OverflowY = Overflow.Hidden,
        ScrollSnapType = ScrollSnapType.XMandatory,
        OverscrollBehaviorX = OverscrollBehaviorX.Contain,
        ScrollbarWidth = ScrollbarWidth.None,
    });

    // 一次看几张：每张占（宽度减去间距）的几分之一
    static Style Slide => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        MinWidth = 0,
        ScrollSnapAlign = ScrollSnapAlign.Start,
        FlexBasis = $"calc((100% - ({PerViewVar.Reference} - 1) * {GapVar.Reference}) / {PerViewVar.Reference})",
    });

    static Style Arrow => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetBlockStart = CssLength.Percent(50),
        Translate = "0 -50%",
        BoxShadow = Shadows.Sm,
    });

    static Style Previous => Css.Define(static () => new Style { InsetInlineStart = Space.Sm });

    static Style Next => Css.Define(static () => new Style { InsetInlineEnd = Space.Sm });

    static Style Controls => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetBlockEnd = Space.Sm,
        InsetInlineStart = CssLength.Percent(50),
        Translate = "-50% 0",
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = 0,
        PaddingInline = Space.Xs,
        BorderRadius = Radii.Full,
        BackgroundColor = $"color-mix(in srgb, {Colors.Surface} 75%, transparent)",
        BackdropFilter = "blur(4px)",
    });

    // 点击区域够大（24px），看得见的点是 ::after；当前页（JS 写 data-active）的点拉长、强调色
    static Style Dot => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        Width = CssLength.Em(1.25),
        Height = $"max(1.5em, {Sizes.MinTarget})",
        Transition = $"width {Durations.Md} {Easings.Standard}",
        [Pseudo.After] = new()
        {
            Content = "\"\"",
            Width = CssLength.Em(0.5),
            Height = CssLength.Em(0.5),
            BorderRadius = Radii.Full,
            BackgroundColor = Colors.BorderStrong,
            Transition = $"width {Durations.Md} {Easings.Standard}, background-color {Durations.Md} {Easings.Standard}",
        },
        // 悬停时圆点加深：页点也是按钮
        [When.Hover] = new() { [Pseudo.After] = new() { BackgroundColor = Colors.TextMuted } },
        [When.Attribute("[data-active]")] = new()
        {
            Width = CssLength.Em(2),
            [Pseudo.After] = new() { Width = CssLength.Em(1.25), BackgroundColor = Tones.Accent.Solid },
        },
    });
}
