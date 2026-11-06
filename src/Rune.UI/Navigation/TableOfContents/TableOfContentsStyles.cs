using Rune.Styles;

namespace Rune.UI;

/// <summary>本页目录的部件。</summary>
public enum TableOfContentsPart
{
    /// <summary>导航（nav）。</summary>
    Root,

    /// <summary>标题（TextSpan 组件），同时是导航的名称。</summary>
    Title,

    /// <summary>列表（ul，FlexList 组件）：指示条的定位容器。</summary>
    List,

    /// <summary>跟着当前节走的指示条（span）。</summary>
    Indicator,

    /// <summary>链接（a）。</summary>
    Link,
}

/// <summary>目录里的一项：标题的 id、文字与级别（2 是 h2，3 是 h3……）。</summary>
/// <param name="Id">标题的 id（锚点）。</param>
/// <param name="Label">标题文字。</param>
/// <param name="Level">标题级别，2 级起，每深一级多缩进一档。</param>
public sealed record HeadingEntry(string Id, string Label, int Level = 2);

/// <summary>本页目录的样式（同 vue-tsx-pro 的 tableOfContentsStyles）。</summary>
public static class TableOfContentsStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<TableOfContentsPart> Of(ComponentStyles styles) =>
        Recipe.Of("table-of-contents", styles, static (TableOfContentsPart part) => part switch
        {
            TableOfContentsPart.Title => Title,
            TableOfContentsPart.List => List,
            TableOfContentsPart.Indicator => Indicator,
            TableOfContentsPart.Link => Sx.Of(Link, SharedStyles.FocusRing),
            _ => Sx.Of(),
        });

    /// <summary>按级别缩进：2 级贴着细线留一档，往下每级多一档。</summary>
    internal static Style Indent(int level) => Css.Define(Math.Max(level - 1, 1), static steps => new Style
    {
        PaddingInlineStart = CssLength.Em(steps * 0.75),
    });

    static Style Title => Css.Define(static () => new Style { Display = Display.Block, MarginBlockEnd = Space.Sm });

    // 指示条按链接的 offsetTop 定位：列表是它的定位容器
    static Style List => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        BorderInlineStartWidth = BorderWidths.Xs,
        BorderInlineStartStyle = BorderStyle.Solid,
        BorderInlineStartColor = Colors.Border,
    });

    // 盖住列表起始边的细线；位置与高度由 JS 写（没有当前节时高度是 0，看不见）
    static Style Indicator => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetBlockStart = 0,
        InsetInlineStart = 0,
        MarginInlineStart = $"calc(-1 * {BorderWidths.Xs})",
        Width = BorderWidths.Sm,
        BorderRadius = Radii.Full,
        BackgroundColor = Tones.Accent.Solid,
        Transition = $"transform {Durations.Md} {Easings.Standard}, height {Durations.Md} {Easings.Standard}",
    });

    static Style Link => Css.Define(static () => new Style
    {
        Display = Display.Block,
        PaddingBlock = Space.Xs,
        PaddingInlineEnd = Space.Xs,
        BorderRadius = Radii.Xs,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
        LineHeight = LineHeights.Snug,
        TextDecorationLine = TextDecorationLine.None,
        Transition = $"color {Durations.Sm} {Easings.Standard}",
        [When.Hover] = new() { Color = Colors.Text },
        [When.Current] = new() { Color = Tones.Accent.Text },
    });
}
