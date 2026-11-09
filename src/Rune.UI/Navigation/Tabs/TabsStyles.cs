using Rune.Styles;

namespace Rune.UI;

/// <summary>标签页的部件。</summary>
public enum TabsPart
{
    /// <summary>外层（div，Tabs）。</summary>
    Root,

    /// <summary>横排时包着标签列表与右边按钮的一层（div，TabList）。</summary>
    Bar,

    /// <summary>标签列表（div，role="tablist"）。</summary>
    List,

    /// <summary>line 样式的下划线指示条（span）。</summary>
    Indicator,

    /// <summary>横排放不下时的滚动按钮、新建按钮（IconButton 组件，保留 IconButton 自己的部件标记，同 vue-tsx-pro）。</summary>
    BarButton,

    /// <summary>标签面板（div，role="tabpanel"）。</summary>
    Panel,
}

/// <summary>一个标签的部件（同 vue-tsx-pro 单独的 Tab 配方：部件标记是 tab/root、tab/closeWrapper、tab/close）。</summary>
public enum TabPart
{
    /// <summary>标签（button，role="tab"）。</summary>
    Root,

    /// <summary>可关闭的标签：包着标签与关闭按钮的一层（span）。</summary>
    CloseWrapper,

    /// <summary>关闭按钮（IconButton 组件）。</summary>
    Close,
}

/// <summary>标签的样式。</summary>
public enum TabsVariant
{
    /// <summary>下划线（默认）：当前标签下方一条品牌色的线。</summary>
    Line,

    /// <summary>浅色底：当前标签有浅色底，像分段控件。</summary>
    Soft,
}

/// <summary>焦点移动时是否同时切换。</summary>
public enum TabsActivation
{
    /// <summary>焦点移到哪个标签就显示哪个面板（默认）：面板内容轻量时用。</summary>
    Automatic,

    /// <summary>方向键只移动焦点，按 Enter / 空格才切换：切换面板代价大（要请求数据）时用。</summary>
    Manual,
}

/// <summary>标签页的默认外观（UIConfig.Defaults 里配置）。</summary>
public sealed record TabsDefaults : IComponentDefaults<TabsDefaults>
{
    /// <summary>样式。</summary>
    public TabsVariant? Variant { get; init; }

    /// <summary>字号档位。</summary>
    public Size? Size { get; init; }

    /// <inheritdoc/>
    public TabsDefaults Over(TabsDefaults outer) => new() { Variant = Variant ?? outer.Variant, Size = Size ?? outer.Size };
}

/// <summary>
/// Tabs 交给 TabList、Tab、TabPanel 的状态（级联值）：每次渲染一个新实例，当前标签变了时它们跟着重新渲染。
/// 登记表（各标签的顺序、有没有面板）是 Tabs 自己的同一个对象。
/// </summary>
public sealed record TabsState(
    string? Value,
    Orientation Orientation,
    TabsActivation Activation,
    TabsVariant Variant,
    bool Lazy,
    bool? Ripple,
    TabsRegistry Registry,
    Func<string, Task> Select)
{
    /// <summary>标签的 id（与面板互相引用）。</summary>
    public string TabId(string value) => $"{Registry.BaseId}-tab-{Safe(value)}";

    /// <summary>面板的 id。</summary>
    public string PanelId(string value) => $"{Registry.BaseId}-panel-{Safe(value)}";

    // 值放进 id（同 vue-tsx-pro）：字母、数字、- 以外的字符（空格、中文、_ 本身）写成 _码位_，不同的值不会撞成同一个 id
    // （只换成 _ 时，“行程”与“费用”都成了 __）
    static string Safe(string value) => string.Concat(value.EnumerateRunes().Select(rune =>
        rune.IsAscii && (char.IsAsciiLetterOrDigit((char)rune.Value) || rune.Value == '-') ? rune.ToString() : $"_{rune.Value:x}_"));
}

