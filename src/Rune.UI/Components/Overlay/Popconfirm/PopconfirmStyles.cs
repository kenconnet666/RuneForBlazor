using Rune.Styles;

namespace Rune.UI;

/// <summary>气泡确认的部件（弹出层本身是 Popover 的 Root，属性直接写在 Popconfirm 上）。</summary>
public enum PopconfirmPart
{
    /// <summary>内容：图标、问题、说明、按钮排成网格。</summary>
    Body,

    /// <summary>图标（交给 Icon）。</summary>
    Icon,

    /// <summary>问题。</summary>
    Title,

    /// <summary>说明。</summary>
    Description,

    /// <summary>按钮行。</summary>
    Footer,
}

/// <summary>气泡确认的样式（同 vue-tsx-pro 的 popconfirmStyles）。</summary>
public static class PopconfirmStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<PopconfirmPart> Of(ComponentStyles styles) =>
        Recipe.Of("popconfirm", styles, static (PopconfirmPart part) => part switch
        {
            PopconfirmPart.Body => Body,
            PopconfirmPart.Icon => Icon,
            PopconfirmPart.Title => Title,
            PopconfirmPart.Description => Description,
            _ => Footer,
        });

    // 图标占左边一列、跨问题与说明两行
    static Style Body => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "auto minmax(0, 1fr)",
        ColumnGap = Space.Sm,
        RowGap = Space.Xs,
        MaxWidth = CssLength.Rem(20),
    });

    static Style Icon => Css.Define(static () => new Style { GridRow = "span 2", MarginBlockStart = CssLength.Em(0.15) });

    static Style Title => Css.Define(static () => new Style
    {
        GridColumn = "2",
        FontWeight = FontWeights.Semibold,
        LineHeight = LineHeights.Tight,
    });

    static Style Description => Css.Define(static () => new Style
    {
        GridColumn = "2",
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
    });

    static Style Footer => Css.Define(static () => new Style
    {
        GridColumn = "1 / -1",
        MarginBlockStart = Space.Sm,
        Display = Display.Flex,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = Space.Sm,
    });
}
