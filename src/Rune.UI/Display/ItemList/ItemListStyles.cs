using Rune.Styles;

namespace Rune.UI;

/// <summary>条目的疏密。</summary>
public enum ItemListDensity
{
    /// <summary>标准（默认）：弹出层、页面里的列表。</summary>
    Standard,

    /// <summary>紧凑：卡片里的短列表。</summary>
    Compact,
}

/// <summary>条目列表的部件。</summary>
public enum ItemListPart
{
    /// <summary>列表（ul）。</summary>
    Root,

    /// <summary>一条（li）：起始边、内容、结束边三栏。</summary>
    Item,

    /// <summary>起始边：图标、色点、头像。</summary>
    Start,

    /// <summary>内容：标题、说明、更多内容上下排。</summary>
    Body,

    /// <summary>标题。</summary>
    Title,

    /// <summary>标题下的说明。</summary>
    Description,

    /// <summary>结束边：标记、数字、操作。</summary>
    End,
}

/// <summary>条目列表交给条目的外观（级联值）。</summary>
/// <param name="Divided">条目之间画线。</param>
/// <param name="Hoverable">悬停时着底色。</param>
/// <param name="Density">疏密。</param>
/// <param name="Inset">条目两侧的内边距。</param>
public sealed record ItemListLook(bool Divided = false, bool Hoverable = false, ItemListDensity Density = ItemListDensity.Standard, SpaceScale? Inset = null)
{
    /// <summary>不在 ItemList 里时的外观。</summary>
    public static readonly ItemListLook Default = new();
}

/// <summary>条目的外观：交给 <see cref="ItemListStyles.Of"/> 的档位组合。</summary>
/// <param name="Look">列表给的外观。</param>
/// <param name="HasStart">有起始边。</param>
/// <param name="AlignStart">起始边、结束边贴着第一行。</param>
public readonly record struct ItemListAppearance(ItemListLook Look, bool HasStart, bool AlignStart);

/// <summary>条目列表的样式（配方，同 vue-tsx-pro 的 itemListStyles）。</summary>
public static class ItemListStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<ItemListPart> Of(ItemListAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("item-list", appearance, styles, static (ItemListAppearance a, ItemListPart part) => part switch
        {
            ItemListPart.Root => List,
            ItemListPart.Item => Sx.Of(
                Item,
                a.Look.Density == ItemListDensity.Compact ? Compact : Standard,
                a.HasStart ? null : NoStart,
                a.AlignStart ? AlignStart : null,
                a.Look.Divided ? Divided : null,
                a.Look.Hoverable ? ItemStyles.Hoverable : null,
                a.Look.Inset is { } inset ? ScaleStyles.PaddingInline(inset) : null),
            ItemListPart.Start => a.AlignStart ? StartAligned : Sx.Of(),
            ItemListPart.Body => Body,
            ItemListPart.Title => Title,
            ItemListPart.Description => Description,
            _ => End,
        });

    static Style List => Css.Define(static () => new Style
    {
        ListStyle = ListStyle.None,
        Margin = 0,
        Padding = 0,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
    });

    static Style Item => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "auto minmax(0, 1fr) auto",
        ColumnGap = Space.Sm,
        AlignItems = AlignItems.Center,
    });

    static Style AlignStart => Css.Define(static () => new Style { AlignItems = AlignItems.Start });

    static Style Divided => Css.Define(static () => new Style
    {
        BorderBlockEndWidth = BorderWidths.Xs,
        BorderBlockEndStyle = BorderBlockEndStyle.Solid,
        BorderBlockEndColor = Colors.BorderMuted,
        [When.LastChild] = new() { BorderBlockEndWidth = 0 },
    });

    static Style Compact => Css.Define(static () => new Style { PaddingBlock = Space.Xs });

    static Style Standard => Css.Define(static () => new Style { PaddingBlock = Space.Sm });

    // 没有起始边时第一列不占位、不留间距
    static Style NoStart => Css.Define(static () => new Style { GridTemplateColumns = "minmax(0, 1fr) auto" });

    // 色点、小图标贴着第一行的中线
    static Style StartAligned => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, MinHeight = "1lh" });

    static Style Body => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = CssLength.Em(0.15),
        MinWidth = 0,
    });

    static Style Title => Css.Define(static () => new Style { MinWidth = 0, OverflowWrap = OverflowWrap.Anywhere });

    static Style Description => Css.Define(static () => new Style
    {
        Color = Colors.TextMuted,
        FontSize = FontSizes.Xs,
        MinWidth = 0,
        OverflowWrap = OverflowWrap.Anywhere,
    });

    static Style End => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        JustifySelf = JustifySelf.End,
    });
}
