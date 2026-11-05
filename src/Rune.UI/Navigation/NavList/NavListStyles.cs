using Rune.Styles;

namespace Rune.UI;

/// <summary>侧边导航的部件。</summary>
public enum NavListPart
{
    /// <summary>导航（nav）。</summary>
    Root,

    /// <summary>列表（ul，FlexList 组件）：NavList、NavGroup、NavSection 里的列表。</summary>
    List,

    /// <summary>一项（li）。</summary>
    Item,

    /// <summary>链接（a）与分组的标题按钮共用的行。</summary>
    Row,

    /// <summary>行里的图标（Icon 组件）。</summary>
    Icon,

    /// <summary>行里的文字（span）。</summary>
    Label,

    /// <summary>分组标题按钮后的箭头（Icon 组件）。</summary>
    Chevron,

    /// <summary>小节（li，NavSection）。</summary>
    Section,

    /// <summary>小节标题（span）。</summary>
    SectionLabel,

    /// <summary>小节标题的图标（Icon 组件）。</summary>
    SectionIcon,

    /// <summary>小节标题的文字（span）。</summary>
    SectionText,

    /// <summary>紧凑分组弹出的卡片（HoverCard 组件）。</summary>
    Flyout,
}

/// <summary>NavList 交给里面的项的信息（级联值）：是否紧凑。</summary>
/// <param name="Compact">只显示图标。</param>
public sealed record NavListContext(bool Compact)
{
    /// <summary>展开（完整文字）。</summary>
    public static readonly NavListContext Expanded = new(false);

    /// <summary>紧凑（只显示图标）。</summary>
    public static readonly NavListContext IconOnly = new(true);

    /// <summary>按是否紧凑取共用的实例。</summary>
    public static NavListContext Of(bool compact) => compact ? IconOnly : Expanded;
}

/// <summary>
/// NavGroup 交给组内各项的状态（级联值）：各项报告自己是不是当前页，组里有当前页时分组自动展开（紧凑时图标高亮）。
/// </summary>
/// <param name="changed">组里“有没有当前页”变了时调用。</param>
public sealed class NavGroupState(Action<bool> changed)
{
    readonly HashSet<object> current = [];

    /// <summary>组里有没有当前页。</summary>
    public bool HasCurrent => current.Count > 0;

    /// <summary>组内的一项报告自己是不是当前页（<paramref name="item"/> 是这一项本身）。</summary>
    public void Report(object item, bool isCurrent)
    {
        var had = HasCurrent;
        if (isCurrent) current.Add(item);
        else current.Remove(item);
        if (had != HasCurrent) changed(HasCurrent);
    }
}

/// <summary>侧边导航的样式（同 vue-tsx-pro 的 navListStyles）。</summary>
public static class NavListStyles
{
    /// <summary>行的状态（配方的键）：当前页、不可用、只剩图标。</summary>
    /// <param name="Current">当前页（分组：组里有当前页）。</param>
    /// <param name="Disabled">不可用。</param>
    /// <param name="IconOnly">紧凑：只剩图标。</param>
    public readonly record struct RowState(bool Current, bool Disabled, bool IconOnly);

    /// <summary>某种行状态、某套定制下各部件的默认层。</summary>
    public static PartLayers<NavListPart> Of(RowState state, ComponentStyles styles) =>
        Recipe.Of("nav-list", state, styles, static (RowState s, NavListPart part) => part switch
        {
            NavListPart.Row => Sx.Of(
                ItemStyles.Row,
                ItemStyles.Hoverable,
                Row,
                SharedStyles.FocusRing,
                SharedStyles.RippleHost,
                s.IconOnly ? CompactRow : null,
                s.Current ? ItemStyles.Current : null,
                s.Current ? Current : null,
                s.Disabled ? Disabled : null),
            NavListPart.Label => Sx.Of(ItemStyles.Label, s.IconOnly ? SharedStyles.VisuallyHidden : null),
            NavListPart.Chevron => Chevron,
            NavListPart.Section => Section,
            NavListPart.SectionLabel => Sx.Of(SectionLabel, s.IconOnly ? CompactSection : null),
            NavListPart.SectionIcon => SectionIcon,
            NavListPart.SectionText => Sx.Of(SectionText, s.IconOnly ? SharedStyles.VisuallyHidden : null),
            NavListPart.Flyout => Flyout,
            _ => Sx.Of(),
        });

    /// <summary>分组、紧凑分组里的列表：缩进；紧凑时不缩进。</summary>
    public static Style Nested(bool compact) => compact ? CompactNested : NestedList;

    /// <summary>分组展开时的箭头（转到朝下）。</summary>
    public static Style ChevronOpen => Css.Define(static () => new Style { Transform = "rotate(90deg)" });

    static Style NestedList => Css.Define(static () => new Style { PaddingInlineStart = Space.Lg, PaddingTop = Space.Xs });

    static Style CompactNested => Css.Define(static () => new Style { PaddingInlineStart = 0, PaddingTop = Space.Xs });

    // 链接与分组的触发按钮共用：列表行之上，导航项更宽松、字重更重，平时是次要颜色
    static Style Row => Css.Define(static () => new Style
    {
        PaddingInline = Space.Md,
        BorderRadius = Radii.Md,
        Color = Colors.TextMuted,
        FontWeight = FontWeights.Medium,
        [When.Hover] = new() { Color = Colors.Text },
    });

    // 当前页的颜色来自 ItemStyles.Current；悬停时也保持品牌色
    static Style Current => Css.Define(static () => new Style
    {
        Color = Tones.Accent.Text,
        [When.Hover] = new() { Color = Tones.Accent.Text },
    });

    static Style Disabled => Css.Define(static () => new Style
    {
        Opacity = Opacities.Disabled,
        Cursor = Cursor.NotAllowed,
        BackgroundColor = CssColors.Transparent,
        PointerEvents = PointerEvents.None,
    });

    static Style Chevron => Css.Define(static () => new Style { Transition = $"transform {Durations.Md} {Easings.Standard}" });

    // 小节之间留出间隔，第一节贴着顶部
    static Style Section => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Xs,
        MarginBlockStart = Space.Lg,
        [When.FirstChild] = new() { MarginBlockStart = 0 },
    });

    // 标题行的内边距、图标与间距和链接行同一套，图标与文字才和下面的项上下对齐；只有文字小一档、加粗
    static Style SectionLabel => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Md,
        Color = Colors.Text,
    });

    static Style SectionText => Css.Define(static () => new Style
    {
        FontSize = FontSizes.Sm,
        FontWeight = FontWeights.Semibold,
        LetterSpacing = LetterSpacings.Wide,
    });

    // 小节标题的图标只是标记，比链接的图标淡
    static Style SectionIcon => Css.Define(static () => new Style { Color = Colors.TextMuted, FlexShrink = 0 });

    // 紧凑：图标居中；小节标题只剩图标，上面一条细线与上一节隔开
    static Style CompactRow => Css.Define(static () => new Style { JustifyContent = JustifyContent.Center, PaddingInline = 0 });

    static Style CompactSection => Css.Define(static () => new Style
    {
        JustifyContent = JustifyContent.Center,
        PaddingInline = 0,
        PaddingBlockStart = Space.Sm,
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderBlockStartColor = Colors.BorderMuted,
    });

    // 紧凑分组弹出的卡片：比侧栏宽，放得下完整的文字
    static Style Flyout => Css.Define(static () => new Style { Padding = Space.Xs, MinWidth = CssLength.Em(12) });
}
