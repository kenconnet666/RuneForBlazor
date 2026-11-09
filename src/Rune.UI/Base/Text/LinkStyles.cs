using Rune.Styles;

namespace Rune.UI;

/// <summary>链接的下划线。</summary>
public enum LinkUnderline
{
    /// <summary>一直显示（默认）：正文里的链接不能只靠颜色区分（WCAG 1.4.1）。</summary>
    Always,

    /// <summary>悬停时才显示：列表、卡片里一眼能看出是链接的地方。</summary>
    Hover,

    /// <summary>不显示：导航菜单等上下文已表明是链接的地方。</summary>
    None,
}

/// <summary>
/// 所在容器给里面的 TextLink 的默认下划线（级联值）：界面里的链接（列表的标题、卡片页头的操作、描述列表的值）一列下来都带下划线太重，
/// 这些容器给 <see cref="LinkUnderline.Hover"/>。取值：链接上显式写的 &gt; 所在容器给的 &gt; 配置的默认值 &gt; Always（同 vue-tsx-pro 的 LinkUnderlineContext）。
/// </summary>
/// <param name="Underline">下划线。</param>
public sealed record LinkUnderlineContext(LinkUnderline Underline)
{
    /// <summary>悬停才有下划线：条目列表、指标、时间线、描述列表、清单与卡片页头给的。</summary>
    public static readonly LinkUnderlineContext Hover = new(LinkUnderline.Hover);
}

/// <summary>链接的部件。</summary>
public enum LinkPart
{
    /// <summary>a 元素。</summary>
    Root,

    /// <summary>外部链接的图标。</summary>
    Icon,
}

/// <summary>链接的默认值。</summary>
public sealed record LinkDefaults : IComponentDefaults<LinkDefaults>
{
    /// <summary>色调。</summary>
    public TextTone? Tone { get; init; }

    /// <summary>下划线。</summary>
    public LinkUnderline? Underline { get; init; }

    /// <inheritdoc/>
    public LinkDefaults Over(LinkDefaults outer) => new()
    {
        Tone = Tone ?? outer.Tone,
        Underline = Underline ?? outer.Underline,
    };
}

/// <summary>链接的样式（配方）：色调 × 下划线。</summary>
public static class LinkStyles
{
    /// <summary>某个色调、下划线方式下各部件的默认层。</summary>
    // 色调排在配方前面，按部件定制能盖过它（同 vue-tsx-pro）
    public static PartLayers<LinkPart> Of(TextTone tone, LinkUnderline underline, ComponentStyles styles) =>
        Recipe.Of("link", (tone, underline), styles, static ((TextTone Tone, LinkUnderline Underline) a, LinkPart part) => part switch
        {
            LinkPart.Root => Sx.Of(SharedStyles.TextColor(a.Tone), a.Tone == TextTone.Muted ? MutedHover : null, Root, Underline(a.Underline), SharedStyles.FocusRing),
            _ => Icon,
        });

    static Style Root => Css.Define(static () => new Style
    {
        BorderRadius = Radii.Xs,
        TextDecorationThickness = BorderWidths.Xs,
        TextUnderlineOffset = CssLength.Em(0.2),
        Cursor = Cursor.Pointer,
        // 下划线平时是半透明的文字色，悬停时变成实色（同 vue-tsx-pro、Radix Themes）：一直有下划线的链接也有悬停反馈，各色调都适用
        TextDecorationColor = "color-mix(in srgb, currentColor 50%, transparent)",
        [When.Hover] = new() { TextDecorationColor = CssColors.CurrentColor },
    });

    // 次要链接（导航、页脚）悬停时换成正文色：不画下划线时也有悬停反馈
    static Style MutedHover => Css.Define(static () => new Style { [When.Hover] = new() { Color = Colors.Text } });

    static Style Underline(LinkUnderline underline) => Css.Define(underline, static underline => underline switch
    {
        LinkUnderline.Hover => new Style
        {
            TextDecorationLine = TextDecorationLine.None,
            [When.Hover] = new() { TextDecorationLine = TextDecorationLine.Underline },
        },
        LinkUnderline.None => new Style { TextDecorationLine = TextDecorationLine.None },
        _ => new Style { TextDecorationLine = TextDecorationLine.Underline },
    });

    // 图标跟着文字，贴着基线
    static Style Icon => Css.Define(static () => new Style
    {
        MarginInlineStart = CssLength.Em(0.1),
        VerticalAlign = CssLength.Em(-0.1),
    });
}
