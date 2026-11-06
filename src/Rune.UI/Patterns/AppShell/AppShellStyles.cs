using Rune.Styles;

namespace Rune.UI;

/// <summary>AppShell 的部件。</summary>
public enum AppShellPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>粘性顶栏（header）。</summary>
    Header,

    /// <summary>窄屏时打开侧栏抽屉的按钮（IconButton 组件）。</summary>
    MenuButton,

    /// <summary>顶栏的品牌格（div）。</summary>
    Brand,

    /// <summary>顶栏里品牌后面的导航格（div）。</summary>
    Nav,

    /// <summary>顶栏里 Header 内容那一格（div，含全屏按钮）。</summary>
    HeaderContent,

    /// <summary>Header 内容本身（div）。</summary>
    HeaderMain,

    /// <summary>全屏按钮（IconButton 组件）。</summary>
    FullscreenButton,

    /// <summary>全屏时右上角的退出按钮（IconButton 组件）。</summary>
    FullscreenExit,

    /// <summary>顶栏下面的主体：侧栏、正文、右栏的栅格（div）。</summary>
    Body,

    /// <summary>左侧栏（div）。</summary>
    Sidebar,

    /// <summary>左侧栏里的滚动区域（ScrollArea 组件）。</summary>
    SidebarScroll,

    /// <summary>左侧栏底部放“收起侧栏”按钮的一条（div）。</summary>
    CollapseBar,

    /// <summary>正文那一列：正文与页脚（div）。</summary>
    Column,

    /// <summary>正文（main）。</summary>
    Main,

    /// <summary>页脚（footer）。</summary>
    Footer,

    /// <summary>右侧栏（aside）。</summary>
    Aside,

    /// <summary>右侧栏里的滚动区域（ScrollArea 组件）。</summary>
    AsideScroll,
}

/// <summary>
/// 应用外壳的样式（同 vue-tsx-pro 的 appShellStyles）。宽度档位：<see cref="Breakpoints.Lg"/>（64rem）以上左侧栏常驻，
/// <see cref="Breakpoints.Xl"/>（80rem）以上显示右侧栏。
/// </summary>
public static class AppShellStyles
{
    // 顶栏高度：也是两侧栏粘住的位置、标题跳转时让开的距离（LayoutVars.StickyOffset）
    static readonly CssLength HeaderHeight = CssLength.Rem(3);

    // 左侧栏的宽度：顶栏的品牌格与它同宽
    static readonly CssLength SidebarWidth = CssLength.Rem(16);

    // 侧栏收起成图标栏时的宽度
    static readonly CssLength RailWidth = CssLength.Rem(3.5);

    // 右侧栏的宽度
    static readonly CssLength AsideWidth = CssLength.Rem(14);

    /// <summary>侧栏常驻的媒体查询（窄屏抽屉在这个宽度以上自动关上）。</summary>
    public const string WideQuery = "(min-width: 64rem)";

    /// <summary>外壳的布局状态（配方的键）。</summary>
    /// <param name="Brand">写了品牌。</param>
    /// <param name="Sidebar">有左侧栏（全屏时也算：侧栏留在页面里，只是藏起来）。</param>
    /// <param name="Aside">有右侧栏。</param>
    /// <param name="Nav">顶栏里有导航。</param>
    /// <param name="Collapsible">侧栏可以收起。</param>
    /// <param name="Collapsed">侧栏收起成了图标栏。</param>
    /// <param name="Fullscreen">全屏中：不显示顶栏和左侧栏。</param>
    public readonly record struct ShellState(bool Brand, bool Sidebar, bool Aside, bool Nav, bool Collapsible, bool Collapsed, bool Fullscreen);

