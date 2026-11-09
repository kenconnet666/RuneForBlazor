using Rune.Styles;

namespace Rune.UI;

/// <summary>完成清单的样式（同 vue-tsx-pro 的 checklistStyles）：状态一列、内容一列、操作一列，状态贴着名称那一行。</summary>
internal static class ChecklistStyles
{
    internal static readonly Attrs Root = new() { Sx = RootStyle };
    internal static readonly Attrs Summary = new() { Sx = SummaryStyle };
    internal static readonly Attrs List = new() { Sx = ListStyle };
    internal static readonly Attrs Item = new() { Sx = ItemStyle };
    internal static readonly Attrs Status = new() { Sx = StatusStyle };
    internal static readonly Attrs Toggle = new() { Sx = Sx.Of(ToggleStyle, SharedStyles.FocusRing) };
    internal static readonly Attrs Body = new() { Sx = BodyStyle };
    internal static readonly Attrs Description = new() { Sx = DescriptionStyle };
    internal static readonly Attrs Meta = new() { Sx = MetaStyle };
    internal static readonly Attrs LateMeta = new() { Sx = Sx.Of(MetaStyle, LateStyle) };
    internal static readonly Attrs Late = new() { Sx = LateStyle };
    internal static readonly Attrs Action = new() { Sx = ActionStyle };
    internal static readonly Attrs Inline = new() { Sx = InlineStyle };
    internal static readonly Attrs InlineItem = new() { Sx = InlineItemStyle };
    internal static readonly Attrs Icons = new() { Sx = IconsStyle };

    /// <summary>名称：完成的是正文色，没完成的是次要色，不适用的划掉。</summary>
    internal static Attrs Label(ChecklistStatus status) => status switch
    {
        ChecklistStatus.Done => LabelDone,
        ChecklistStatus.Skipped => LabelSkipped,
        _ => LabelPending,
    };

    /// <summary>状态图标的颜色。</summary>
    internal static Attrs StatusIcon(ChecklistStatus status) => StatusIcons[(int)status];

    static readonly Attrs LabelDone = new() { Sx = LabelStyle };
    static readonly Attrs LabelPending = new() { Sx = Sx.Of(LabelStyle, PendingStyle) };
    static readonly Attrs LabelSkipped = new() { Sx = Sx.Of(LabelStyle, SkippedStyle) };
    static readonly Attrs[] StatusIcons = [.. Enum.GetValues<ChecklistStatus>().Select(status => new Attrs { Sx = StatusColor(status) })];

    static Style RootStyle => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Sm });

    static Style SummaryStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "auto 1fr",
        AlignItems = AlignItems.Center,
        Gap = Space.Md,
        FontSize = FontSizes.Sm,
        Color = Colors.TextMuted,
    });

    static Style ListStyle => Css.Define(static () => new Style { Display = Display.Grid, Margin = 0, Padding = 0, ListStyleType = "none" });

    static Style ItemStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "auto minmax(0, 1fr) auto",
        ColumnGap = Space.Sm,
        AlignItems = AlignItems.Start,
        PaddingBlock = Space.Xs,
    });

    static Style StatusStyle => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, MinHeight = "1lh", FontSize = CssLength.Em(1.125) });

    // 可以勾选时的按钮：只有图标，去掉按钮的外观
    static Style ToggleStyle => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        Padding = 0,
        Margin = 0,
        BorderWidth = 0,
        // 悬停同幽灵按钮：圆圈后面铺一层底色
        BackgroundColor = CssColors.Transparent,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        Color = CssWide.Inherit,
        Font = CssWide.Inherit,
        BorderRadius = Radii.Full,
        Cursor = Cursor.Pointer,
        [When.Disabled] = new() { Cursor = Cursor.Default },
    });

    static Style BodyStyle => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Xxs, MinWidth = 0 });

    static Style LabelStyle => Css.Define(static () => new Style { Color = Colors.Text, OverflowWrap = OverflowWrap.Anywhere });

    static Style PendingStyle => Css.Define(static () => new Style { Color = Colors.TextMuted });

    static Style SkippedStyle => Css.Define(static () => new Style { Color = Colors.TextMuted, TextDecorationLine = TextDecorationLine.LineThrough });

    static Style DescriptionStyle => Css.Define(static () => new Style { FontSize = FontSizes.Sm, Color = Colors.TextMuted });

    static Style MetaStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        ColumnGap = Space.Sm,
        RowGap = Space.Xxs,
        FontSize = FontSizes.Sm,
        Color = Colors.TextMuted,
    });

    static Style LateStyle => Css.Define(static () => new Style { Color = Tones.Danger.Text });

    static Style ActionStyle => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Xs });

    static Style InlineStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        ColumnGap = Space.Lg,
        RowGap = Space.Xs,
        Margin = 0,
        Padding = 0,
        ListStyleType = "none",
    });

    static Style InlineItemStyle => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = Space.Xs, WhiteSpace = WhiteSpace.Nowrap });

    static Style IconsStyle => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = Space.Xxs });

    static Style StatusColor(ChecklistStatus status) => Css.Define(status, static status => new Style
    {
        Color = status switch
        {
            ChecklistStatus.Done => Tones.Success.Text,
            ChecklistStatus.Overdue => Tones.Danger.Text,
            ChecklistStatus.Blocked => Tones.Warning.Text,
            _ => Colors.TextMuted,
        },
    });
}
