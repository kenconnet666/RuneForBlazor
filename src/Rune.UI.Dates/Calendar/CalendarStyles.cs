using Rune.Styles;

namespace Rune.UI;

/// <summary>日历的部件（同 vue-tsx-pro 的 CalendarView）。</summary>
public enum CalendarPart
{
    /// <summary>根元素（div role="group"）。</summary>
    Root,

    /// <summary>页头：上一页、标题、下一页。</summary>
    Header,

    /// <summary>标题按钮（切到月份、年份视图）。</summary>
    Heading,

    /// <summary>并排的月份那一层。</summary>
    Months,

    /// <summary>每张网格（table role="grid"）。</summary>
    Grid,

    /// <summary>星期表头。</summary>
    Weekday,

    /// <summary>周数。</summary>
    WeekNumber,

    /// <summary>每格（td role="gridcell"）：区间的底色画在这里。</summary>
    Cell,

    /// <summary>每格里的按钮。</summary>
    CellButton,

    /// <summary>页脚。</summary>
    Footer,
}

/// <summary>
/// 日历的样式（同 vue-tsx-pro 的 calendarStyles）。格子的状态都写在 data-* 上（今天、选中、区间、补齐的上下月、不可选），
/// 样式跟着属性走：区间选到一半时的悬停预览由 JS 写 data-preview*，与 C# 写的 data-in-range 同一套外观。
/// </summary>
public static class CalendarStyles
{
    static readonly CssLength Target = $"max({Sizes.Control}, {Sizes.MinTarget})";
    static readonly CssLength CellSize = $"max(2.25em, {Sizes.MinTarget})";

    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<CalendarPart> Of(ComponentStyles styles) =>
        Recipe.Of("calendar", styles, static (CalendarPart part) => part switch
        {
            CalendarPart.Root => Root,
            CalendarPart.Header => Header,
            CalendarPart.Heading => Sx.Of(SharedStyles.ButtonReset, SharedStyles.InteractionColors, Heading, SharedStyles.FocusRing),
            CalendarPart.Months => Months,
            CalendarPart.Grid => Grid,
            CalendarPart.Weekday => Weekday,
            CalendarPart.WeekNumber => WeekNumber,
            CalendarPart.Cell => Cell,
            CalendarPart.CellButton => Sx.Of(SharedStyles.ButtonReset, SharedStyles.InteractionColors, CellButton, SharedStyles.FocusRing),
            _ => Footer,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Sm,
        Color = Colors.Text,
        LineHeight = LineHeights.Normal,
        UserSelect = UserSelect.None,
    });