    /// <summary>某种布局状态、某套定制下各部件的默认层。</summary>
    public static PartLayers<AppShellPart> Of(ShellState state, ComponentStyles styles) =>
        Recipe.Of("app-shell", state, styles, static (ShellState s, AppShellPart part) => part switch
        {
            AppShellPart.Root => Sx.Of(Root, s.Fullscreen ? FullscreenOffset : HeaderOffset),
            AppShellPart.Header => Sx.Of(Header, s.Brand && s.Sidebar ? HeaderWithBrand : null, s.Fullscreen ? Hidden : null),
            AppShellPart.MenuButton => MenuButton,
            AppShellPart.Brand => Sx.Of(Brand, s.Sidebar ? BrandRail : null, s.Collapsed ? BrandRailCollapsed : null),
            AppShellPart.Nav => Nav,
            AppShellPart.HeaderContent => Sx.Of(HeaderContent, s.Nav ? HeaderContentAfterNav : null),
            AppShellPart.HeaderMain => HeaderContent,
            AppShellPart.FullscreenButton => FullscreenButton,
            AppShellPart.FullscreenExit => FullscreenExit,
            AppShellPart.Body => Sx.Of(Body, Columns(s.Sidebar && !s.Fullscreen, s.Aside, s.Collapsed)),
            AppShellPart.Sidebar => Sx.Of(Rail, Sidebar, s.Collapsible ? SidebarColumn : null, s.Fullscreen ? SidebarHidden : null),
            AppShellPart.SidebarScroll => Sx.Of(RailScroll, s.Collapsible ? SidebarScroll : null),
            AppShellPart.CollapseBar => Sx.Of(CollapseBar, s.Collapsed ? CollapseBarCollapsed : null),
            AppShellPart.Column => Column,
            AppShellPart.Main => Main,
            AppShellPart.Footer => Footer,
            AppShellPart.Aside => Sx.Of(Rail, Aside),
            AppShellPart.AsideScroll => RailScroll,
            _ => Sx.Of(),
        });

    /// <summary>两侧栏滚动区域的视口：滚到头不带动页面。</summary>
    internal static Style RailViewport => Css.Define(static () => new Style { OverscrollBehavior = OverscrollBehavior.Contain });

    /// <summary>左侧栏滚动区域的内容层；紧凑（图标栏）时两边只留一点，图标居中。</summary>
    internal static Style SidebarContent(bool compact) => compact ? SidebarContentCompact : SidebarContentFull;

    /// <summary>右侧栏滚动区域的内容层。</summary>
    internal static Style AsideContent => Css.Define(static () => new Style { PaddingBlock = Space.Xl, PaddingInline = Space.Lg });

    /// <summary>品牌的产品名：侧栏收起成图标栏（宽屏）时只留给读屏。</summary>
    internal static Style BrandNameCollapsed => Css.Define(static () => new Style
    {
#pragma warning disable RUNE007 // 同 SharedStyles.VisuallyHidden，只在宽屏时
        [Breakpoints.Lg] = new()
        {
            Position = Position.Absolute,
            Width = 1,
            Height = 1,
            Padding = 0,
            Margin = -1,
            Overflow = Overflow.Hidden,
            Clip = "rect(0, 0, 0, 0)",
            WhiteSpace = WhiteSpace.Nowrap,
            BorderWidth = 0,
        },
#pragma warning restore RUNE007
    });

    // 栅格的列：侧栏（常驻的宽度以上）、正文、右栏（更宽时）；一个属性上两个断点时窄的一档用 LgOnly，不和 Xl 重叠
    static Style? Columns(bool sidebar, bool aside, bool collapsed) => (sidebar, aside) switch
    {
        (true, true) => collapsed ? WithBothCollapsed : WithBoth,
        (true, false) => collapsed ? WithSidebarCollapsed : WithSidebar,
        (false, true) => WithAside,
        _ => null,
    };

