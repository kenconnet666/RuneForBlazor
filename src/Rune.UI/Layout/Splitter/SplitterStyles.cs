using Rune.Styles;

namespace Rune.UI;

/// <summary>分栏收起哪一侧。</summary>
public enum SplitterSide
{
    /// <summary>起始一栏（左或上）。</summary>
    Start,

    /// <summary>结束一栏（右或下）。</summary>
    End,
}

/// <summary>
/// 分栏的样式（同 vue-tsx-pro 的 splitterStyles）：三格的网格（起始一栏、分隔条、结束一栏），两栏的份额经 --rune-split-start / -end（fr）给，
/// 拖动时 JS 直接改这两个变量；收起的一侧（data-collapsed）那一格是 0，另一栏占满。
/// </summary>
internal static class SplitterStyles
{
    // 线的粗细：分隔条按状态给，里面的线读它（线看不到分隔条的悬停）；写在最前面，下面的静态 Attrs 初始化时就要用到
    static readonly CssVar<CssLength> LineWidth = CssVar.Length("rune-splitter-line");

    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    internal static readonly CssVar<CssValue> Start = CssVar.Value("rune-split-start");
    internal static readonly CssVar<CssValue> End = CssVar.Value("rune-split-end");
    static readonly Condition StartCollapsed = When.Attribute("[data-collapsed=start]");
    static readonly Condition EndCollapsed = When.Attribute("[data-collapsed=end]");

    internal static Attrs Root(bool horizontal, bool bordered) => Roots[(horizontal ? 0 : 2) + (bordered ? 1 : 0)];

    internal static readonly Attrs Pane = new() { Sx = PaneStyle };
    internal static readonly Attrs PaneScroll = new() { Sx = PaneScrollStyle };
    internal static readonly Attrs PaneContent = new() { Sx = PaneContentStyle };
    internal static readonly Attrs Toggle = new() { Sx = ToggleStyle };

    internal static Attrs Track(bool horizontal) => horizontal ? TrackHorizontal : TrackVertical;

    internal static Attrs Handle(bool horizontal) => horizontal ? HandleHorizontal : HandleVertical;

    internal static Attrs Line(bool horizontal) => horizontal ? LineHorizontal : LineVertical;

    static readonly Attrs[] Roots =
    [
        new() { Sx = Sx.Of(RootStyle, ColumnsStyle) },
        new() { Sx = Sx.Of(RootStyle, ColumnsStyle, BorderedStyle) },
        new() { Sx = Sx.Of(RootStyle, RowsStyle) },
        new() { Sx = Sx.Of(RootStyle, RowsStyle, BorderedStyle) },
    ];

    static readonly Attrs TrackHorizontal = new() { Sx = Sx.Of(TrackStyle, HorizontalSize) };
    static readonly Attrs TrackVertical = new() { Sx = Sx.Of(TrackStyle, VerticalSize) };
    static readonly Attrs HandleHorizontal = new() { Sx = Sx.Of(HandleStyle, HorizontalSize, ColResize) };
    static readonly Attrs HandleVertical = new() { Sx = Sx.Of(HandleStyle, VerticalSize, RowResize) };
    static readonly Attrs LineHorizontal = new() { Sx = Sx.Of(LineStyle, LineHorizontalStyle) };
    static readonly Attrs LineVertical = new() { Sx = Sx.Of(LineStyle, LineVerticalStyle) };

    static Style RootStyle => Css.Define(static () => new Style { Display = Display.Grid, MinWidth = 0, MinHeight = 0, Height = CssLength.Percent(100) });

    static Style ColumnsStyle => Css.Define(static () => new Style
    {
        GridTemplateColumns = $"minmax(0, {Start}) auto minmax(0, {End})",
        [StartCollapsed] = new() { GridTemplateColumns = "0 auto minmax(0, 1fr)" },
        [EndCollapsed] = new() { GridTemplateColumns = "minmax(0, 1fr) auto 0" },
    });

    static Style RowsStyle => Css.Define(static () => new Style
    {
        GridTemplateRows = $"minmax(0, {Start}) auto minmax(0, {End})",
        [StartCollapsed] = new() { GridTemplateRows = "0 auto minmax(0, 1fr)" },
        [EndCollapsed] = new() { GridTemplateRows = "minmax(0, 1fr) auto 0" },
    });

