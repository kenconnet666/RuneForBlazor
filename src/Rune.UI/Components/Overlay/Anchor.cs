using Rune.Styles;

namespace Rune.UI;

/// <summary>浮层相对触发元素的位置；放不下时自动翻到对面。</summary>
public enum Placement
{
    /// <summary>下方（默认）。</summary>
    Bottom,

    /// <summary>上方。</summary>
    Top,

    /// <summary>起始边（从左到右的页面是左边）。</summary>
    Start,

    /// <summary>结束边。</summary>
    End,
}

/// <summary>
/// 锚点定位：浮层贴着触发元素，用 CSS 锚点定位（<c>anchor-name</c> / <c>position-anchor</c> / <c>position-area</c>），
/// 滚动、缩放时由浏览器跟着走，不需要 JS 计算。不支持锚点定位的浏览器由 JS 初始化器在打开时按触发元素摆放一次。
/// </summary>
public static class Anchor
{
    /// <summary>触发元素上的属性：锚点名（写在 style 上，名字每个实例不同，不进样式类）。</summary>
    public static Attrs Trigger(string overlayId) => new() { InlineStyle = $"anchor-name:{Name(overlayId)}" };

    /// <summary>浮层上的属性：跟随的锚点。</summary>
    public static Attrs Overlay(string overlayId) => new() { InlineStyle = $"position-anchor:{Name(overlayId)}" };

    static string Name(string overlayId) => $"--{overlayId}";

    /// <summary>
    /// 浮层的位置样式。<paramref name="alignStart"/> 时与触发元素的起始边对齐（下拉菜单），否则居中（提示）。
    /// 放不下时翻到对面（<c>position-try-fallbacks</c>），与触发元素之间留一点间隙。
    /// </summary>
    public static Style Position(Placement placement, bool alignStart) => Css.Define((placement, alignStart), static key => key.placement switch
    {
        Placement.Top => new Style
        {
            PositionArea = key.alignStart ? "block-start span-inline-end" : "block-start",
            PositionTryFallbacks = "flip-block",
            MarginBlockEnd = Space.Xs,
        },
        Placement.Start => new Style
        {
            PositionArea = key.alignStart ? "inline-start span-block-end" : "inline-start",
            PositionTryFallbacks = "flip-inline",
            MarginInlineEnd = Space.Xs,
        },
        Placement.End => new Style
        {
            PositionArea = key.alignStart ? "inline-end span-block-end" : "inline-end",
            PositionTryFallbacks = "flip-inline",
            MarginInlineStart = Space.Xs,
        },
        _ => new Style
        {
            PositionArea = key.alignStart ? "block-end span-inline-end" : "block-end",
            PositionTryFallbacks = "flip-block",
            MarginBlockStart = Space.Xs,
        },
    });

    /// <summary>JS 兜底定位要知道的位置（<c>data-placement</c>）。</summary>
    public static string NameOf(Placement placement) => placement switch
    {
        Placement.Top => "top",
        Placement.Start => "start",
        Placement.End => "end",
        _ => "bottom",
    };
}