    // 整页的界面基准文字（同 Provider）：内容少时页脚也贴着底部
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        MinHeight = CssLength.Dvh(100),
        Color = Colors.Text,
        FontSize = FontSizes.Md,
        LineHeight = LineHeights.Normal,
    });

    // 外壳把顶栏高度交给里面的标题与粘性侧栏；全屏时没有顶栏，贴着窗口顶端
    static Style HeaderOffset => Css.Define(static () => new Style { [LayoutVars.StickyOffset.Name] = HeaderHeight.ToString() });

    static Style FullscreenOffset => Css.Define(static () => new Style { [LayoutVars.StickyOffset.Name] = "0px" });

    static Style Header => Css.Define(static () => new Style
    {
        Position = Position.Sticky,
        Top = 0,
        // 盖住滚动的正文与粘性侧栏；低于浮层（顶层渲染）与 SkipLink
        ZIndex = 10,
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Md,
        Height = HeaderHeight,
        FlexShrink = 0,
        PaddingInline = Space.Lg,
        BorderBlockEndWidth = BorderWidths.Xs,
        BorderBlockEndStyle = BorderStyle.Solid,
        BorderBlockEndColor = Colors.Border,
        // 半透明：滚动的正文从下面透出
        BackgroundColor = Colors.SurfaceTranslucent,
        BackdropFilter = "blur(12px) saturate(1.5)",
    });

    static Style MenuButton => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        FlexShrink = 0,
        [Breakpoints.Lg] = new() { Display = Display.None },
    });

    // 有品牌格与侧栏：宽屏时品牌格贴着起始边；品牌格与内容之间隔开正文的内边距，内容与正文的起始边对齐
    static Style HeaderWithBrand => Css.Define(static () => new Style
    {
        [Breakpoints.Lg] = new() { PaddingInlineStart = 0, ColumnGap = Space.Xl },
    });

    static Style Brand => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        FlexShrink = 0,
        MinWidth = 0,
    });

    // 宽屏时与侧栏同宽，结束边的竖线与侧栏的竖线接上
    static Style BrandRail => Css.Define(static () => new Style
    {
        [Breakpoints.Lg] = new()
        {
            AlignSelf = AlignSelf.Stretch,
            BoxSizing = BoxSizing.BorderBox,
            Width = SidebarWidth,
            PaddingInline = Space.Lg,
            BorderInlineEndWidth = BorderWidths.Xs,
            BorderInlineEndStyle = BorderStyle.Solid,
            BorderInlineEndColor = Colors.Border,
        },
    });

    static Style BrandRailCollapsed => Css.Define(static () => new Style
    {
        [Breakpoints.Lg] = new() { Width = RailWidth, JustifyContent = JustifyContent.Center, PaddingInline = Space.Xs },
    });

    // 顶栏的导航：占品牌与右边工具之间的位置；有导航时右边的工具按内容宽、靠结束边
    static Style Nav => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        AlignSelf = AlignSelf.Stretch,
        FlexGrow = 1,
        MinWidth = 0,
    });

    static Style HeaderContent => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Md,
        FlexGrow = 1,
        MinWidth = 0,
    });

    static Style HeaderContentAfterNav => Css.Define(static () => new Style { FlexGrow = 0, JustifyContent = JustifyContent.FlexEnd });

    // 手机上不显示：顶栏放不下，iOS Safari 也不支持页面全屏
    static Style FullscreenButton => Css.Define(static () => new Style
    {
        Display = Display.None,
        FlexShrink = 0,
        [Breakpoints.Md] = new() { Display = Display.InlineFlex },
    });

    // 全屏时的退出按钮：贴着窗口右上角（从右到左排版时是左上角），不用时淡一点，不抢正文
    static Style FullscreenExit => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        Top = Space.Sm,
        InsetInlineEnd = Space.Lg,
        ZIndex = 10,
        BackgroundColor = Colors.SurfaceTranslucent,
        Opacity = 0.6,
        [When.Hover] = new() { Opacity = 1 },
        [When.FocusVisible] = new() { Opacity = 1 },
    });

    // 全屏时不显示：留在 DOM 里，退出后侧栏的滚动位置、展开的分组都还在
    static Style Hidden => Css.Define(static () => new Style { Display = Display.None });

    // 侧栏的 display 写在断点里（常驻的宽度以上才显示），隐藏也要盖住那一档
    static Style SidebarHidden => Css.Define(static () => new Style { Display = Display.None, [Breakpoints.Lg] = new() { Display = Display.None } });

    static Style Body => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        FlexGrow = 1,
        GridTemplateColumns = "minmax(0, 1fr)",
    });

    static Style WithSidebar => Css.Define(static () => new Style
    {
        [Breakpoints.Lg] = new() { GridTemplateColumns = $"{SidebarWidth} minmax(0, 1fr)" },
    });

    static Style WithAside => Css.Define(static () => new Style
    {
        [Breakpoints.Xl] = new() { GridTemplateColumns = $"minmax(0, 1fr) {AsideWidth}" },
    });

    static Style WithBoth => Css.Define(static () => new Style
    {
        [Breakpoints.LgOnly] = new() { GridTemplateColumns = $"{SidebarWidth} minmax(0, 1fr)" },
        [Breakpoints.Xl] = new() { GridTemplateColumns = $"{SidebarWidth} minmax(0, 1fr) {AsideWidth}" },
    });

    static Style WithSidebarCollapsed => Css.Define(static () => new Style
    {
        [Breakpoints.Lg] = new() { GridTemplateColumns = $"{RailWidth} minmax(0, 1fr)" },
    });

    static Style WithBothCollapsed => Css.Define(static () => new Style
    {
        [Breakpoints.LgOnly] = new() { GridTemplateColumns = $"{RailWidth} minmax(0, 1fr)" },
        [Breakpoints.Xl] = new() { GridTemplateColumns = $"{RailWidth} minmax(0, 1fr) {AsideWidth}" },
    });

    // 两侧栏：粘在顶栏下面、占满剩下的视口高度，内容多时各自滚动
    static Style Rail => Css.Define(static () => new Style
    {
        Position = Position.Sticky,
        Top = LayoutVars.StickyOffset,
        BoxSizing = BoxSizing.BorderBox,
        Height = $"calc(100dvh - {LayoutVars.StickyOffset})",
    });

    // 侧栏里的滚动区域：浮层滚动条；内边距放在内容层上，滚动条贴着侧栏边缘
    static Style RailScroll => Css.Define(static () => new Style { Height = Sizes.Full });

    static Style Sidebar => Css.Define(static () => new Style
    {
        Display = Display.None,
        BorderInlineEndWidth = BorderWidths.Xs,
        BorderInlineEndStyle = BorderStyle.Solid,
        BorderInlineEndColor = Colors.Border,
        [Breakpoints.Lg] = new() { Display = Display.Block },
    });

    // 有收起按钮的侧栏：滚动区域占满剩下的高度，按钮在最下面
    static Style SidebarColumn => Css.Define(static () => new Style
    {
        FlexDirection = FlexDirection.Column,
        [Breakpoints.Lg] = new() { Display = Display.Flex },
    });

    static Style SidebarScroll => Css.Define(static () => new Style { FlexGrow = 1, MinHeight = 0, Height = CssLength.Auto });

    static Style SidebarContentFull => Css.Define(static () => new Style { PaddingBlock = Space.Xl, PaddingInline = Space.Md });

    static Style SidebarContentCompact => Css.Define(static () => new Style { PaddingBlock = Space.Xl, PaddingInline = Space.Xs });

    static Style CollapseBar => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        JustifyContent = JustifyContent.FlexEnd,
        FlexShrink = 0,
        Padding = Space.Sm,
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderBlockStartColor = Colors.BorderMuted,
    });

    static Style CollapseBarCollapsed => Css.Define(static () => new Style { JustifyContent = JustifyContent.Center });

    static Style Aside => Css.Define(static () => new Style
    {
        Display = Display.None,
        [Breakpoints.Xl] = new() { Display = Display.Block },
    });

    // 正文那一列：正文、页脚上下排，页脚在内容少时也贴着底部
    static Style Column => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, MinWidth = 0 });

    // 正文由 SkipLink 聚焦（tabindex=-1）：不显示焦点框
    static Style Main => Css.Define(static () => new Style
    {
        MinWidth = 0,
        FlexGrow = 1,
        PaddingBlock = Space.Xl,
        PaddingInline = Space.Xl,
        OutlineStyle = OutlineStyle.None,
    });

    static Style Footer => Css.Define(static () => new Style
    {
        PaddingBlock = Space.Lg,
        PaddingInline = Space.Xl,
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderBlockStartColor = Colors.BorderMuted,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
    });
}

/// <summary>AppShell 交给里面的部件的状态（级联值）：AppShellBrand 据此在侧栏收起时只显示标志。</summary>
/// <param name="Collapsed">侧栏收起成了图标栏（宽屏时）。</param>
public sealed record AppShellContext(bool Collapsed)
{
    /// <summary>侧栏展开（或没有侧栏）。</summary>
    public static readonly AppShellContext Expanded = new(false);

    /// <summary>侧栏收起成了图标栏。</summary>
    public static readonly AppShellContext Rail = new(true);

    /// <summary>按是否收起取共用的实例。</summary>
    public static AppShellContext Of(bool collapsed) => collapsed ? Rail : Expanded;
}
