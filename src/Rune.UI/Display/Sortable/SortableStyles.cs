using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 可拖动排序的列表的样式（同 vue-tsx-pro 的 sortableStyles）。拖动中的位移由 JS 写在项上（transform），
/// 让位的项带过渡；被拖着的（data-dragging）不带过渡、浮在上面；键盘拿起的（data-grabbed）同样浮起。
/// 顺序跟上后 JS 在列表上写 data-settling，去掉位移时不要过渡。
/// </summary>
internal static class SortableStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    static readonly Marker ListMarker = Css.Marker("rune-sortable");
    static readonly Condition Lifted = When.Attribute("[data-dragging],[data-grabbed]");
    static readonly Condition Dragging = When.Attribute("[data-dragging]");
    static readonly Condition Settling = When.Inside(When.Attribute("[data-settling]"), ListMarker);

    internal static readonly Attrs List = ListMarker.Attrs;
    internal static readonly Attrs Item = new() { Sx = ItemStyle };
    internal static readonly Attrs ItemHorizontal = new() { Sx = Sx.Of(ItemStyle, HorizontalStyle) };
    internal static readonly Attrs Handle = new() { Sx = HandleStyle };
    internal static readonly Attrs Content = new() { Sx = ContentStyle };

    static Style ItemStyle => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        PaddingBlock = Space.Xs,
        // 起始边是把手（自带留白），结束边多留一点，文字不贴着边框
        PaddingInlineStart = Space.Xs,
        PaddingInlineEnd = Space.Sm,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Surface,
        Color = Colors.Text,
        TransitionProperty = "transform, box-shadow, border-color",
        TransitionDuration = Durations.Sm,
        TransitionTimingFunction = Easings.Standard,
        // 拿起时浮起来：阴影与品牌色边框，盖在其他项上面
        [Lifted] = new() { ZIndex = 1, BorderColor = Tones.Accent.Solid, BoxShadow = Shadows.Md },
        [Dragging] = new() { TransitionProperty = "box-shadow, border-color" },
        [Settling] = new() { TransitionProperty = TransitionProperty.None },
        // 减少动态效果：让位、滑到落点都直接到位
        [Media.ReducedMotion] = new() { TransitionProperty = TransitionProperty.None },
    });

    // 横排时项不收缩：放不下就溢出（外面包 ScrollArea 横向滚动），不把文字挤成一字一行
    static Style HorizontalStyle => Css.Define(static () => new Style { FlexShrink = 0 });

    // 去掉按钮默认外观、焦点环由 ButtonBase 给出
    static Style HandleStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        FlexShrink = 0,
        Width = $"max(1.75em, {Sizes.MinTarget})",
        Height = $"max(1.75em, {Sizes.MinTarget})",
        BorderRadius = Radii.Sm,
        BackgroundColor = CssColors.Transparent,
        Color = Colors.TextMuted,
        Cursor = Cursor.Grab,
        // 触摸拖动时不滚动页面
        TouchAction = TouchAction.None,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        [When.Active] = new() { Cursor = Cursor.Grabbing },
    });

    static Style ContentStyle => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0 });
}