    static Style Months => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Xl,
    });

    /// <summary>一个月：页头加网格。</summary>
    internal static Style Month => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Xs });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = $"{Target} 1fr {Target}",
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
    });

    /// <summary>占住翻页按钮的位置：并排几个月时中间的月没有翻页按钮，标题仍居中。</summary>
    internal static Style Spacer => Css.Define(static () => new Style { Width = Target });

    static Style Heading => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifySelf = JustifySelf.Center,
        MinHeight = Target,
        PaddingInline = Space.Sm,
        BorderRadius = Radii.Md,
        FontWeight = FontWeights.Medium,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        // 已经是最粗的视图（或不可用）：标题只是文字
        [When.Attribute("[data-static]")] = new() { Cursor = Cursor.Default, [When.Hover] = new() { BackgroundColor = CssColors.Transparent } },
    });

    static Style Grid => Css.Define(static () => new Style
    {
        BorderCollapse = BorderCollapse.Collapse,
        BorderSpacing = 0,
        // 月份、年份视图：一行 3 格，与日期网格同宽
        [When.Attribute("[data-view=\"month\"], [data-view=\"year\"]")] = new() { Width = $"calc(7 * {CellSize})" },
    });

    static Style Weekday => Css.Define(static () => new Style
    {
        PaddingBlock = Space.Xs,
        PaddingInline = 0,
        Color = Colors.TextMuted,
        FontWeight = FontWeights.Normal,
        TextAlign = TextAlign.Center,
    });

    static Style WeekNumber => Css.Define(static () => new Style
    {
        PaddingInlineEnd = Space.Xs,
        Color = Colors.TextMuted,
        FontWeight = FontWeights.Normal,
        TextAlign = TextAlign.Center,
    });

    // 区间的底色画在 td 上，相邻的格连成一条；两端与每行的首尾是圆的
    static Style Cell => Css.Define(static () => new Style
    {
        Padding = 0,
        TextAlign = TextAlign.Center,
        [When.Attribute("[data-in-range], [data-preview]")] = new()
        {
            BackgroundColor = Tones.Accent.Soft,
            // 高对比下背景色被系统换掉：区间用系统的高亮色（格子里的字跟着换，见 CellButton）
            [Media.ForcedColors] = new() { BackgroundColor = CssColors.Highlight, ForcedColorAdjust = ForcedColorAdjust.None },
            [new Condition(":first-of-type")] = new() { BorderStartStartRadius = Radii.Md, BorderEndStartRadius = Radii.Md },
            [new Condition(":last-of-type")] = new() { BorderStartEndRadius = Radii.Md, BorderEndEndRadius = Radii.Md },
        },
        [When.Attribute("[data-range-start], [data-preview-start]")] = new() { BorderStartStartRadius = Radii.Md, BorderEndStartRadius = Radii.Md },
        [When.Attribute("[data-range-end], [data-preview-end]")] = new() { BorderStartEndRadius = Radii.Md, BorderEndEndRadius = Radii.Md },
    });

    static Style CellButton => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Width = CellSize,
        Height = CellSize,
        BorderRadius = Radii.Md,
        TextAlign = TextAlign.Center,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        [When.Attribute("[data-period]")] = new() { Width = CssLength.Percent(100), Height = $"max(3em, {Sizes.MinTarget})" },
        [When.Attribute("[data-outside]")] = new() { Color = Colors.TextMuted },
        // 高对比下阴影画的那一圈没有了：加下划线
        [When.Attribute("[data-today]")] = new()
        {
            FontWeight = FontWeights.Semibold,
            BoxShadow = $"inset 0 0 0 {BorderWidths.Xs} {Tones.Accent.Border}",
            [Media.ForcedColors] = new() { TextDecorationLine = TextDecorationLine.Underline },
        },
        [When.Attribute("[data-unavailable]")] = new() { Color = Colors.TextMuted, TextDecorationLine = TextDecorationLine.LineThrough },
        // 区间中间的格：按钮透明，底色是 td 的那一条
        [When.Attribute("[data-in-range], [data-preview]")] = new()
        {
            BackgroundColor = CssColors.Transparent,
            Color = Tones.Accent.Text,
            [When.Hover] = new() { BackgroundColor = Tones.Accent.SoftHover },
            [Media.ForcedColors] = new() { Color = CssColors.HighlightText },
        },
        [When.Attribute("[data-selected], [data-anchor], [data-preview-start], [data-preview-end]")] = new()
        {
            BackgroundColor = Tones.Accent.Solid,
            Color = Tones.Accent.Contrast,
            BoxShadow = BoxShadow.None,
            [When.Hover] = new() { BackgroundColor = Tones.Accent.SolidHover, [Media.ForcedColors] = new() { BackgroundColor = CssColors.Highlight } },
            // 高对比下背景色被系统换掉、和没选的一样：选中的用系统的高亮色
            [Media.ForcedColors] = new() { BackgroundColor = CssColors.Highlight, Color = CssColors.HighlightText, ForcedColorAdjust = ForcedColorAdjust.None },
        },
        [When.Attribute("[aria-disabled=\"true\"]")] = new()
        {
            Opacity = Opacities.Disabled,
            Cursor = Cursor.NotAllowed,
            [When.Hover] = new() { BackgroundColor = CssColors.Transparent },
        },
    });

    static Style Footer => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
    });
}
