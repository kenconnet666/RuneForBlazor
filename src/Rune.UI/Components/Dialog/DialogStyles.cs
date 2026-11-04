using Rune.Styles;

namespace Rune.UI;

/// <summary>对话框（抽屉）的宽度档位。</summary>
public enum DialogWidth
{
    /// <summary>24em：确认、简短的提示。</summary>
    Sm,

    /// <summary>32em（默认）：一般的表单。</summary>
    Md,

    /// <summary>48em：较宽的表单、详情。</summary>
    Lg,

    /// <summary>64em：表格、预览。</summary>
    Xl,
}

/// <summary>对话框与抽屉的部件。</summary>
public enum DialogPart
{
    /// <summary>dialog 元素。</summary>
    Root,

    /// <summary>页头：标题与关闭按钮。</summary>
    Header,

    /// <summary>标题（h2，也是对话框的名称）。</summary>
    Title,

    /// <summary>关闭按钮。</summary>
    Close,

    /// <summary>正文（内容多时在这里滚动，页头页脚不动）。</summary>
    Body,

    /// <summary>页脚：操作按钮。</summary>
    Footer,
}

/// <summary>
/// 对话框与抽屉的样式（配方）。原生的模态 <c>&lt;dialog&gt;</c> 已在顶层、带遮罩；这里换成主题的表面，
/// 进出场与遮罩用 <see cref="OverlayTransitions"/>（对话框缩放，抽屉从一边滑入）。
/// </summary>
public static class DialogStyles
{
    /// <summary>对话框（居中）的部件默认层。</summary>
    public static PartLayers<DialogPart> Dialog(DialogWidth width, ComponentStyles styles) =>
        Recipe.Of("dialog", width, styles, static (DialogWidth width, DialogPart part) => part switch
        {
            DialogPart.Root => Sx.Of(Surface, OverlayTransitions.Scale, OverlayTransitions.Backdrop, Centered, WidthOf(width), SharedStyles.ReducedMotion),
            _ => Inner(part),
        });

    /// <summary>抽屉（贴着页面一边滑出）的部件默认层。</summary>
    public static PartLayers<DialogPart> Drawer(Placement placement, DialogWidth width, ComponentStyles styles) =>
        Recipe.Of("drawer", (placement, width), styles, static ((Placement Placement, DialogWidth Width) a, DialogPart part) => part switch
        {
            DialogPart.Root => Sx.Of(
                Surface,
                OverlayTransitions.Slide(HiddenOffset(a.Placement)),
                OverlayTransitions.Backdrop,
                Side(a.Placement),
                a.Placement is Placement.Start or Placement.End ? WidthOf(a.Width) : null,
                SharedStyles.ReducedMotion),
            _ => Inner(part),
        });

    static Sx Inner(DialogPart part) => part switch
    {
        DialogPart.Header => Header,
        DialogPart.Title => Title,
        DialogPart.Close => Close,
        DialogPart.Body => Body,
        _ => Footer,
    };

    static Style Surface => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        FlexDirection = FlexDirection.Column,
        Padding = 0,
        BorderWidth = 1,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BackgroundColor = Colors.Background,
        Color = Colors.Text,
        BoxShadow = Shadows.Lg,
        FontSize = FontSizes.Md,
        LineHeight = LineHeights.Normal,
        Overflow = "hidden",
        // 打开时才是 flex：dialog 关闭时浏览器给的 display: none 不能被盖掉
        [When.Open] = new() { Display = Display.Flex },
    });

    static Style Centered => Css.Define(static () => new Style
    {
        MaxHeight = "calc(100dvh - 64px)",
        BorderRadius = Radii.Lg,
    });

    static Style WidthOf(DialogWidth width) => Css.Define(width, static width => new Style
    {
        Width = width switch
        {
            DialogWidth.Sm => "24em",
            DialogWidth.Lg => "48em",
            DialogWidth.Xl => "64em",
            _ => "32em",
        },
        MaxWidth = "calc(100vw - 32px)",
    });

    // 抽屉隐藏时的位移：移到它那一边的外面
    static string HiddenOffset(Placement placement) => placement switch
    {
        Placement.Start => "-100% 0",
        Placement.Top => "0 -100%",
        Placement.Bottom => "0 100%",
        _ => "100% 0",
    };

    // 抽屉：贴边、占满那一边（另一个方向按内容或宽度档位）
    static Style Side(Placement placement) => Css.Define(placement, static placement =>
        placement switch
        {
            Placement.Start or Placement.End => new Style
            {
                MarginBlock = 0,
                MarginInline = placement == Placement.End ? "auto 0" : "0 auto",
                Height = "100dvh",
                MaxHeight = "100dvh",
                BorderRadius = 0,
            },
            _ => new Style
            {
                MarginInline = 0,
                MarginBlock = placement == Placement.Bottom ? "auto 0" : "0 auto",
                Width = "100vw",
                MaxWidth = "100vw",
                MaxHeight = "85dvh",
                BorderRadius = 0,
            },
        });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        PaddingInline = Space.Lg,
        PaddingBlockStart = Space.Lg,
        PaddingBlockEnd = Space.Sm,
    });

    static Style Title => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinWidth = 0,
        Margin = 0,
        FontSize = FontSizes.Lg,
        FontWeight = FontWeights.Semibold,
        LineHeight = LineHeights.Tight,
    });

    static Style Close => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        MarginBlock = $"calc(-1 * {Space.Xs})",
        MarginInlineEnd = $"calc(-1 * {Space.Xs})",
    });

    static Style Body => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinHeight = 0,
        PaddingInline = Space.Lg,
        PaddingBlock = Space.Sm,
        OverflowY = "auto",
    });

    static Style Footer => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = Space.Sm,
        PaddingInline = Space.Lg,
        PaddingBlockStart = Space.Sm,
        PaddingBlockEnd = Space.Lg,
    });
}
