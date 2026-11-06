using Rune.Styles;

namespace Rune.UI;

/// <summary>跑马灯滚动的方向。</summary>
public enum MarqueeDirection
{
    /// <summary>向左（默认）。</summary>
    Left,

    /// <summary>向右。</summary>
    Right,

    /// <summary>向上。</summary>
    Up,

    /// <summary>向下。</summary>
    Down,
}

/// <summary>
/// 跑马灯的样式（同 vue-tsx-pro 的 marqueeStyles）：两份内容并排，动画平移 -50% 正好是一份（含它后面的间距），无缝循环。
/// 一圈的时长由 JS 按一份内容的长度与速度算出，写在 --rune-marquee-duration 上；减少动态效果时不滚动，变成可以手动滚动的区域。
/// </summary>
internal static class MarqueeStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它
    static readonly CssVar<CssValue> Duration = CssVar.Value("rune-marquee-duration");

    /// <summary>外层：横排或竖排，要不要两端渐隐。</summary>
    internal static Attrs Root(bool vertical, bool fade) => Roots[(vertical ? 2 : 0) + (fade ? 1 : 0)];

    /// <summary>做动画的那一层：方向、悬停或聚焦时暂停、外部暂停。</summary>
    internal static Attrs Track(MarqueeDirection direction, bool pauseOnHover, bool paused) =>
        new() { Sx = Sx.Of(TrackStyle, Vertical(direction) ? TrackVertical : null, direction is MarqueeDirection.Right or MarqueeDirection.Down ? Reverse : null, pauseOnHover ? Pausable : null, paused ? Paused : null) };

    /// <summary>一份内容：项之间与这一份后面都是同样的间距（两份之间与项之间一样宽）；复制的那份在不滚动时不显示。</summary>
    internal static Attrs Group(bool vertical, SpaceScale gap, bool copy) =>
        new() { Sx = Sx.Of(GroupStyle, vertical ? GroupVertical : null, ScaleStyles.Gap(gap), Trail(vertical, gap), copy ? Copy : null) };

    internal static bool Vertical(MarqueeDirection direction) => direction is MarqueeDirection.Up or MarqueeDirection.Down;

    static readonly Attrs[] Roots =
    [
        new() { Sx = RootStyle },
        new() { Sx = Sx.Of(RootStyle, Fade) },
        new() { Sx = Sx.Of(RootStyle, RootVertical) },
        new() { Sx = Sx.Of(RootStyle, RootVertical, FadeVertical) },
    ];

    static Keyframes ScrollX => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { Transform = "none" },
        ["to"] = new() { Transform = "translateX(-50%)" },
    });

    static Keyframes ScrollY => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { Transform = "none" },
        ["to"] = new() { Transform = "translateY(-50%)" },
    });

    static Style RootStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        Overflow = Overflow.Hidden,
        [Media.ReducedMotion] = new() { Overflow = Overflow.Auto },
        [When.FocusVisible] = new() { OutlineWidth = BorderWidths.Md, OutlineStyle = OutlineStyle.Solid, OutlineColor = Colors.FocusRing },
    });

    static Style RootVertical => Css.Define(static () => new Style { FlexDirection = FlexDirection.Column });

    static Style Fade => Css.Define(static () => new Style { MaskImage = "linear-gradient(to right, transparent, black 10%, black 90%, transparent)" });

    static Style FadeVertical => Css.Define(static () => new Style { MaskImage = "linear-gradient(to bottom, transparent, black 10%, black 90%, transparent)" });

    static Style TrackStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexShrink = 0,
        Width = Width.MaxContent,
        Keyframes = ScrollX,
        AnimationDuration = Duration,
        AnimationTimingFunction = AnimationTimingFunction.Linear,
        AnimationIterationCount = "infinite",
        [Media.ReducedMotion] = new() { AnimationName = "none" },
    });

    static Style TrackVertical => Css.Define(static () => new Style
    {
        FlexDirection = FlexDirection.Column,
        Width = CssLength.Auto,
        Height = Height.MaxContent,
        Keyframes = ScrollY,
        [Media.ReducedMotion] = new() { AnimationName = "none" },
    });

    static Style Reverse => Css.Define(static () => new Style { AnimationDirection = AnimationDirection.Reverse });

    static Style Pausable => Css.Define(static () => new Style
    {
        AnimationPlayState = AnimationPlayState.Running,
        [When.Hover] = new() { AnimationPlayState = AnimationPlayState.Paused },
        [When.FocusWithin] = new() { AnimationPlayState = AnimationPlayState.Paused },
    });

    static Style Paused => Css.Define(static () => new Style { AnimationPlayState = AnimationPlayState.Paused });

    static Style GroupStyle => Css.Define(static () => new Style { Display = Display.Flex, FlexShrink = 0, AlignItems = AlignItems.Center });

    static Style GroupVertical => Css.Define(static () => new Style { FlexDirection = FlexDirection.Column });

    static Style Trail(bool vertical, SpaceScale gap) => Css.Define((vertical, gap), static k => k.vertical
        ? new Style { PaddingBlockEnd = ScaleStyles.SpaceOf(k.gap) }
        : new Style { PaddingInlineEnd = ScaleStyles.SpaceOf(k.gap) });

    // 不滚动时用不着复制的那份
    static Style Copy => Css.Define(static () => new Style { [Media.ReducedMotion] = new() { Display = Display.None } });
}
