using Rune.Styles;

namespace Rune.UI;

/// <summary>一级路径。</summary>
/// <param name="Label">显示的文字。</param>
/// <param name="Href">地址；最后一项（当前页）不需要。</param>
/// <param name="Icon">文字前的图标。</param>
/// <param name="Key">唯一的 key；不写时用 Href，再没有时用位置。</param>
public sealed record BreadcrumbItem(string Label, string? Href = null, IconData? Icon = null, string? Key = null);

/// <summary>面包屑的部件。</summary>
public enum BreadcrumbsPart
{
    /// <summary>列表（ol）。</summary>
    List,

    /// <summary>每一项（li）。</summary>
    Item,

    /// <summary>每一项的文字（图标加文字）。</summary>
    Label,

    /// <summary>当前页（最后一项）。</summary>
    Current,

    /// <summary>分隔符。</summary>
    Separator,

    /// <summary>折叠按钮（被折叠各级的菜单的触发器）。</summary>
    Expand,
}

/// <summary>面包屑的样式（配方，同 vue-tsx-pro 的 breadcrumbsStyles）。</summary>
public static class BreadcrumbsStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<BreadcrumbsPart> Of(ComponentStyles styles) =>
        Recipe.Of("breadcrumbs", styles, static (BreadcrumbsPart part) => part switch
        {
            BreadcrumbsPart.List => List,
            BreadcrumbsPart.Item => Item,
            BreadcrumbsPart.Label => Label,
            BreadcrumbsPart.Current => Current,
            BreadcrumbsPart.Separator => Separator,
            _ => Expand,
        });

    // 横排、换行、间距由 FlexOrderedList 给
    static Style List => Css.Define(static () => new Style { FontSize = FontSizes.Sm });

    static Style Item => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = Space.Xs, MinWidth = 0 });

    static Style Current => Css.Define(static () => new Style { Color = Colors.Text, FontWeight = FontWeights.Medium });

    static Style Separator => Css.Define(static () => new Style { Display = Display.InlineFlex, Color = Colors.TextMuted });

    static Style Label => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = Space.Xs });

    static Style Expand => Css.Define(static () => new Style { Width = CssLength.Em(1.75), MinHeight = CssLength.Em(1.75) });
}
