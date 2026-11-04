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
/// 进出场用 <c>@starting-style</c> 与 display / overlay 的离散过渡（关闭后先播完动画再隐藏，不需要 JS）。
/// </summary>
public static class DialogStyles
{
    /// <summary>对话框（居中）的部件默认层。</summary>
    public static PartLayers<DialogPart> Dialog(DialogWidth width, ComponentStyles styles) =>
        Recipe.Of("dialog", width, styles, static (DialogWidth width, DialogPart part) => part switch
        {
            DialogPart.Root => Sx.Of(Surface, Centered, WidthOf(width), SharedStyles.ReducedMotion),
            _ => Inner(part),
        });

    /// <summary>抽屉（贴着页面一边滑出）的部件默认层。</summary>
    public static PartLayers<DialogPart> Drawer(Placement placement, DialogWidth width, ComponentStyles styles) =>
        Recipe.Of("drawer", (placement, width), styles, static ((Placement Placement, DialogWidth Width) a, DialogPart part) => part switch
        {
            DialogPart.Root => Sx.Of(
                Surface,
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
        Opacity = 0,
        Transition = $"opacity {Durations.Normal} {Easings.Standard}, translate {Durations.Normal} {Easings.Standard}, scale {Durations.Normal} {Easings.Standard}, " +
                     $"overlay {Durations.Normal} allow-discrete, display {Durations.Normal} allow-discrete",
        [Pseudo.Backdrop] = new()
        {
            BackgroundColor = "rgb(0 0 0 / 0%)",
            Transition = $"background-color {Durations.Normal}, overlay {Durations.Normal} allow-discrete, display {Durations.Normal} allow-discrete",
        },
        // 打开时才是 flex：dialog 关闭时浏览器给的 display: none 不能被盖掉
        [When.Open] = new()
        {
            Display = Display.Flex,
            Opacity = 1,
            Translate = "0 0",
            Scale = 1,
            [Pseudo.Backdrop] = new()
            {
                BackgroundColor = "rgb(0 0 0 / 45%)",
                [When.Starting] = new() { BackgroundColor = "rgb(0 0 0 / 0%)" },
            },
        },
    });

    // 居中的对话框：从略小放大到正常
    static Style Centered => Css.Define(static () => new Style
    {
        MaxHeight = "calc(100dvh - 64px)",
        BorderRadius = Radii.Lg,
        Scale = 0.96,
        [When.Open] = new() { [When.Starting] = new() { Opacity = 0, Scale = 0.96 } },
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

    // 抽屉：贴边、占满那一边（另一个方向按内容或宽度档位），从那一边滑进来
    static Style Side(Placement placement) => Css.Define(placement, static placement =>
    {
        var hidden = placement switch
        {
            Placement.Start => "-100% 0",
            Placement.Top => "0 -100%",
            Placement.Bottom => "0 100%",
            _ => "100% 0",
        };
        return placement switch
        {
            Placement.Start or Placement.End => new Style
            {
                MarginBlock = 0,
                MarginInline = placement == Placement.End ? "auto 0" : "0 auto",
                Height = "100dvh",
                MaxHeight = "100dvh",
                BorderRadius = 0,
                Translate = hidden,
                [When.Open] = new() { [When.Starting] = new() { Opacity = 0, Translate = hidden } },
            },
            _ => new Style
            {
                MarginInline = 0,
                MarginBlock = placement == Placement.Bottom ? "auto 0" : "0 auto",
                Width = "100vw",
                MaxWidth = "100vw",
                MaxHeight = "85dvh",
                BorderRadius = 0,
                Translate = hidden,
                [When.Open] = new() { [When.Starting] = new() { Opacity = 0, Translate = hidden } },
            },
        };
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
