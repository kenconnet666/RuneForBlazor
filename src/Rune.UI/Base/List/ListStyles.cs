using Rune.Styles;

namespace Rune.UI;

/// <summary>List 交给 ListItem 的：各项默认的图标与它的部件属性（OrderedList 里是空的）。</summary>
/// <param name="Icon">各项默认的图标；没有时画圆点。</param>
/// <param name="PropsIcon">各项图标默认的部件属性。</param>
public sealed record ListMarker(IconData? Icon, Attrs? PropsIcon)
{
    /// <summary>没有图标（有序列表、圆点）。</summary>
    public static readonly ListMarker None = new(null, null);
}

/// <summary>正文列表的样式（同 vue-tsx-pro 的 listStylesProse）：缩进与项目之间的间距；有图标时不要圆点、不缩进。</summary>
internal static class ListStyles
{
    /// <summary>列表元素（ul、ol）的默认层：按间距、字号与标记（圆点、编号、图标）缓存。</summary>
    internal static Attrs Of(SpaceScale gap, Size? size, ListKind kind) =>
        Css.Define((gap, size, kind), static k => new Attrs
        {
            Sx = Sx.Of(
                Root,
                ScaleStyles.Gap(k.gap),
                k.size is { } s ? ScaleStyles.FontSize(s) : null,
                k.kind switch { ListKind.Number => Number, ListKind.Icon => Iconic, _ => Bullet }),
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.Grid, Margin = 0, PaddingInlineStart = CssLength.Em(1.5) });

    static Style Bullet => Css.Define(static () => new Style { ListStyleType = "disc" });

    static Style Number => Css.Define(static () => new Style { ListStyleType = "decimal" });

    // 图标代替圆点：图标在项里占一列，项的文字换行时对齐文字
    static Style Iconic => Css.Define(static () => new Style { ListStyleType = "none", PaddingInlineStart = 0 });

    /// <summary>项（li）。</summary>
    internal static Style Item => Css.Define(static () => new Style { PaddingInlineStart = Space.Xs });

    /// <summary>带图标的项：图标在前、文字在后。</summary>
    internal static Style IconItem => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Baseline,
        Gap = Space.Sm,
        PaddingInlineStart = 0,
    });

    /// <summary>项前的图标：与第一行文字对齐（基线对不齐图标，按行高居中）。</summary>
    internal static Style Icon => Css.Define(static () => new Style { FlexShrink = 0, AlignSelf = AlignSelf.FlexStart, MarginBlockStart = CssLength.Em(0.2) });

    /// <summary>带图标时包着文字的内容层。</summary>
    internal static Style Content => Css.Define(static () => new Style { MinWidth = 0 });
}

/// <summary>列表的标记。</summary>
internal enum ListKind
{
    /// <summary>圆点（ul）。</summary>
    Bullet,

    /// <summary>编号（ol）。</summary>
    Number,

    /// <summary>图标代替圆点（ul）。</summary>
    Icon,
}
