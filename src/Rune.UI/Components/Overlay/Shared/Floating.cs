using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 浮层相对触发元素的位置（同 vue-tsx-pro 的 FloatingPlacement）：主方向加对齐方式，共 12 种。
/// Start / End 对齐是逻辑方向，从右到左排版下自动镜像；空间不够时自动翻到另一侧、贴着视口平移。
/// </summary>
public enum Placement
{
    /// <summary>上方居中。</summary>
    Top,

    /// <summary>上方，与触发元素的起始边对齐。</summary>
    TopStart,

    /// <summary>上方，与触发元素的结束边对齐。</summary>
    TopEnd,

    /// <summary>下方居中。</summary>
    Bottom,

    /// <summary>下方，与触发元素的起始边对齐。</summary>
    BottomStart,

    /// <summary>下方，与触发元素的结束边对齐。</summary>
    BottomEnd,

    /// <summary>左侧居中。</summary>
    Left,

    /// <summary>左侧，与触发元素的顶边对齐。</summary>
    LeftStart,

    /// <summary>左侧，与触发元素的底边对齐。</summary>
    LeftEnd,

    /// <summary>右侧居中。</summary>
    Right,

    /// <summary>右侧，与触发元素的顶边对齐。</summary>
    RightStart,

    /// <summary>右侧，与触发元素的底边对齐。</summary>
    RightEnd,
}

/// <summary>
/// 锚定浮层（Popover、Menu、Tooltip，之后的 Select）：显示、隐藏、Esc、点击外部交给原生 popover，
/// 位置由 JS 初始化器用 Floating UI 计算（与 vue-tsx-pro 同一个库，第一次打开浮层时才加载）：
/// 浮层带 <c>data-rune-floating="bottom-start"</c>，打开时贴着触发元素（<c>popovertarget</c> 或 <c>data-rune-tooltip</c> 指向它的元素）定位并持续跟随，
/// 最终方向写到 <c>data-side</c> / <c>data-align</c>，缩放原点朝向触发元素；里面的 <c>data-rune-arrow</c> 是指向触发元素的箭头。
/// </summary>
public static class Floating
{
    /// <summary>Floating UI 的位置名（<c>bottom-start</c>）。</summary>
    public static string NameOf(Placement placement) => placement switch
    {
        Placement.Top => "top",
        Placement.TopStart => "top-start",
        Placement.TopEnd => "top-end",
        Placement.Bottom => "bottom",
        Placement.BottomStart => "bottom-start",
        Placement.BottomEnd => "bottom-end",
        Placement.Left => "left",
        Placement.LeftStart => "left-start",
        Placement.LeftEnd => "left-end",
        Placement.Right => "right",
        Placement.RightStart => "right-start",
        _ => "right-end",
    };

    /// <summary>浮层上交给 JS 的定位约定：位置，与触发元素的距离（px，不写时 6）。</summary>
    public static Attrs Placed(Placement placement, int? offset) => new()
    {
        ["data-rune-floating"] = NameOf(placement),
        ["data-offset"] = offset,
    };

    /// <summary>箭头元素上由组件管理的属性。</summary>
    public static readonly Attrs ArrowAttrs = new() { ["data-rune-arrow"] = true, AriaHidden = true };
}

/// <summary>锚定浮层共用的样式（同 vue-tsx-pro 的 floatingStyles）。</summary>
public static class FloatingStyles
{
    /// <summary>原生 popover 默认居中显示、带外边距，改为由 JS 写入的 left / top 定位。</summary>
    public static Style Anchored => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        Inset = CssLength.Auto,
        Margin = 0,
    });

    /// <summary>浮起的表面：边框、圆角、阴影（Popover、Menu、选项列表）；内边距各自定。</summary>
    public static Style Surface => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Lg,
        BackgroundColor = Colors.SurfaceRaised,
        Color = Colors.Text,
        BoxShadow = Shadows.Lg,
    });

    /// <summary>
    /// 箭头：一个小方块，只画右、下两条边，旋转后尖角朝向触发元素；底色、边框跟随浮层（inherit）。位置与旋转由 JS 写。
    /// </summary>
    public static Style Arrow => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        BoxSizing = BoxSizing.BorderBox,
        Width = CssLength.Em(0.625),
        Height = CssLength.Em(0.625),
        BackgroundColor = CssWide.Inherit,
        BorderStyle = BorderStyle.Solid,
        BorderColor = CssWide.Inherit,
        BorderWidth = 0,
        BorderRightWidth = CssWide.Inherit,
        BorderBottomWidth = CssWide.Inherit,
        BorderBottomRightRadius = BorderWidths.Sm,
        PointerEvents = PointerEvents.None,
    });

    /// <summary>箭头元素的默认层。</summary>
    internal static readonly Attrs ArrowLayer = new() { Sx = Arrow };

    /// <summary>有箭头的浮层：原生 popover 默认 overflow: auto 会把伸出去的箭头裁掉。</summary>
    public static Style WithArrow => Css.Define(static () => new Style { Overflow = Overflow.Visible });
}
