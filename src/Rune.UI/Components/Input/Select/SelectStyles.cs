using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 下拉选择自己的部件（同 vue-tsx-pro 的 selectStyles）。外壳、前置图标、清空按钮、下拉箭头是输入框的部件（<see cref="InputPart"/>）：
/// 按 InputPart 定制时下拉选择一起变，同 vue-tsx-pro 里 styles.Input 也管 Select 的外壳。
/// </summary>
public enum SelectPart
{
    /// <summary>触发按钮（button，role="combobox"）：焦点一直在它上面。</summary>
    Trigger,

    /// <summary>触发按钮里已选内容那一层。</summary>
    Value,

    /// <summary>没有选择时的占位文字。</summary>
    Placeholder,
}

/// <summary>
/// 多选时触发框里最多显示几个已选标签（同 vue-tsx-pro 的 maxTagCount）：数字，或 <see cref="Responsive"/>（排成一行、按宽度放）；
/// 其余收进“+N”。可以直接写数字：<c>MaxTagCount="2"</c>。
/// </summary>
/// <param name="Count">最多显示几个；<see cref="Responsive"/> 时为 null。</param>
public readonly record struct TagLimit(int? Count)
{
    /// <summary>排成一行、按宽度放，放不下的收进“+N”（至少显示一个）。</summary>
    public static readonly TagLimit Responsive = new(null);

    /// <summary>按宽度放。</summary>
    public bool IsResponsive => Count is null;

    /// <summary>最多显示几个。</summary>
    public static implicit operator TagLimit(int count) => new(count);
}

/// <summary>下拉选择的样式：触发按钮去掉按钮的默认外观，外壳用 <see cref="InputStyles"/>（<see cref="InputAppearance.Picker"/>）。</summary>
public static class SelectStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<SelectPart> Of(ComponentStyles styles) =>
        Recipe.Of("select", styles, static (SelectPart part) => part switch
        {
            SelectPart.Trigger => Trigger,
            SelectPart.Value => Value,
            _ => Placeholder,
        });

    /// <summary>多选时标签排在触发按钮前面、占剩余宽度，触发按钮里只剩读屏用的文字。</summary>
    internal static Style TriggerBehindTags => Css.Define(static () => new Style { FlexGrow = 0 });

    /// <summary>下拉箭头（输入框的图标部件）打开时转半圈：外壳（它的父元素）的 data-state 由组件与 JS 初始化器写。</summary>
    internal static Style Chevron => Css.Define(static () => new Style
    {
        Transition = $"rotate {Durations.Sm} {Easings.Standard}",
        [When.Parent(When.Attribute("[data-state=open]"))] = new() { Rotate = "180deg" },
    });

    // 去掉按钮的默认外观；像输入框一样没有自己的焦点环（焦点环画在外壳上）
    static Style Trigger => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        AlignSelf = AlignSelf.Stretch,
        FlexGrow = 1,
        MinWidth = 0,
        Margin = 0,
        Padding = 0,
        BorderWidth = 0,
        BackgroundColor = "transparent",
        Color = CssWide.Inherit,
        FontFamily = CssWide.Inherit,
        FontSize = CssWide.Inherit,
        LineHeight = CssWide.Inherit,
        TextAlign = TextAlign.Start,
        TextDecorationLine = TextDecorationLine.None,
        Cursor = CssWide.Inherit,
        UserSelect = UserSelect.None,
        TouchAction = TouchAction.Manipulation,
        WebkitTapHighlightColor = "transparent",
        OutlineStyle = "none",
    });

    static Style Value => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        MinWidth = 0,
        PaddingBlock = Space.Xs,
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
    });

    // 是真正的文字（不是原生 placeholder），要满足 4.5:1：用 TextMuted 而不是 TextPlaceholder
    static Style Placeholder => Css.Define(static () => new Style { Color = Colors.TextMuted });
}
