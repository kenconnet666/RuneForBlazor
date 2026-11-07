using Rune.Styles;

namespace Rune.UI;

/// <summary>整页月历的部件（同 vue-tsx-pro 的 CalendarBoard）。</summary>
public enum CalendarBoardPart
{
    /// <summary>根元素（div role="group"）。</summary>
    Root,

    /// <summary>页头：标题、工具栏、今天、上个月 / 下个月。</summary>
    Header,

    /// <summary>标题（年月）。</summary>
    Title,

    /// <summary>网格（table role="grid"）。</summary>
    Grid,

    /// <summary>星期表头。</summary>
    Weekday,

    /// <summary>一天的格子（td）。</summary>
    Cell,

    /// <summary>日期数字（可聚焦的按钮）。</summary>
    DayButton,

    /// <summary>日期旁边的小字（节假日、休、班）。</summary>
    Note,

    /// <summary>那天的数据（ul）。</summary>
    Events,

    /// <summary>一条数据的默认显示（带色调的一行文字）。</summary>
    Event,

    /// <summary>“还有 N 项”。</summary>
    More,
}

/// <summary>整页月历的样式（同 vue-tsx-pro 的 calendarBoardStyles）。格子的状态写在 data-* 上：今天、补齐的上下月、选中、周末、不可选、只读。</summary>
public static class CalendarBoardStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<CalendarBoardPart> Of(ComponentStyles styles) =>
        Recipe.Of("calendar-board", styles, static (CalendarBoardPart part) => part switch
        {
            CalendarBoardPart.Root => Root,
            CalendarBoardPart.Header => Header,
            CalendarBoardPart.Title => Title,
            CalendarBoardPart.Grid => Grid,
            CalendarBoardPart.Weekday => Weekday,
            CalendarBoardPart.Cell => Cell,
            CalendarBoardPart.DayButton => Sx.Of(SharedStyles.ButtonReset, SharedStyles.InteractionColors, DayButton, SharedStyles.FocusRing),
            CalendarBoardPart.Note => Note,
            CalendarBoardPart.Events => Events,
            CalendarBoardPart.Event => Event,
            _ => Sx.Of(SharedStyles.ButtonReset, SharedStyles.InteractionColors, More, SharedStyles.FocusRing),
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Sm, MinWidth = 0 });

    static Style Header => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, FlexWrap = FlexWrap.Wrap, Gap = Space.Sm });

    static Style Title => Css.Define(static () => new Style { FontSize = FontSizes.Lg, FontWeight = FontWeights.Semibold, MarginInlineEnd = CssLength.Auto });

    static Style Grid => Css.Define(static () => new Style
    {
        Width = CssLength.Percent(100),
        TableLayout = TableLayout.Fixed,
        BorderCollapse = BorderCollapse.Collapse,
        BorderSpacing = 0,
    });

    static Style Weekday => Css.Define(static () => new Style
    {
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        Color = Colors.TextMuted,
        FontWeight = FontWeights.Normal,
        FontSize = FontSizes.Sm,
        TextAlign = TextAlign.Start,
    });

    static Style Cell => Css.Define(static () => new Style
    {
        Height = CssLength.Em(7),
        Padding = Space.Xs,
        VerticalAlign = VerticalAlign.Top,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
        Cursor = Cursor.Pointer,
        Transition = $"background-color {Durations.Sm} {Easings.Standard}",
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        [When.Attribute("[data-weekend]")] = new() { BackgroundColor = Colors.SurfaceMuted, [When.Hover] = new() { BackgroundColor = Colors.FillHover } },
        [When.Attribute("[data-outside]")] = new() { Color = Colors.TextMuted },
        [When.Attribute("[aria-selected=\"true\"]")] = new()
        {
            BackgroundColor = Tones.Accent.Soft,
            BoxShadow = $"inset 0 0 0 {BorderWidths.Sm} {Tones.Accent.Border}",
            [When.Hover] = new() { BackgroundColor = Tones.Accent.SoftHover },
        },
        [When.Attribute("[aria-disabled=\"true\"]")] = new() { Cursor = Cursor.NotAllowed, Opacity = Opacities.Disabled },
        // 只读：格子不像能点
        [When.Attribute("[data-readonly]")] = new() { Cursor = Cursor.Default },
    });

    /// <summary>日期数字与小字那一行。</summary>
    internal static Style DayHead => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Xs, MinWidth = 0 });

    static Style DayButton => Css.Define(static () => new Style
    {
        Display = Display.InlineGrid,
        PlaceItems = PlaceItems.Center,
        FlexShrink = 0,
        MinWidth = CssLength.Em(1.75),
        Height = CssLength.Em(1.75),
        PaddingInline = Space.Xs,
        BorderRadius = Radii.Full,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        FontWeight = FontWeights.Medium,
        [When.Attribute("[data-today]")] = new() { BackgroundColor = Tones.Accent.Solid, Color = Tones.Accent.Contrast },
    });

    static Style Note => Css.Define(static () => new Style
    {
        MinWidth = 0,
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
        FontSize = FontSizes.Xs,
        Color = Colors.TextMuted,
    });

    static Style Events => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = CssLength.Rem(0.125),
        MarginBlock = Space.Xs,
        MarginInline = 0,
        Padding = 0,
        ListStyle = ListStyle.None,
    });

    static Style Event => Css.Define(static () => new Style
    {
        Display = Display.Block,
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
        PaddingInline = Space.Xs,
        BorderRadius = Radii.Sm,
        FontSize = FontSizes.Xs,
        LineHeight = 1.6,
    });

    static Style More => Css.Define(static () => new Style
    {
        PaddingInline = Space.Xs,
        BorderRadius = Radii.Sm,
        FontSize = FontSizes.Xs,
        Color = Colors.TextMuted,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
    });
}
