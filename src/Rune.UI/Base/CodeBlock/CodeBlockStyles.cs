using Rune.Styles;

namespace Rune.UI;

/// <summary>代码块的部件。</summary>
public enum CodeBlockPart
{
    /// <summary>外层（div，包着标题栏、代码区与展开按钮）。</summary>
    Root,

    /// <summary>标题栏（div）。</summary>
    Header,

    /// <summary>标题栏里的标题文字（span）。</summary>
    Title,

    /// <summary>代码区（div，包着滚动区域与右上角的复制按钮）。</summary>
    Body,

    /// <summary>pre 元素。</summary>
    Pre,

    /// <summary>code 元素。</summary>
    Code,

    /// <summary>按行渲染时的一行（span）。</summary>
    Line,

    /// <summary>行号（span，对读屏隐藏）。</summary>
    LineNumber,

    /// <summary>复制按钮（CopyButton 组件）。</summary>
    Copy,

    /// <summary>展开、收起长代码的按钮。</summary>
    Expand,

    /// <summary>展开按钮里的箭头（Icon 组件）。</summary>
    ExpandIcon,
}

/// <summary>代码块的外观（配方的键）。</summary>
/// <param name="Flush">嵌在已有外框的容器里：去掉边框、圆角与底色。</param>
/// <param name="CopyInCorner">复制按钮在右上角（没有标题栏）：给它留出位置。</param>
/// <param name="Wrap">长行换行。</param>
/// <param name="Collapsed">长代码折叠着。</param>
/// <param name="ByLine">按行渲染（行号、行高亮、diff）。</param>
public readonly record struct CodeBlockAppearance(bool Flush, bool CopyInCorner, bool Wrap, bool Collapsed, bool ByLine);

/// <summary>按行标记的状态。</summary>
public enum CodeLineMark
{
    /// <summary>普通的行。</summary>
    None,

    /// <summary>强调。</summary>
    Highlighted,

    /// <summary>新增（diff）。</summary>
    Added,

    /// <summary>删除（diff）。</summary>
    Removed,
}

