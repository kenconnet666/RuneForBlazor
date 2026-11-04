using Rune.Styles;

namespace Rune.UI;

/// <summary>对话框的宽度档位（同 vue-tsx-pro 的 DialogSizes）：与 Container 相同（30–80rem），另加 Full 占满视口。</summary>
public enum DialogSize
{
    /// <summary>30rem（默认）：确认、简短表单。</summary>
    Xs,

    /// <summary>40rem。</summary>
    Sm,

    /// <summary>48rem。</summary>
    Md,

    /// <summary>64rem。</summary>
    Lg,

    /// <summary>80rem。</summary>
    Xl,

    /// <summary>占满视口（四周留出 1em）。</summary>
    Full,
}

/// <summary>对话框与抽屉的部件（同 vue-tsx-pro 的 ModalPart）。</summary>
public enum DialogPart
{
    /// <summary>dialog 元素。</summary>
    Root,

    /// <summary>标题行：标题与关闭按钮。</summary>
    Header,

    /// <summary>标题（h2，交给 H2）。</summary>
    Title,

    /// <summary>关闭按钮（交给 CloseButton）。</summary>
    Close,

    /// <summary>标题下的说明（交给 Paragraph）。</summary>
    Description,

    /// <summary>正文：内容长时只滚这里，标题与页脚固定。</summary>
    Body,

    /// <summary>页脚：操作按钮，靠结束边排列。</summary>
    Footer,
}

/// <summary>对话框的默认值。</summary>
public sealed record DialogDefaults : IComponentDefaults<DialogDefaults>
{
    /// <summary>宽度档位。</summary>
    public DialogSize? Size { get; init; }

    /// <inheritdoc/>
    public DialogDefaults Over(DialogDefaults outer) => new() { Size = Size ?? outer.Size };
}

/// <summary>抽屉的默认值。</summary>
public sealed record DrawerDefaults : IComponentDefaults<DrawerDefaults>
{
    /// <summary>宽度档位（左右两侧）。</summary>
    public ContainerSize? Size { get; init; }

    /// <inheritdoc/>
    public DrawerDefaults Over(DrawerDefaults outer) => new() { Size = Size ?? outer.Size };
}

/// <summary>对话框与抽屉的样式（同 vue-tsx-pro 的 dialogRecipe、drawerRecipe 与 modalPartStyles）。</summary>
public static class DialogStyles
{
    /// <summary>对话框（居中、淡入放大）的部件默认层。</summary>
    public static PartLayers<DialogPart> Dialog(DialogSize size, ComponentStyles styles) =>
        Recipe.Of("dialog", size, styles, static (DialogSize size, DialogPart part) => part switch
        {
            DialogPart.Root => Sx.Of(Surface, Centered, DialogSizeOf(size), OverlayTransitions.Scale, OverlayTransitions.Backdrop),
            _ => Inner(part),
        });

    /// <summary>抽屉（贴着视口一侧滑入）的部件默认层。</summary>
    public static PartLayers<DialogPart> Drawer(DrawerSide side, ContainerSize size, ComponentStyles styles) =>
        Recipe.Of("drawer", (side, size), styles, static ((DrawerSide Side, ContainerSize Size) a, DialogPart part) => part switch
        {
            DialogPart.Root => Sx.Of(
                Surface,
                DrawerRoot,
                SideOf(a.Side),
                a.Side is DrawerSide.Start or DrawerSide.End ? DrawerSizeOf(a.Size) : null,
                OverlayTransitions.Backdrop),
            _ => Inner(part),
        });

    static Sx Inner(DialogPart part) => part switch
    {
        DialogPart.Header => Header,
        DialogPart.Title => Title,
        DialogPart.Close => Close,
        DialogPart.Description => Description,
        DialogPart.Body => Body,
        _ => Footer,
    };

