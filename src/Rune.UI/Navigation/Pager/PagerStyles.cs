using Rune.Styles;

namespace Rune.UI;

/// <summary>翻页导航的部件。</summary>
public enum PagerPart
{
    /// <summary>导航（nav）。</summary>
    Root,

    /// <summary>链接卡片（a）。</summary>
    Link,

    /// <summary>“上一篇 / 下一篇”与箭头（span）。</summary>
    Hint,

    /// <summary>文章标题（span）。</summary>
    Label,

    /// <summary>箭头（Icon 组件）。</summary>
    Arrow,
}

/// <summary>翻页导航一侧的链接。</summary>
/// <param name="Href">地址。</param>
/// <param name="Label">文章标题。</param>
public sealed record PagerLink(string Href, string Label);

/// <summary>翻页导航的样式（同 vue-tsx-pro 的 pagerStyles）。</summary>
public static class PagerStyles
{
    /// <summary>某一侧（下一篇时靠结束边）、某套定制下各部件的默认层。</summary>
    public static PartLayers<PagerPart> Of(bool next, ComponentStyles styles) =>
        Recipe.Of("pager", next, styles, static (bool next, PagerPart part) => part switch
        {
            PagerPart.Root => Root,
            PagerPart.Link => Sx.Of(Link, next ? Next : null, SharedStyles.FocusRing, SharedStyles.InteractionColors),
            PagerPart.Hint => Hint,
            PagerPart.Label => Label,
            _ => Arrow,
        });

    // 够宽时两列；只有下一篇时它仍在结束边那一列
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "repeat(auto-fit, minmax(min(100%, 14rem), 1fr))",
        Gap = Space.Md,
    });

    static Style Link => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Xs,
        Padding = Space.Md,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Md,
        Color = Colors.Text,
        TextDecorationLine = TextDecorationLine.None,
        [When.Hover] = new() { BorderColor = Tones.Accent.Solid },
    });

    static Style Next => Css.Define(static () => new Style { GridColumnEnd = "-1", AlignItems = AlignItems.FlexEnd, TextAlign = TextAlign.End });

    static Style Hint => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
    });

    static Style Label => Css.Define(static () => new Style { Color = Tones.Accent.Text, FontWeight = FontWeights.Medium });

    // 从右到左排版时“上一篇”在右边：箭头跟着掉头
    static Style Arrow => Css.Define(static () => new Style { [When.Rtl] = new() { Scale = "-1 1" } });
}
