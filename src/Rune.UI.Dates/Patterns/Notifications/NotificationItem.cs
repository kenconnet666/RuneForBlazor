using Rune.Styles;

namespace Rune.UI;

/// <summary>一条通知（同 vue-tsx-pro 的 NotificationItem）。</summary>
/// <param name="Id">标识。</param>
/// <param name="Title">标题：一句话说清发生了什么。</param>
/// <param name="Time">发生的时间。</param>
public sealed record NotificationItem(string Id, string Title, DateTimeOffset Time)
{
    /// <summary>补充说明（谁、哪张单）。</summary>
    public string? Description { get; init; }

    /// <summary>类型：按 <see cref="NotificationList.Types"/> 筛选。</summary>
    public string? Type { get; init; }

    /// <summary>读过了。</summary>
    public bool Read { get; init; }

    /// <summary>点了去哪儿。</summary>
    public string? Href { get; init; }

    /// <summary>未读色点的色调，默认 Accent；超时、被退回用 Danger、Warning。</summary>
    public Tone? Tone { get; init; }
}

/// <summary>通知的一种类型（筛选按钮）。</summary>
/// <param name="Key">类型：对应 <see cref="NotificationItem.Type"/>。</param>
/// <param name="Label">名称。</param>
public sealed record NotificationType(string Key, string Label);

/// <summary>通知列表的部件。</summary>
public enum NotificationListPart
{
    /// <summary>外层 div。</summary>
    Root,

    /// <summary>标题行。</summary>
    Head,

    /// <summary>标题。</summary>
    Title,

    /// <summary>“全部标为已读”按钮。</summary>
    MarkAll,

    /// <summary>筛选行。</summary>
    Filters,

    /// <summary>类型筛选按钮组（ToggleGroup）。</summary>
    Types,

    /// <summary>“只看未读”开关（Switch）。</summary>
    UnreadOnly,

    /// <summary>列表外面的滚动区域（ScrollArea）：最大高度给它。</summary>
    Scroll,

    /// <summary>列表（ul）。</summary>
    List,

    /// <summary>一条（有地址时是 a，没有时是 button）。</summary>
    Item,

    /// <summary>未读的色点。</summary>
    Dot,

    /// <summary>标题与说明。</summary>
    Body,

    /// <summary>一条的标题。</summary>
    ItemTitle,

    /// <summary>一条的说明。</summary>
    Description,

    /// <summary>时间（RelativeTime）。</summary>
    Time,

    /// <summary>列表为空时那一块。</summary>
    Empty,

    /// <summary>列表下面那一行。</summary>
    Footer,
}

/// <summary>通知列表的样式（同 vue-tsx-pro 的 notificationListRecipe）。</summary>
public static class NotificationListStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<NotificationListPart> Of(ComponentStyles styles) =>
        Recipe.Of("notification-list", styles, static (NotificationListPart part) => part switch
        {
            NotificationListPart.Root => Root,
            NotificationListPart.Head => Head,
            NotificationListPart.Title => Title,
            NotificationListPart.Filters => Filters,
            NotificationListPart.Types => Types,
            NotificationListPart.UnreadOnly => UnreadOnly,
            NotificationListPart.List => List,
            NotificationListPart.Item => Sx.Of(SharedStyles.FocusRingInset, Item),
            NotificationListPart.Dot => Dot,
            NotificationListPart.Body => Body,
            NotificationListPart.ItemTitle => ItemTitle,
            NotificationListPart.Description => Description,
            NotificationListPart.Time => Time,
            NotificationListPart.Footer => Footer,
            NotificationListPart.Empty => Empty,
            _ => Sx.Of((Style?)null),
        });

    /// <summary>未读的标题加粗：按条叠在部件层之后。</summary>
    public static Style Unread => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold });

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, MinWidth = 0 });

    static Style Head => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Sm,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
        BorderBlockEndWidth = BorderWidths.Xs,
        BorderBlockEndStyle = BorderStyle.Solid,
        BorderBlockEndColor = Colors.Border,
    });

    static Style Title => Css.Define(static () => new Style { FontWeight = FontWeights.Medium });

    // 类型多时筛选按钮换行；“只看未读”靠结束边（没有标题行时“全部标为已读”跟在它后面）
    static Style Filters => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
    });

    static Style Types => Css.Define(static () => new Style { FlexWrap = FlexWrap.Wrap });

    static Style UnreadOnly => Css.Define(static () => new Style { MarginInlineStart = CssLength.Auto });

    static Style List => Css.Define(static () => new Style { ListStyle = ListStyle.None, Margin = 0, Padding = 0 });

    // 整行贴着滚动区域的边：焦点环画在里面，画在外面会被裁掉
    static Style Item => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "0.5rem minmax(0, 1fr) auto",
        ColumnGap = Space.Sm,
        AlignItems = AlignItems.Baseline,
        BoxSizing = BoxSizing.BorderBox,
        Width = CssLength.Percent(100),
        Margin = 0,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
        Color = CssWide.Inherit,
        FontFamily = CssWide.Inherit,
        FontSize = CssWide.Inherit,
        LineHeight = CssWide.Inherit,
        TextAlign = TextAlign.Start,
        TextDecorationLine = TextDecorationLine.None,
        BorderWidth = 0,
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderBlockStartColor = Colors.BorderMuted,
        BackgroundColor = CssColors.Transparent,
        Cursor = Cursor.Pointer,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
    });

    // 未读的色点贴着标题的第一行
    static Style Dot => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Height = "1lh", AlignSelf = AlignSelf.Start });

    static Style Body => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Xxs, MinWidth = 0 });

    static Style ItemTitle => Css.Define(static () => new Style { FontSize = FontSizes.Sm });

    static Style Description => Css.Define(static () => new Style { Color = Colors.TextMuted, FontSize = FontSizes.Xs });

    static Style Time => Css.Define(static () => new Style { Color = Colors.TextMuted, FontSize = FontSizes.Xs, WhiteSpace = WhiteSpace.Nowrap });

    static Style Footer => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        JustifyContent = JustifyContent.Center,
        PaddingBlock = Space.Sm,
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderBlockStartColor = Colors.Border,
    });

    static Style Empty => Css.Define(static () => new Style { PaddingBlock = Space.Lg });
}

/// <summary>通知中心的部件。</summary>
public enum NotificationCenterPart
{
    /// <summary>弹出层。</summary>
    Panel,

    /// <summary>未读数（Indicator）。</summary>
    Indicator,

    /// <summary>铃铛按钮（IconButton）。</summary>
    Trigger,

    /// <summary>面板里通知列表的滚动区域：最大高度。</summary>
    List,
}

/// <summary>通知中心的样式（同 vue-tsx-pro 的 notificationCenterRecipe）。</summary>
public static class NotificationCenterStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<NotificationCenterPart> Of(ComponentStyles styles) =>
        Recipe.Of("notification-center", styles, static (NotificationCenterPart part) => part switch
        {
            NotificationCenterPart.Panel => Panel,
            NotificationCenterPart.List => List,
            _ => Sx.Of((Style?)null),
        });

    static Style Panel => Css.Define(static () => new Style { Width = "min(26rem, calc(100vw - 2rem))", Padding = 0 });

    static Style List => Css.Define(static () => new Style { MaxHeight = CssLength.Rem(24) });
}
