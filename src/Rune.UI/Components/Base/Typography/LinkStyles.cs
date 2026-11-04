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

/// <summary>链接的部件。</summary>
public enum LinkPart
{
    /// <summary>a 元素。</summary>
    Root,

    /// <summary>外部链接的图标。</summary>
    Icon,

    /// <summary>外部链接给读屏的提示（视觉隐藏）。</summary>
    Hint,
}

/// <summary>链接的默认值。</summary>
public sealed record LinkDefaults : IComponentDefaults<LinkDefaults>
{
    /// <summary>色调。</summary>
    public Tone? Tone { get; init; }

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
    public static PartLayers<LinkPart> Of(Tone tone, LinkUnderline underline, ComponentStyles styles) =>
        Recipe.Of("link", (tone, underline), styles, static ((Tone Tone, LinkUnderline Underline) a, LinkPart part) => part switch
        {
            LinkPart.Root => Sx.Of(Root, SharedStyles.ToneText(a.Tone), Underline(a.Underline), SharedStyles.FocusRing),
            LinkPart.Icon => Icon,
            _ => SharedStyles.VisuallyHidden,
        });

    static Style Root => Css.Define(static () => new Style
    {
        BorderRadius = Radii.Sm,
        TextDecorationThickness = 1,
        TextUnderlineOffset = "0.2em",
        Cursor = Cursor.Pointer,
        [When.Hover] = new() { TextDecorationThickness = 2 },
    });


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

    // 外链图标比文字略小，贴着文字的基线
    static Style Icon => Css.Define(static () => new Style
    {
        FontSize = "0.85em",
        MarginInlineStart = "0.15em",
        VerticalAlign = "-0.05em",
    });
}