    static Style BorderedStyle => Css.Define(static () => new Style
    {
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Lg,
        BackgroundColor = Colors.Surface,
        Overflow = Overflow.Hidden,
    });

    static Style PaneStyle => Css.Define(static () => new Style { MinWidth = 0, MinHeight = 0, Overflow = Overflow.Hidden });

    static Style PaneScrollStyle => Css.Define(static () => new Style { Height = CssLength.Percent(100) });

    // 内容层和这一栏一样高：里面的表格、空状态写 100% 才有依据
    static Style PaneContentStyle => Css.Define(static () => new Style { Height = CssLength.Percent(100) });

    // 分隔条所在的格：分隔条铺满它，收起按钮叠在中间（不放进可聚焦的分隔条里：交互元素不能嵌套）
    static Style TrackStyle => Css.Define(static () => new Style { Position = Position.Relative, Display = Display.Flex, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center });

    static Style HorizontalSize => Css.Define(static () => new Style { Width = Space.Md });

    static Style VerticalSize => Css.Define(static () => new Style { Height = Space.Md });

    // 看得见的是中间一条细线，能拖的范围更宽。悬停、拖动时线加粗变深，键盘聚焦时加粗变成强调色（同 ResizeHandle、vue-tsx-pro）；
    // 不画焦点框：贯穿整栏的框两端会被容器裁掉
    static Style HandleStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        TouchAction = TouchAction.None,
        OutlineStyle = OutlineStyle.None,
        Color = Colors.BorderMuted,
        [LineWidth.Name] = BorderWidths.Xs.ToString(),
        [When.Hover] = new() { Color = Colors.BorderStrong, [LineWidth.Name] = BorderWidths.Md.ToString() },
        [When.Dragging] = new() { Color = Colors.BorderStrong, [LineWidth.Name] = BorderWidths.Md.ToString() },
        [When.FocusVisible] = new() { Color = Tones.Accent.Solid, [LineWidth.Name] = BorderWidths.Md.ToString() },
    });

    // 触屏上手指按不准这么窄的一条：能按到的范围向两边各扩一截（不小于 24px，WCAG 2.5.8），只扩命中区、不改布局
    static Style ColResize => Css.Define(static () => new Style
    {
        Cursor = Cursor.ColResize,
        [Pseudo.After] = new()
        {
            Content = "\"\"",
            Position = Position.Absolute,
            InsetBlock = 0,
            InsetInline = 0,
            [Media.CoarsePointer] = new() { InsetInline = $"calc(-1 * {Space.Sm})" },
        },
    });

    static Style RowResize => Css.Define(static () => new Style
    {
        Cursor = Cursor.RowResize,
        [Pseudo.After] = new()
        {
            Content = "\"\"",
            Position = Position.Absolute,
            InsetInline = 0,
            InsetBlock = 0,
            [Media.CoarsePointer] = new() { InsetBlock = $"calc(-1 * {Space.Sm})" },
        },
    });

    static Style LineStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        BackgroundColor = CssColors.CurrentColor,
        PointerEvents = PointerEvents.None,
        Transition = $"width {Durations.Sm} {Easings.Standard}, height {Durations.Sm} {Easings.Standard}, inset {Durations.Sm} {Easings.Standard}",
    });

    // 线宽取分隔条给的变量（平时是主题的边框宽度），减去一半居中
    static Style LineHorizontalStyle => Css.Define(static () => new Style
    {
        InsetBlock = 0,
        InsetInlineStart = $"calc(50% - {LineWidth} / 2)",
        Width = LineWidth,
    });

    static Style LineVerticalStyle => Css.Define(static () => new Style
    {
        InsetInline = 0,
        Top = $"calc(50% - {LineWidth} / 2)",
        Height = LineWidth,
    });

    // 收起、展开的小按钮压在线上：不透明的底盖住线；悬停换成同样不透明的填充色（实底会盖掉描边按钮自己的悬停）
    static Style ToggleStyle => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        ZIndex = 1,
        BackgroundColor = Colors.Surface,
        Cursor = Cursor.Pointer,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
    });
}
