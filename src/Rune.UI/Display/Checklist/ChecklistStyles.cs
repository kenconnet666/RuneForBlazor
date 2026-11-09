using Rune.Styles;

namespace Rune.UI;

/// <summary>完成清单的部件（同 vue-tsx-pro 的 checklistPart）。</summary>
public enum ChecklistPart
{
    /// <summary>外层：一项一行时是 div，一行排开时是 ul，只有图标时是 span（role="img"）。</summary>
    Root,

    /// <summary>上面的“已完成 2 / 3”与进度条。</summary>
    Summary,

    /// <summary>上面的进度条（Progress）。</summary>
    Progress,

    /// <summary>一项一行时的列表（ul）。</summary>
    List,

    /// <summary>一项（li）：状态一列、内容一列、操作一列；一行排开时是一个行内的小组。</summary>
    Item,

    /// <summary>状态（图标与给读屏的状态文字）。</summary>
    Status,

    /// <summary>可以勾选时的复选按钮（只有图标）。</summary>
    Toggle,

    /// <summary>内容一列：名称、说明、负责人与日期。</summary>
    Body,

    /// <summary>名称：完成的是正文色，没完成的是次要色，不适用的划掉。</summary>
    Label,

    /// <summary>说明。</summary>
    Description,

    /// <summary>负责人、日期、证据一行（超时的日期用危险色）。</summary>
    Meta,

    /// <summary>操作一列。</summary>
    Action,
}

/// <summary>
/// 完成清单的样式（配方，同 vue-tsx-pro 的 checklistStyles）：排法是档位；状态一列、内容一列、操作一列，状态贴着名称那一行。
/// 名称按状态的颜色、超时的日期、状态图标的颜色随项变化，叠在部件的默认层后面。
/// </summary>
public static class ChecklistStyles
{
    /// <summary>某种排法、某套定制下各部件的默认层。</summary>
    public static PartLayers<ChecklistPart> Of(ChecklistLayout layout, ComponentStyles styles) =>
        Recipe.Of("checklist", layout, styles, static (ChecklistLayout layout, ChecklistPart part) => part switch
        {
            ChecklistPart.Root => layout switch
            {
                ChecklistLayout.Inline => InlineStyle,
                ChecklistLayout.Icons => IconsStyle,
                _ => RootStyle,
            },
            ChecklistPart.Summary => SummaryStyle,
            ChecklistPart.List => ListStyle,
            ChecklistPart.Item => layout == ChecklistLayout.Inline ? InlineItemStyle : ItemStyle,
            ChecklistPart.Status => StatusStyle,
            ChecklistPart.Toggle => Sx.Of(ToggleStyle, SharedStyles.FocusRing),
            ChecklistPart.Body => BodyStyle,
            ChecklistPart.Label => LabelStyle,
            ChecklistPart.Description => DescriptionStyle,
            ChecklistPart.Meta => MetaStyle,
            ChecklistPart.Action => ActionStyle,
            _ => Sx.Of(),
        });

    /// <summary>名称按状态：没完成的是次要色，不适用的划掉。</summary>
    internal static Attrs? LabelState(ChecklistStatus status) => status switch
    {
        ChecklistStatus.Done => null,
        ChecklistStatus.Skipped => LabelSkipped,
        _ => LabelPending,
    };

    /// <summary>超时的日期。</summary>
    internal static readonly Attrs Late = new() { Sx = LateStyle };

    /// <summary>状态图标的颜色。</summary>
    internal static Attrs StatusIcon(ChecklistStatus status) => StatusIcons[(int)status];

    static readonly Attrs LabelPending = new() { Sx = PendingStyle };
    static readonly Attrs LabelSkipped = new() { Sx = SkippedStyle };
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