/// <summary>代码块的样式（同 vue-tsx-pro 的 codeBlockStyles）。</summary>
public static class CodeBlockStyles
{
    /// <summary>折叠时显示几行（写在 pre 上的变量）。</summary>
    public static readonly CssVar<CssLength> MaxLines = CssVar.Length("rune-code-block-max-lines");

    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<CodeBlockPart> Of(CodeBlockAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("code-block", appearance, styles, static (CodeBlockAppearance a, CodeBlockPart part) => part switch
        {
            CodeBlockPart.Root => Sx.Of(Wrapper, a.Flush ? Flush : null),
            CodeBlockPart.Header => Header,
            CodeBlockPart.Title => Title,
            CodeBlockPart.Body => Body,
            CodeBlockPart.Pre => Sx.Of(Pre, a.CopyInCorner ? WithCopy : null, a.Wrap ? Wrap : null, a.Collapsed ? Collapsed : null),
            CodeBlockPart.Code => Sx.Of(Code, a.ByLine ? Lines : null),
            CodeBlockPart.Line => Line,
            CodeBlockPart.LineNumber => LineNumber,
            CodeBlockPart.Copy => a.CopyInCorner ? Copy : Sx.Of(),
            CodeBlockPart.Expand => Sx.Of(Expand, SharedStyles.FocusRingInset),
            _ => ExpandIcon,
        });

    /// <summary>一行的标记样式；普通的行没有。</summary>
    public static Style? MarkOf(CodeLineMark mark) => mark switch
    {
        CodeLineMark.Highlighted => Highlighted,
        CodeLineMark.Added => Added,
        CodeLineMark.Removed => Removed,
        _ => null,
    };

    /// <summary>展开后箭头朝上。</summary>
    public static Style ExpandIconOpen => Css.Define(static () => new Style { Rotate = CssValue.Deg(180) });

    // MinWidth 0：放在 grid / flex 里时外层不被最长的一行代码撑宽（滚动的是里面的 pre）
    static Style Wrapper => Css.Define(static () => new Style
    {
        MinWidth = 0,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Surface,
        Overflow = Overflow.Hidden,
    });

    static Style Flush => Css.Define(static () => new Style { BorderWidth = 0, BorderRadius = 0, BackgroundColor = CssColors.Transparent });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        MinHeight = $"max({Sizes.Control}, {Sizes.MinTarget})",
        PaddingInlineStart = Space.Lg,
        PaddingInlineEnd = Space.Xs,
        BorderBottomWidth = BorderWidths.Xs,
        BorderBottomStyle = BorderStyle.Solid,
        BorderBottomColor = Colors.BorderMuted,
        BackgroundColor = Colors.SurfaceMuted,
        Color = Colors.TextMuted,
        FontFamily = Fonts.Mono,
        FontSize = CssLength.Em(0.85),
    });

    static Style Title => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinWidth = 0,
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
    });

    static Style Body => Css.Define(static () => new Style { Position = Position.Relative });

    // 给复制按钮留出位置，第一行的末尾不被盖住
    static Style WithCopy => Css.Define(static () => new Style
    {
        PaddingInlineEnd = $"calc({Space.Md} + max({Sizes.Control}, {Sizes.MinTarget}))",
    });

    // 不透明的底色：横向滚动的长行从按钮下面经过时，字不和图标叠在一起
    static Style Copy => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = Space.Xs,
        InsetInlineEnd = Space.Xs,
        BackgroundColor = Colors.Surface,
        [When.Hover] = new() { BackgroundColor = Colors.Fill },
    });

    static Style Pre => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Margin = 0,
        PaddingBlock = Space.Md,
        PaddingInline = Space.Lg,
        Color = Colors.Text,
        FontFamily = Fonts.Mono,
        // 代码总是从左到右排：从右到左的页面里也不跟着镜像
        Direction = Direction.Ltr,
        // 代码字体的连字（=> 显示成 ⇒）会让照着抄的人看错，一律关掉
        FontVariantLigatures = FontVariantLigatures.None,
        // 相对周围文字略小：等宽字体的字面显得大；是比例不是字号档位
        FontSize = CssLength.Em(0.9),
        LineHeight = LineHeights.Normal,
        TabSize = 2,
    });

    static Style Collapsed => Css.Define(static () => new Style
    {
        OverflowY = Overflow.Hidden,
        MaxHeight = $"calc({MaxLines} * 1lh + 2 * 0.75em)",
    });

    static Style Wrap => Css.Define(static () => new Style { WhiteSpace = WhiteSpace.PreWrap, OverflowWrap = OverflowWrap.Anywhere });

    // 继承 pre 的字体：浏览器默认给 code 设了 monospace 字号缩放
    static Style Code => Css.Define(static () => new Style { FontFamily = CssWide.Inherit, FontSize = CssWide.Inherit });

    // 按行渲染时：code 至少与 pre 一样宽，行的底色铺满横向滚动的整行
    static Style Lines => Css.Define(static () => new Style { Display = Display.InlineBlock, MinWidth = CssLength.Percent(100) });

    static Style Line => Css.Define(static () => new Style
    {
        Display = Display.Block,
        MinHeight = "1lh",
        MarginInline = $"calc(-1 * {Space.Lg})",
        PaddingInline = Space.Lg,
    });

    static Style Highlighted => Css.Define(static () => new Style { BackgroundColor = Colors.Fill });

    static Style Added => Css.Define(static () => new Style { BackgroundColor = CodeColors.Inserted });

    static Style Removed => Css.Define(static () => new Style { BackgroundColor = CodeColors.Deleted });

    static Style LineNumber => Css.Define(static () => new Style
    {
        Display = Display.InlineBlock,
        MinWidth = CssLength.Ch(2),
        MarginInlineEnd = Space.Lg,
        Color = CodeColors.Comment,
        TextAlign = TextAlign.End,
        UserSelect = UserSelect.None,
    });

    static Style Expand => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Xs,
        Width = CssLength.Percent(100),
        MinHeight = $"max({Sizes.Control}, {Sizes.MinTarget})",
        Margin = 0,
        BorderWidth = 0,
        BorderTopWidth = BorderWidths.Xs,
        BorderTopStyle = BorderStyle.Solid,
        BorderTopColor = Colors.BorderMuted,
        BackgroundColor = Colors.SurfaceMuted,
        Color = Colors.TextMuted,
        FontFamily = CssWide.Inherit,
        FontSize = FontSizes.Sm,
        Cursor = Cursor.Pointer,
        [When.Hover] = new() { BackgroundColor = Colors.Fill },
    });

    // 旋转的过渡（同 vue-tsx-pro 的 disclosureStyles.icon）
    static Style ExpandIcon => Css.Define(static () => new Style { Transition = $"rotate {Durations.Sm} {Easings.Standard}" });
}
