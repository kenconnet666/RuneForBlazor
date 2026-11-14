using Rune.Styles;

namespace Rune.UI;

/// <summary>页头的部件。</summary>
public enum PageHeaderPart
{
    /// <summary>header 元素。</summary>
    Root,

    /// <summary>面包屑。</summary>
    Breadcrumbs,

    /// <summary>标题行（返回、标题、操作）。</summary>
    Main,

    /// <summary>返回按钮。</summary>
    Back,

    /// <summary>标题与说明的外层（hgroup）。</summary>
    Heading,

    /// <summary>标题与标记那一行。</summary>
    TitleRow,

    /// <summary>标题（H1–H3）。</summary>
    Title,

    /// <summary>标题后面的标记的外层。</summary>
    Meta,

    /// <summary>说明（Paragraph）。</summary>
    Description,

    /// <summary>操作的外层。</summary>
    Actions,
}

/// <summary>页头的样式（同 vue-tsx-pro 的 pageHeaderStyles）。</summary>
public static class PageHeaderStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<PageHeaderPart> Of(ComponentStyles styles) =>
        Recipe.Of("page-header", styles, static (PageHeaderPart part) => part switch
        {
            PageHeaderPart.Root => Sx.Of(Root),
            PageHeaderPart.Main => Main,
            PageHeaderPart.Back => Back,
            PageHeaderPart.Heading => Heading,
            PageHeaderPart.TitleRow => TitleRow,
            PageHeaderPart.Meta => Meta,
            PageHeaderPart.Actions => Actions,
            _ => Sx.Of((Style?)null),
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Md, MinWidth = 0 });

    // 标题一侧尽量宽，操作靠结束边；放不下时操作整块换到下一行
    static Style Main => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Md,
        MinWidth = 0,
    });

    static Style Back => Css.Define(static () => new Style { FlexShrink = 0 });

    static Style Heading => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Xs,
        FlexGrow = 1,
        FlexBasis = CssLength.Rem(16),
        MinWidth = 0,
        Margin = 0,
    });

    static Style TitleRow => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        ColumnGap = Space.Md,
        RowGap = Space.Xs,
    });

    static Style Meta => Css.Define(static () => new Style { Display = Display.InlineFlex, FlexWrap = FlexWrap.Wrap, AlignItems = AlignItems.Center, Gap = Space.Xs });

    static Style Actions => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        MarginInlineStart = CssLength.Auto,
    });
}