/// <summary>Tabs 的登记表：各标签按渲染顺序登记（带文字，“全部标签页”菜单用），各面板登记自己（只当导航用的标签没有面板，不写 aria-controls）。</summary>
/// <param name="baseId">标签、面板 id 的前缀。</param>
/// <param name="changed">登记变了（第一个标签、面板的有无）：Tabs 重新渲染。</param>
public sealed class TabsRegistry(string baseId, Action changed)
{
    readonly List<string> tabs = [];
    readonly Dictionary<string, string> labels = [];
    readonly HashSet<string> panels = [];

    /// <summary>id 的前缀。</summary>
    public string BaseId => baseId;

    /// <summary>登记了的标签，按渲染顺序。</summary>
    public IReadOnlyList<string> Tabs => tabs;

    /// <summary>这个标签有没有面板。</summary>
    public bool HasPanel(string value) => panels.Contains(value);

    /// <summary>标签的文字（没登记过时是值本身）。</summary>
    public string Label(string value) => labels.GetValueOrDefault(value, value);

    /// <summary>登记标签（已经登记过时只更新文字）。</summary>
    public void AddTab(string value, string label)
    {
        labels[value] = label;
        if (tabs.Contains(value)) return;
        tabs.Add(value);
        if (tabs.Count == 1) changed();
    }

    /// <summary>拖动排序后按新的顺序排（“全部标签页”菜单跟着显示的顺序）；不在新顺序里的排在最后。</summary>
    public void Reorder(IReadOnlyList<string> values)
    {
        var ordered = values.Where(tabs.Contains).Concat(tabs.Where(t => !values.Contains(t))).ToList();
        tabs.Clear();
        tabs.AddRange(ordered);
    }

    /// <summary>整组正在卸载（Tabs 先于里面的标签释放）：标签逐个注销时不另选，不然会依次写回旁边的值。</summary>
    public bool Unmounting { get; set; }

    /// <summary>注销标签；返回它原来的位置（关掉当前标签时按它选旁边的）。</summary>
    public int RemoveTab(string value)
    {
        var index = tabs.IndexOf(value);
        if (index >= 0) tabs.RemoveAt(index);
        labels.Remove(value);
        return index;
    }

    /// <summary>登记面板。</summary>
    public void AddPanel(string value)
    {
        if (panels.Add(value)) changed();
    }

    /// <summary>注销面板。</summary>
    public void RemovePanel(string value) => panels.Remove(value);
}

/// <summary>标签页的样式（同 vue-tsx-pro 的 tabsStyles、tabListStyles、tabRecipe、tabPanelStyles）。</summary>
public static class TabsStyles
{
    /// <summary>标签页的外观（配方的键）。</summary>
    /// <param name="Variant">样式。</param>
    /// <param name="Orientation">方向。</param>
    /// <param name="Size">字号档位。</param>
    /// <param name="Closable">标签可以关闭。</param>
    public readonly record struct TabsAppearance(TabsVariant Variant, Orientation Orientation, Size? Size, bool Closable) : IAppearance
    {
        IEnumerable<Enum?> IAppearance.Variants => [Variant, Orientation, Size];
    }

    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<TabsPart> Of(TabsAppearance a, ComponentStyles styles) =>
        Recipe.Of("tabs", a, styles, static (TabsAppearance a, TabsPart part) =>
        {
            var vertical = a.Orientation == Orientation.Vertical;
            var line = a.Variant == TabsVariant.Line;
            return part switch
            {
                TabsPart.Root => Sx.Of(Root, vertical ? VerticalRoot : null, a.Size is { } size ? ScaleStyles.FontSize(size) : null),
                TabsPart.Bar => Sx.Of(Bar, line ? BarLine : null),
                TabsPart.List => Sx.Of(
                    List,
                    vertical ? null : Scrollable,
                    line ? LineList : SoftList,
                    line && vertical ? LineListVertical : null,
                    !line && vertical ? SoftListVertical : null),
                TabsPart.Indicator => Sx.Of(Indicator, vertical ? IndicatorVertical : IndicatorHorizontal),
                TabsPart.BarButton => BarButton,
                _ => Sx.Of(Panel, SharedStyles.FocusRing),
            };
        });

