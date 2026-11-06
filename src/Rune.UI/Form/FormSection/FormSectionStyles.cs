using Rune.Styles;

namespace Rune.UI;

/// <summary>FormSection 的部件。</summary>
public enum FormSectionPart
{
    /// <summary>外层（div，有标题时 role="group"）。</summary>
    Root,

    /// <summary>标题栏（div）：标题、说明与结束边的内容，下面一条细线。</summary>
    Header,

    /// <summary>标题与说明的容器（div）。</summary>
    HeaderText,

    /// <summary>标题（h2–h6）。</summary>
    Heading,

    /// <summary>可折叠时标题里的展开收起按钮（button）。</summary>
    Trigger,

    /// <summary>展开箭头（Icon 组件）。</summary>
    Chevron,

    /// <summary>说明（Paragraph 组件）。</summary>
    Description,

    /// <summary>标题栏结束边的内容（div）。</summary>
    Extra,

    /// <summary>字段的栅格（div）。</summary>
    Body,
}

/// <summary>表单分组的样式（同 vue-tsx-pro 的 formSectionStyles）。</summary>
public static class FormSectionStyles
{
    /// <summary>最多几列（写在字段栅格上）。</summary>
    public static readonly CssVar<CssValue> Columns = CssVar.Value("rune-form-section-columns");

    /// <summary>每列的最小宽度（写在字段栅格上）。</summary>
    public static readonly CssVar<CssLength> MinColumnWidth = CssVar.Length("rune-form-section-min-column");

    static readonly CssVar<CssLength> ColumnGap = CssVar.Length("rune-form-section-column-gap");

    /// <summary>某种状态、某套定制下各部件的默认层。</summary>
    /// <param name="multiColumn">字段多于一列。</param>
    /// <param name="closed">可折叠且收起着（箭头转向起始边）。</param>
    /// <param name="styles">按部件的样式定制。</param>
    public static PartLayers<FormSectionPart> Of(bool multiColumn, bool closed, ComponentStyles styles) =>
        Recipe.Of("form-section", (multiColumn, closed), styles, static ((bool MultiColumn, bool Closed) s, FormSectionPart part) => part switch
        {
            FormSectionPart.Root => Root,
            FormSectionPart.Header => Header,
            FormSectionPart.HeaderText => HeaderText,
            FormSectionPart.Heading => Heading,
            FormSectionPart.Trigger => Sx.Of(SharedStyles.ButtonReset, SharedStyles.FocusRing, Trigger),
            FormSectionPart.Chevron => Sx.Of(Chevron, DisclosureStyles.Icon, s.Closed ? ChevronClosed : null),
            FormSectionPart.Description => Description,
            FormSectionPart.Extra => Extra,
            FormSectionPart.Body => Sx.Of(Body, s.MultiColumn ? BodyColumns : null),
            _ => Sx.Of(),
        });

    // 分节导航（TableOfContents、锚点）跳过来时让开粘性顶栏，再留一点空
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Lg,
        MinWidth = 0,
        ScrollMarginBlockStart = $"calc({LayoutVars.StickyOffset} + {Space.Lg})",
    });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.FlexEnd,
        JustifyContent = JustifyContent.SpaceBetween,
        FlexWrap = FlexWrap.Wrap,
        ColumnGap = Space.Lg,
        RowGap = Space.Xs,
        PaddingBlockEnd = Space.Sm,
        BorderBlockEndWidth = BorderWidths.Xs,
        BorderBlockEndStyle = BorderStyle.Solid,
        BorderBlockEndColor = Colors.BorderMuted,
    });

    static Style HeaderText => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Xs, MinWidth = 0 });

    static Style Heading => Css.Define(static () => new Style
    {
        Margin = 0,
        FontSize = FontSizes.Md,
        FontWeight = FontWeights.Medium,
        LineHeight = LineHeights.Tight,
        Color = Colors.Text,
    });

    static Style Trigger => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        MarginInline = $"calc(-1 * {Space.Xs})",
        PaddingInline = Space.Xs,
        BorderRadius = Radii.Sm,
        FontWeight = CssWide.Inherit,
        Color = CssWide.Inherit,
        TextAlign = TextAlign.Start,
        BackgroundColor = CssColors.Transparent,
        [When.Hover] = new() { BackgroundColor = Colors.Fill },
    });

    static Style Chevron => Css.Define(static () => new Style { FlexShrink = 0, Color = Colors.TextMuted });

    // 收起时向下的箭头朝行尾：从右往左时朝左
    static Style ChevronClosed => Css.Define(static () => new Style
    {
        Rotate = CssValue.Deg(-90),
        [When.Rtl] = new() { Rotate = CssValue.Deg(90) },
    });

    static Style Description => Css.Define(static () => new Style { FontSize = FontSizes.Sm, LineHeight = LineHeights.Normal });

    static Style Extra => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Sm, FlexShrink = 0 });

    // 多列时列间距比行间距大：一眼看出哪几项是同一列；字段之间比字段内部（Xs）明显宽：一眼看出哪几行是一组
    static Style Body => Css.Define(static () => new Style
    {
        [ColumnGap.Name] = Space.Xl.ToString(),
        Display = Display.Grid,
        ColumnGap = ColumnGap,
        RowGap = Space.Lg,
        GridTemplateColumns = "minmax(0, 1fr)",
        AlignItems = AlignItems.Start,
    });

    // 最多 Columns 列，每列不窄于 MinColumnWidth：容器窄时自动减少列数，不需要断点
    static Style BodyColumns => Css.Define(static () => new Style
    {
        GridTemplateColumns = $"repeat(auto-fill, minmax(max(min({MinColumnWidth}, 100%), calc((100% - ({Columns} - 1) * {ColumnGap}) / {Columns})), 1fr))",
    });
}
