using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 相连的一组（ControlGroup、ButtonGroup）交给里面控件的信息（级联值）：排列方向。
/// 输入框外壳与按钮家族据此把 <see cref="AttachedStyles"/> 加到自己身上（同 vue-tsx-pro 的 AttachedContext）。
/// </summary>
/// <param name="Orientation">排列方向：决定去掉哪一侧的圆角。</param>
public sealed record AttachedGroup(Orientation Orientation);

/// <summary>
/// 组里相连的控件（同 vue-tsx-pro 的 attachedStyles）：去掉相邻一侧的圆角，边框叠在一起（不显得加粗）；
/// 悬停、聚焦的那个浮到上面，边框与焦点环不被邻居盖住。控件的外壳要是组的直接子元素；判断首尾时跳过不占位的兄弟
/// （选择框的弹出面板、隐藏的 input），用 <c>:nth-child(1 of …)</c>。只在“不是第一个 / 最后一个”时设值，两端照常用控件自己的圆角。
/// </summary>
public static class AttachedStyles
{
    /// <summary>某个方向的相连样式；不在组里（null）时没有。</summary>
    public static Style? Of(Orientation? orientation) => orientation switch
    {
        Orientation.Horizontal => Horizontal,
        Orientation.Vertical => Vertical,
        _ => null,
    };

    // 不占位的兄弟不算首尾
    const string Placed = ":not([popover], [hidden], input[type=\"hidden\"], template)";
    static readonly Condition NotFirst = new($":not(:nth-child(1 of {Placed}))");
    static readonly Condition NotLast = new($":not(:nth-last-child(1 of {Placed}))");

    static Style Horizontal => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        [When.Hover] = new() { ZIndex = 1 },
        [When.FocusVisible] = new() { ZIndex = 2 },
        [When.FocusWithin] = new() { ZIndex = 2 },
        [NotFirst] = new()
        {
            MarginInlineStart = $"calc(-1 * {BorderWidths.Xs})",
            BorderStartStartRadius = 0,
            BorderEndStartRadius = 0,
        },
        [NotLast] = new() { BorderStartEndRadius = 0, BorderEndEndRadius = 0 },
    });

    static Style Vertical => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        [When.Hover] = new() { ZIndex = 1 },
        [When.FocusVisible] = new() { ZIndex = 2 },
        [When.FocusWithin] = new() { ZIndex = 2 },
        [NotFirst] = new()
        {
            MarginBlockStart = $"calc(-1 * {BorderWidths.Xs})",
            BorderStartStartRadius = 0,
            BorderStartEndRadius = 0,
        },
        [NotLast] = new() { BorderEndStartRadius = 0, BorderEndEndRadius = 0 },
    });
}