    /// <summary>一个标签的各部件（单独的配方，同 vue-tsx-pro 的 tabRecipe）。</summary>
    public static PartLayers<TabPart> TabOf(TabsAppearance a, ComponentStyles styles) =>
        Recipe.Of("tab", a, styles, static (TabsAppearance a, TabPart part) =>
        {
            var vertical = a.Orientation == Orientation.Vertical;
            var line = a.Variant == TabsVariant.Line;
            return part switch
            {
                TabPart.Root => Sx.Of(
                    SharedStyles.ButtonReset,
                    SharedStyles.InteractionColors,
                    SharedStyles.FocusRing,
                    SharedStyles.RippleHost,
                    Tab,
                    line ? LineTab : SoftTab,
                    line && vertical ? LineTabVertical : null,
                    a.Closable ? ClosableTab : null),
                TabPart.CloseWrapper => CloseWrapper,
                _ => Close,
            };
        });

    // 能收缩：放在顶栏这类一行里时，列表放不下就在列表里滚动
    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Md, MinWidth = 0 });

    static Style VerticalRoot => Css.Define(static () => new Style { FlexDirection = FlexDirection.Row, AlignItems = AlignItems.FlexStart });

    // 横排时包着标签列表与右边的按钮（滚动、新建）：列表按内容宽，放不下时收缩并滚动
    static Style Bar => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Xs, MinWidth = 0 });

    // 横排 line 的分隔线画在整行上：标签少时也铺满
    static Style BarLine => Css.Define(static () => new Style
    {
        BorderBlockEndWidth = BorderWidths.Xs,
        BorderBlockEndStyle = BorderStyle.Solid,
        BorderBlockEndColor = Colors.BorderMuted,
    });

    static Style List => Css.Define(static () => new Style { Display = Display.Flex, FlexShrink = 0 });

    // 横排放不下时在列表里横向滚动；滚动条藏起来，触控板、拖动照样能滚。
    // 还能往哪头滚（JS 标 data-fade：start、end、both），哪头的边缘渐隐：被截断的标签不是生硬地切开；从右到左时镜像
    static Style Scrollable => Css.Define(static () => new Style
    {
        FlexShrink = 1,
        MinWidth = 0,
        OverflowX = Overflow.Auto,
        OverflowY = Overflow.Hidden,
        ScrollbarWidth = ScrollbarWidth.None,
        [When.Attribute("[data-fade=start]")] = Fade(start: true, end: false),
        [When.Attribute("[data-fade=end]")] = Fade(start: false, end: true),
        [When.Attribute("[data-fade=both]")] = Fade(start: true, end: true),
    });

    static Style Fade(bool start, bool end) => new()
    {
        MaskImage = FadeMask("right", start, end),
        [When.Rtl] = new() { MaskImage = FadeMask("left", start, end) },
    };

    // to 是行内方向（从右到左时 left）
    static string FadeMask(string to, bool start, bool end) =>
        $"linear-gradient(to {to}, {(start ? "transparent, #000 1.5em" : "#000")}, {(end ? "#000 calc(100% - 1.5em), transparent" : "#000")})";

    // 指示条按标签的 offsetLeft / offsetTop 定位，列表要是它的定位容器
    static Style LineList => Css.Define(static () => new Style { Position = Position.Relative, Gap = Space.Md });

    static Style LineListVertical => Css.Define(static () => new Style
    {
        FlexDirection = FlexDirection.Column,
        Gap = Space.Xs,
        BorderInlineEndWidth = BorderWidths.Xs,
        BorderInlineEndStyle = BorderStyle.Solid,
        BorderInlineEndColor = Colors.BorderMuted,
    });

    static Style SoftList => Css.Define(static () => new Style { Gap = Space.Xs, Padding = Space.Xs, BorderRadius = Radii.Md, BackgroundColor = Colors.Fill });

    static Style SoftListVertical => Css.Define(static () => new Style { FlexDirection = FlexDirection.Column });

    // 一条 2px 的品牌色线，压在列表的分隔线上；位置与长度由 JS 写（transform 与宽高）。
    // JS 按标签的 offsetLeft / offsetTop（物理坐标）平移，所以起点是物理的 left / top，从右到左时也对得上
    static Style Indicator => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        BorderRadius = Radii.Sm,
        BackgroundColor = Tones.Accent.Solid,
        PointerEvents = PointerEvents.None,
        Transition = $"transform {Durations.Md} {Easings.Standard}, width {Durations.Md} {Easings.Standard}, height {Durations.Md} {Easings.Standard}",
    });

    // 横排：压在列表底边的分隔线上（滚动的列表会裁掉伸出去的部分，所以放在边框内侧）
    static Style IndicatorHorizontal => Css.Define(static () => new Style { Left = 0, InsetBlockEnd = 0, Width = 0, Height = BorderWidths.Sm });

    // 竖排：压在列表结束边的分隔线上。不与横排的 left 叠用：从右到左时结束边就是 left，物理属性会盖过它
    static Style IndicatorVertical => Css.Define(static () => new Style
    {
        Top = 0,
        InsetInlineEnd = $"calc(-1 * {BorderWidths.Xs})",
        Width = BorderWidths.Sm,
        Height = 0,
    });

    static Style Tab => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Sm,
        MinHeight = $"max({Sizes.Control}, {Sizes.MinTarget})",
        PaddingInline = Space.Md,
        Color = Colors.TextMuted,
        FontWeight = FontWeights.Medium,
        WhiteSpace = WhiteSpace.Nowrap,
        BorderRadius = Radii.Sm,
        [When.Hover] = new() { Color = Colors.Text },
        [When.Selected] = new() { Color = Colors.Text },
        [When.Attribute("[data-dragging]")] = new() { Opacity = 0.6 },
        [When.Disabled] = new() { Opacity = Opacities.Disabled },
    });

    // 下划线：线由列表的指示条画（切换时滑过去），标签自己只变文字颜色；底边留出线的位置
    static Style LineTab => Css.Define(static () => new Style
    {
        MarginBlockEnd = $"calc(-1 * {BorderWidths.Xs})",
        BorderBlockEndWidth = BorderWidths.Sm,
        BorderBlockEndStyle = BorderStyle.Solid,
        BorderBlockEndColor = CssColors.Transparent,
    });

    // 竖排叠在 LineTab 上（同 vue 的 compound）：线换到结束边
    static Style LineTabVertical => Css.Define(static () => new Style
    {
        JustifyContent = JustifyContent.FlexStart,
        MarginBlockEnd = 0,
        BorderBlockEndWidth = 0,
        MarginInlineEnd = $"calc(-1 * {BorderWidths.Xs})",
        BorderInlineEndWidth = BorderWidths.Sm,
        BorderInlineEndStyle = BorderStyle.Solid,
        BorderInlineEndColor = CssColors.Transparent,
    });

    static Style SoftTab => Css.Define(static () => new Style
    {
        BackgroundColor = CssColors.Transparent,
        [When.Selected] = new() { BackgroundColor = Colors.Surface, BoxShadow = Shadows.Xs },
    });

    // 可关闭：标签与关闭按钮叠在同一个网格格子里，关闭按钮靠结束端，标签给它留出位置。
    // 不用定位：标签的 offsetParent 还得是列表，下划线指示条按它的 offsetLeft 对齐
    static Style ClosableTab => Css.Define(static () => new Style { GridArea = "1 / 1", PaddingInlineEnd = $"calc({Space.Md} + 1.25em)" });

    static Style CloseWrapper => Css.Define(static () => new Style { Display = Display.InlineGrid, FlexShrink = 0 });

    static Style Close => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        JustifySelf = JustifySelf.End,
        AlignSelf = AlignSelf.Center,
        MarginInlineEnd = Space.Xs,
    });

    // 滚动按钮一开始是 hidden，放不下时 JS 去掉（按钮自己的 display 盖过了浏览器对 hidden 的处理）
    static Style BarButton => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        [When.Attribute("[hidden]")] = new() { Display = Display.None },
    });

    static Style Panel => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0, BorderRadius = Radii.Sm });
}