    // 只在模态打开时排版；关闭时回到 none，离场过渡由 display 的 allow-discrete 撑住
    static Style Surface => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.None,
        FlexDirection = FlexDirection.Column,
        Padding = 0,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BackgroundColor = Colors.SurfaceRaised,
        Color = Colors.Text,
        BoxShadow = Shadows.Xl,
        LineHeight = LineHeights.Normal,
        [When.Modal] = new() { Display = Display.Flex },
    });

    // 原生 dialog 已限制 max-width / max-height 不超出视口；这里只把高度上限放到四周各留 1em
    static Style Centered => Css.Define(static () => new Style
    {
        Width = CssLength.Percent(100),
        MaxHeight = "calc(100dvh - 2em)",
        BorderRadius = Radii.Xl,
    });

    static Style DialogSizeOf(DialogSize size) => Css.Define(size, static size => size switch
    {
        DialogSize.Full => new Style { MaxWidth = "calc(100vw - 2em)", Height = "calc(100dvh - 2em)" },
        DialogSize.Sm => WidthUpTo(Sizes.Sm),
        DialogSize.Md => WidthUpTo(Sizes.Md),
        DialogSize.Lg => WidthUpTo(Sizes.Lg),
        DialogSize.Xl => WidthUpTo(Sizes.Xl),
        _ => WidthUpTo(Sizes.Xs),
    });

    static Style WidthUpTo(CssLength width) => new() { MaxWidth = $"min({width}, calc(100vw - 2em))" };

    static CssLength ContainerWidth(ContainerSize size) => size switch
    {
        ContainerSize.Xs => Sizes.Xs,
        ContainerSize.Sm => Sizes.Sm,
        ContainerSize.Md => Sizes.Md,
        ContainerSize.Lg => Sizes.Lg,
        _ => Sizes.Xl,
    };

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Md,
        PaddingBlockStart = Space.Xl,
        PaddingInline = Space.Xl,
    });

    static Style Title => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0, Margin = 0 });

    // 关闭按钮的点击区域伸到标题行的内边距里：按钮的图标与标题文字对齐
    static Style Close => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        MarginBlock = $"calc(-1 * {Space.Xs})",
        MarginInlineEnd = $"calc(-1 * {Space.Sm})",
    });

    static Style Description => Css.Define(static () => new Style
    {
        MarginBlockStart = Space.Xs,
        PaddingInline = Space.Xl,
        Color = Colors.TextMuted,
    });

    static Style Body => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinHeight = 0,
        OverflowY = Overflow.Auto,
        OverscrollBehavior = OverscrollBehavior.Contain,
        PaddingBlock = Space.Lg,
        PaddingInline = Space.Xl,
    });

    static Style Footer => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = Space.Sm,
        PaddingBlockEnd = Space.Xl,
        PaddingInline = Space.Xl,
    });

    // 抽屉：去掉居中，贴着所在一侧；进场从那一侧滑入，离场滑回去（减少动态效果时时长为 0）。
    // 打开时 translate 是 none 而不是 0 0：否则抽屉会成为 fixed 子元素（移进来的 Toaster）的包含块
    static Style DrawerRoot => Css.Define(static () => new Style
    {
        Margin = 0,
        MaxWidth = CssLength.Vw(100),
        MaxHeight = CssLength.Dvh(100),
        BorderRadius = 0,
        Transition = DrawerTransition(Durations.Md, Easings.Exit),
        [When.Modal] = new() { Translate = CssLength.None, Transition = DrawerTransition(Durations.Lg, Easings.Enter) },
    });

    static string DrawerTransition(CssValue duration, CssValue easing) =>
        $"translate {duration} {easing}, display {duration} allow-discrete, overlay {duration} allow-discrete";

    // 左右两侧占满视口高度、宽度看档位，上下两侧占满视口宽度、高度由内容决定。
    // 位置用逻辑属性，从右到左排版自动换边；translate 是物理方向，用 :dir() 区分
    static Style SideOf(DrawerSide side) => Css.Define(side, static side => side switch
    {
        DrawerSide.Start => new Style
        {
            InsetBlock = 0,
            Width = CssLength.Percent(100),
            Height = CssLength.Dvh(100),
            Overflow = Overflow.Hidden,
            InsetInlineStart = 0,
            InsetInlineEnd = CssLength.Auto,
            BorderInlineStartWidth = 0,
            Translate = "-100% 0",
            [When.Rtl] = new() { Translate = "100% 0" },
            [When.Modal] = new() { [When.Starting] = new() { Translate = "-100% 0", [When.Rtl] = new() { Translate = "100% 0" } } },
        },
        DrawerSide.Top => new Style
        {
            InsetInline = 0,
            Width = CssLength.Vw(100),
            Height = Height.Auto,
            Top = 0,
            Bottom = CssLength.Auto,
            BorderBlockStartWidth = 0,
            Translate = "0 -100%",
            [When.Modal] = new() { [When.Starting] = new() { Translate = "0 -100%" } },
        },
        DrawerSide.Bottom => new Style
        {
            InsetInline = 0,
            Width = CssLength.Vw(100),
            Height = Height.Auto,
            Top = CssLength.Auto,
            Bottom = 0,
            BorderBlockEndWidth = 0,
            Translate = "0 100%",
            [When.Modal] = new() { [When.Starting] = new() { Translate = "0 100%" } },
        },
        _ => new Style
        {
            InsetBlock = 0,
            Width = CssLength.Percent(100),
            Height = CssLength.Dvh(100),
            Overflow = Overflow.Hidden,
            InsetInlineStart = CssLength.Auto,
            InsetInlineEnd = 0,
            BorderInlineEndWidth = 0,
            Translate = "100% 0",
            [When.Rtl] = new() { Translate = "-100% 0" },
            [When.Modal] = new() { [When.Starting] = new() { Translate = "100% 0", [When.Rtl] = new() { Translate = "-100% 0" } } },
        },
    });

    static Style DrawerSizeOf(ContainerSize size) => Css.Define(size, static size => new Style
    {
        MaxWidth = $"min({ContainerWidth(size)}, 100vw)",
    });
}
