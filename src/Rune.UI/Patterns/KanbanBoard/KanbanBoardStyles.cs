using Rune.Styles;

namespace Rune.UI;

/// <summary>看板的一列（同 vue-tsx-pro 的 KanbanColumn）。</summary>
/// <param name="Key">列的标识：GetColumn 返回它。</param>
/// <param name="Label">列名。</param>
public sealed record KanbanColumn(string Key, string Label)
{
    /// <summary>列名旁计数的色调（超时、异常这类要注意的列）。</summary>
    public Tone? Tone { get; init; }
}

/// <summary>卡片的渲染状态（同 vue-tsx-pro 的 KanbanCardState）。</summary>
/// <param name="Item">卡片的数据。</param>
/// <param name="Dragging">正在被拖动或被键盘拿起。</param>
public readonly record struct KanbanCard<TItem>(TItem Item, bool Dragging);

/// <summary>卡片放到了可以放的列。</summary>
/// <param name="Item">卡片的数据。</param>
/// <param name="To">目标列。</param>
/// <param name="Index">在目标列里的位置（从 0 起）：插到目标列第 Index 张的前面。</param>
public sealed record KanbanMove<TItem>(TItem Item, string To, int Index);

/// <summary>卡片放到了不能放的列（已经弹回）。</summary>
/// <param name="Item">卡片的数据。</param>
/// <param name="To">目标列。</param>
/// <param name="Reason">不能放的原因（CanMove 返回的）。</param>
public sealed record KanbanReject<TItem>(TItem Item, string To, string Reason);

/// <summary>看板的部件。</summary>
public enum KanbanBoardPart
{
    /// <summary>外层 div（role="group"）。</summary>
    Root,

    /// <summary>各列的横排。</summary>
    Board,

    /// <summary>一列（section）。</summary>
    Column,

    /// <summary>列头（名称与数量）。</summary>
    Header,

    /// <summary>列名。</summary>
    Title,

    /// <summary>列头的数量（Badge）。</summary>
    Count,

    /// <summary>拖到不能放的列时列头下面的原因。</summary>
    Reason,

    /// <summary>卡片列表（ul）。</summary>
    List,

    /// <summary>空列的提示。</summary>
    Empty,

    /// <summary>一张卡片（li）。</summary>
    Card,

    /// <summary>拖动把手（ButtonBase）。</summary>
    Handle,

    /// <summary>落点的插入线。</summary>
    Indicator,

    /// <summary>跟着指针的影子卡。</summary>
    Ghost,
}

/// <summary>看板的样式（同 vue-tsx-pro 的 kanbanBoardStyles）。</summary>
public static class KanbanBoardStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<KanbanBoardPart> Of(ComponentStyles styles) =>
        Recipe.Of("kanban-board", styles, static (KanbanBoardPart part) => part switch
        {
            KanbanBoardPart.Board => Board,
            KanbanBoardPart.Column => Column,
            KanbanBoardPart.Header => Header,
            KanbanBoardPart.Reason => Reason,
            KanbanBoardPart.List => List,
            KanbanBoardPart.Empty => Empty,
            KanbanBoardPart.Card => Card,
            KanbanBoardPart.Handle => Handle,
            KanbanBoardPart.Indicator => Indicator,
            KanbanBoardPart.Ghost => Ghost,
            _ => Sx.Of((Style?)null),
        });

    static Style Board => Css.Define(static () => new Style { Display = Display.Flex, Gap = Space.Md, AlignItems = AlignItems.FlexStart, PaddingBlockEnd = Space.Sm });

    // 拖到这一列时的框：能放是强调色，不能放是红色（JS 写 data-drop）
    static Style Column => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        FlexShrink = 0,
        BorderRadius = Radii.Lg,
        BackgroundColor = Colors.SurfaceMuted,
        OutlineWidth = BorderWidths.Sm,
        OutlineStyle = OutlineStyle.Solid,
        OutlineColor = CssColors.Transparent,
        OutlineOffset = $"calc(-1 * {BorderWidths.Sm})",
        Transition = $"outline-color {Durations.Sm} {Easings.Standard}",
        [When.Attribute("[data-drop=allowed]")] = new() { OutlineColor = Tones.Accent.Solid },
        [When.Attribute("[data-drop=denied]")] = new() { OutlineColor = Tones.Danger.Solid },
    });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Sm,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
        FontWeight = FontWeights.Medium,
        FontSize = FontSizes.Sm,
    });

    // 文字由 JS 写进去，有原因时才显示
    static Style Reason => Css.Define(static () => new Style
    {
        Display = Display.None,
        PaddingInline = Space.Md,
        PaddingBlockEnd = Space.Xs,
        Color = Tones.Danger.Text,
        FontSize = FontSizes.Xs,
        [When.Attribute("[data-shown]")] = new() { Display = Display.Block },
    });

    static Style List => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Sm,
        Margin = 0,
        PaddingInline = Space.Sm,
        PaddingBlockEnd = Space.Sm,
        ListStyle = ListStyle.None,
        MinHeight = CssLength.Rem(3),
    });

    static Style Empty => Css.Define(static () => new Style { Color = Colors.TextMuted, FontSize = FontSizes.Xs, PaddingBlock = Space.Sm, TextAlign = TextAlign.Center });

    // 指针拖动中原位的卡片变淡（data-dragging），键盘拿起时高亮边框（data-lifted）
    static Style Card => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Grid,
        GridTemplateColumns = "minmax(0, 1fr) auto",
        AlignItems = AlignItems.Start,
        Gap = Space.Xs,
        Padding = Space.Sm,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.SurfaceRaised,
        Cursor = Cursor.Grab,
        [When.Attribute("[data-dragging]")] = new() { Opacity = 0.4 },
        [When.Attribute("[data-lifted]")] = new() { BorderColor = Tones.Accent.Solid, BoxShadow = Shadows.Md },
    });

    // 拖动把手：悬停同幽灵按钮；触屏从把手拖时按住不滚动（卡片的其余部分照常滚）
    static Style Handle => Css.Define(static () => new Style
    {
        Color = Colors.TextMuted,
        BackgroundColor = CssColors.Transparent,
        BorderRadius = Radii.Sm,
        Padding = Space.Xxs,
        Cursor = Cursor.Grab,
        TouchAction = TouchAction.None,
        [When.Hover] = new() { Color = Colors.Text, BackgroundColor = Colors.FillHover },
    });

    // 落点：两张卡片之间的一条线，JS 按视口坐标放（data-shown、data-denied）
    static Style Indicator => Css.Define(static () => new Style
    {
        Display = Display.None,
        Position = Position.Fixed,
        ZIndex = 50,
        Height = BorderWidths.Md,
        Translate = "0 -50%",
        BorderRadius = Radii.Full,
        BackgroundColor = Tones.Accent.Solid,
        PointerEvents = PointerEvents.None,
        [When.Attribute("[data-shown]")] = new() { Display = Display.Block },
        [When.Attribute("[data-denied]")] = new() { BackgroundColor = Tones.Danger.Solid },
    });

    // 影子卡的外层：JS 把卡片的副本放进来、跟着指针移动（不被列的滚动区域裁掉）
    static Style Ghost => Css.Define(static () => new Style
    {
        Display = Display.None,
        Position = Position.Fixed,
        ZIndex = 50,
        PointerEvents = PointerEvents.None,
        Cursor = Cursor.Grabbing,
        BorderRadius = Radii.Md,
        BoxShadow = Shadows.Lg,
        Rotate = CssValue.Deg(1.5),
        [When.Attribute("[data-shown]")] = new() { Display = Display.Block },
    });
}
