using Rune.Styles;

namespace Rune.UI;

/// <summary>时间线的部件（同 vue-tsx-pro 的 timelineRecipe）。</summary>
public enum TimelinePart
{
    /// <summary>ol。</summary>
    Root,

    /// <summary>每一项（li）。</summary>
    Item,

    /// <summary>节点那一列（节点与连线）。</summary>
    Rail,

    /// <summary>节点：圆点或图标（按各项的色调着色）。</summary>
    Marker,

    /// <summary>连到下一项的线。</summary>
    Line,

    /// <summary>内容那一列。</summary>
    Content,

    /// <summary>标题。</summary>
    Title,

    /// <summary>标题下面的一行：操作人与时间。</summary>
    Meta,

    /// <summary>操作人（头像与名字）。</summary>
    Actor,

    /// <summary>时间（time）。</summary>
    Time,

    /// <summary>说明。</summary>
    Description,

    /// <summary>收起的几项所在位置的虚线。</summary>
    ToggleLine,

    /// <summary>展开、收起按钮的那一格。</summary>
    ToggleContent,
}

/// <summary>
/// 时间线的样式（配方，同 vue-tsx-pro 的 timelineRecipe）：字号与交替排布是档位。每项是独立的网格：节点一列、内容一列，
/// 交替排布时三列（左内容、节点、右内容）；节点列至少有图标节点那么宽，圆点、图标、加载指示混排时各项才对得齐。
/// 交替排布按容器宽度切换（窄于 32em 排回一侧）。各项的色调、内容在哪一侧、时间放在哪里随项变化，排在部件的默认层后面。
/// </summary>
public static class TimelineStyles
{
    static readonly Condition Narrow = Containers.MaxWidth(CssLength.Em(32));

    internal static readonly Attrs Pending = new() { Sx = Sx.Of(PendingStyle, ToneStyle(Tone.Accent)) };
    internal static readonly Attrs Avatar = new() { Sx = AvatarStyle };
    internal static readonly Attrs ContentEnd = new() { Sx = ContentEndStyle };
    internal static readonly Attrs TimeOpposite = new() { Sx = TimeOppositeStyle };
    internal static readonly Attrs TimeInline = new() { Sx = TimeInlineStyle };
    internal static readonly Attrs OppositeGap = new() { Sx = TimeOppositeStyle };
    internal static readonly Attrs DetailsToggle = new() { Sx = DetailsToggleStyle };
    internal static readonly Attrs DetailsContent = new() { Sx = DetailsContentStyle };

    /// <summary>某个字号、是否交替排布时各部件的默认层。</summary>
    public static PartLayers<TimelinePart> Of(Size? size, bool alternate, ComponentStyles styles) =>
        Recipe.Of("timeline", (size, alternate), styles, static ((Size? Size, bool Alternate) a, TimelinePart part) => part switch
        {
            TimelinePart.Root => Sx.Of(RootStyle, a.Size is { } s ? ScaleStyles.FontSize(s) : null),
            TimelinePart.Item => Sx.Of(ItemStyle, a.Alternate ? ItemAlternateStyle : null),
            TimelinePart.Rail => RailStyle,
            TimelinePart.Marker => MarkerStyle,
            TimelinePart.Line => LineStyle,
            TimelinePart.Content => ContentStyle,
            TimelinePart.Title => TitleStyle,
            TimelinePart.Meta => MetaStyle,
            TimelinePart.Actor => ActorStyle,
            TimelinePart.Time => TimeStyle,
            TimelinePart.Description => DescriptionStyle,
            TimelinePart.ToggleLine => ToggleLineStyle,
            _ => ToggleContentStyle,
        });

    /// <summary>节点随项变化的那一层：色调，有图标时大一些、描边。</summary>
    internal static Attrs MarkerLook(Tone tone, bool icon) => (icon ? IconMarkers : DotMarkers)[(int)tone];

    static readonly Attrs[] DotMarkers = [.. Enum.GetValues<Tone>().Select(tone => new Attrs { Sx = ToneStyle(tone) })];
    static readonly Attrs[] IconMarkers = [.. Enum.GetValues<Tone>().Select(tone => new Attrs { Sx = Sx.Of(MarkerIconStyle, ToneStyle(tone)) })];

    static Style ToneStyle(Tone tone) => Css.Define(tone, static tone => new Style
    {
        Color = tone switch
        {
            Tone.Neutral => Colors.BorderStrong,
            Tone.Accent => Tones.Accent.Solid,
            _ => Tones.Of(tone).Text,
        },
    });

