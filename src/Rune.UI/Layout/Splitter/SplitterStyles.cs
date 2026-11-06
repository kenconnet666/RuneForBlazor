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
    static readonly Attrs HandleHorizontal = new() { Sx = Sx.Of(HandleStyle, HorizontalSize, ColResize, SharedStyles.FocusRing) };
    static readonly Attrs HandleVertical = new() { Sx = Sx.Of(HandleStyle, VerticalSize, RowResize, SharedStyles.FocusRing) };
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

    // 看得见的是中间一条细线，能拖的范围更宽；悬停、拖动时变成强调色
    static Style HandleStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        TouchAction = TouchAction.None,
        Color = Colors.BorderMuted,
        [When.Hover] = new() { Color = Colors.BorderStrong },
        [When.FocusVisible] = new() { Color = Tones.Accent.Solid },
    });

    static Style ColResize => Css.Define(static () => new Style { Cursor = Cursor.ColResize });

    static Style RowResize => Css.Define(static () => new Style { Cursor = Cursor.RowResize });

    static Style LineStyle => Css.Define(static () => new Style { Position = Position.Absolute, BackgroundColor = CssColors.CurrentColor, PointerEvents = PointerEvents.None });

    // 细线宽取主题的边框宽度，减去一半居中
    static Style LineHorizontalStyle => Css.Define(static () => new Style
    {
        InsetBlock = 0,
        InsetInlineStart = $"calc(50% - {BorderWidths.Xs} / 2)",
        Width = BorderWidths.Xs,
    });

    static Style LineVerticalStyle => Css.Define(static () => new Style
    {
        InsetInline = 0,
        Top = $"calc(50% - {BorderWidths.Xs} / 2)",
        Height = BorderWidths.Xs,
    });

    // 收起、展开的小按钮压在线上：背景盖住线
    static Style ToggleStyle => Css.Define(static () => new Style { Position = Position.Relative, ZIndex = 1, BackgroundColor = Colors.Surface, Cursor = Cursor.Pointer });
}