    // 竖排、去掉编号与缩进；交替排布按容器宽度切换
    static Style RootStyle => Css.Define(static () => new Style
    {
        ContainerType = ContainerType.InlineSize,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Margin = 0,
        Padding = 0,
        ListStyleType = "none",
    });

    static Style ItemStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "minmax(1.5em, auto) 1fr",
        ColumnGap = Space.Md,
    });

    static Style ItemAlternateStyle => Css.Define(static () => new Style
    {
        GridTemplateColumns = "1fr minmax(1.5em, auto) 1fr",
        [Narrow] = new() { GridTemplateColumns = "minmax(1.5em, auto) 1fr" },
    });

    static Style RailStyle => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center });

    static Style MarkerStyle => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        FlexShrink = 0,
        Width = CssLength.Em(0.75),
        Height = CssLength.Em(0.75),
        MarginBlock = CssLength.Em(0.375),
        BorderRadius = Radii.Full,
        BackgroundColor = CssColors.CurrentColor,
    });

    // 有图标时节点大一些，图标描边用色调
    static Style MarkerIconStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        Width = CssLength.Em(1.5),
        Height = CssLength.Em(1.5),
        MarginBlock = 0,
        BackgroundColor = Colors.Surface,
        BorderWidth = BorderWidths.Sm,
        BorderStyle = BorderStyle.Solid,
        BorderColor = CssColors.CurrentColor,
    });

    static Style PendingStyle => Css.Define(static () => new Style { MarginBlock = CssLength.Em(0.25) });

    // 连到下一项的线；最后一项没有
    static Style LineStyle => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        Width = BorderWidths.Sm,
        MinHeight = CssLength.Em(1),
        BackgroundColor = Colors.BorderMuted,
    });

    static Style ContentStyle => Css.Define(static () => new Style { MinWidth = 0, PaddingBlockEnd = Space.Lg, Display = Display.Grid, Gap = Space.Xs });

    // 交替排布时偶数项的内容在节点左侧、靠右对齐
    static Style ContentEndStyle => Css.Define(static () => new Style
    {
        GridColumn = "1",
        GridRow = "1",
        TextAlign = TextAlign.End,
        [Narrow] = new() { GridColumn = "2", TextAlign = TextAlign.Start },
    });

    static Style TitleStyle => Css.Define(static () => new Style { FontWeight = FontWeights.Medium, Color = Colors.Text });

    // 操作人与时间：一行，放不下时换行
    static Style MetaStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        ColumnGap = Space.Sm,
        RowGap = Space.Xs,
        FontSize = FontSizes.Sm,
        Color = Colors.TextMuted,
    });

    static Style ActorStyle => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = Space.Xs, Color = Colors.Text });

    static Style AvatarStyle => Css.Define(static () => new Style { Width = CssLength.Em(1.75), Height = CssLength.Em(1.75) });

    static Style TimeStyle => Css.Define(static () => new Style { FontSize = FontSizes.Sm, Color = Colors.TextMuted });

    // 交替排布时时间放在对侧（它自己占一格）；窄的容器里不占格，改在内容里显示
    static Style TimeOppositeStyle => Css.Define(static () => new Style
    {
        PaddingBlockStart = CssLength.Em(0.125),
        Display = Display.Block,
        [Narrow] = new() { Display = Display.None },
    });

    static Style TimeInlineStyle => Css.Define(static () => new Style
    {
        Display = Display.None,
        [Narrow] = new() { Display = Display.Block },
    });

    static Style DescriptionStyle => Css.Define(static () => new Style { Color = Colors.Text });

    // 详情按钮像链接，靠起始边；展开的内容与上面隔开一点
    static Style DetailsToggleStyle => Css.Define(static () => new Style { JustifySelf = JustifySelf.Start });

    static Style DetailsContentStyle => Css.Define(static () => new Style { PaddingBlockStart = Space.Xs });

    // 收起的那几项的位置：节点列是一段虚线，内容是按钮
    static Style ToggleLineStyle => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinHeight = CssLength.Em(2),
        BorderInlineStartWidth = BorderWidths.Sm,
        BorderInlineStartStyle = BorderStyle.Dashed,
        BorderInlineStartColor = Colors.BorderMuted,
    });

    static Style ToggleContentStyle => Css.Define(static () => new Style { PaddingBlockEnd = Space.Lg });
}
