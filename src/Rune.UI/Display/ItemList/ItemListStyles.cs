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
/// <param name="Interactive">整条可点（标题是链接或按钮）。</param>
/// <param name="Selected">正在看的一条：选中色的底色。</param>
/// <param name="Disabled">整条可点时不可用：变淡、点不了。</param>
public readonly record struct ItemListAppearance(ItemListLook Look, bool HasStart, bool AlignStart, bool Interactive = false, bool Selected = false, bool Disabled = false) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [Look.Density, Look.Inset];
}

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
                a.Look.Hoverable ? Rounded : null,
                // 悬停着底色、整条可点的条目不写 Inset 时两侧留 Sm：底色与焦点环不贴着内容
                (a.Look.Inset ?? (a.Look.Hoverable || a.Interactive ? SpaceScale.Sm : null)) is { } inset ? ScaleStyles.PaddingInline(inset) : null,
                a.Interactive ? Interactive : null,
                a.Selected ? Selected : null,
                a.Interactive && a.Disabled ? Disabled : null),
            ItemListPart.Start => a.AlignStart ? StartAligned : Sx.Of(),
            ItemListPart.Body => Body,
            ItemListPart.Title => Title,
            ItemListPart.Description => Description,
            _ => a.Interactive ? Sx.Of(End, Raised) : End,
        });

    /// <summary>整条可点时标题的链接或按钮：看起来就是标题，伸展盖住整条；选中时带 aria-current。</summary>
    internal static Attrs Action(bool selected) => selected ? SelectedAction : PlainAction;

    static readonly Attrs PlainAction = new() { Sx = ActionStyle, ["data-item-action"] = true };
    static readonly Attrs SelectedAction = new() { Sx = ActionStyle, ["data-item-action"] = true, ["aria-current"] = "true" };

    static Style Rounded => Css.Define(static () => new Style { BorderRadius = Radii.Sm });

    // 整条可点：悬停着底色；焦点在标题的链接或按钮上时整条画焦点环
    static Style Interactive => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        BorderRadius = Radii.Sm,
        OutlineStyle = OutlineStyle.None,
        OutlineWidth = BorderWidths.Sm,
        OutlineColor = Colors.FocusRing,
        OutlineOffset = $"calc(-1 * {BorderWidths.Sm})",
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        [When.Has("[data-item-action]:focus-visible")] = new() { OutlineStyle = OutlineStyle.Solid },
    });

    static Style Selected => Css.Define(static () => new Style
    {
        BackgroundColor = Tones.Accent.Soft,
        [When.Hover] = new() { BackgroundColor = Tones.Accent.SoftHover },
    });

    static Style Disabled => Css.Define(static () => new Style { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed });

    // 标题的链接或按钮：::after 伸展盖住整条，点哪里都等于点它；按钮为波纹定了位，改回 static，伸展层才以整条（li）为准
    static Style ActionStyle => Css.Define(static () => new Style
    {
        Position = Position.Static,
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Padding = 0,
        Color = CssWide.Inherit,
        TextAlign = TextAlign.Start,
        TextDecorationLine = TextDecorationLine.None,
        OutlineStyle = OutlineStyle.None,
        Cursor = CssWide.Inherit,
        [When.FocusVisible] = new() { OutlineStyle = OutlineStyle.None },
        [Pseudo.After] = new() { Content = "\"\"", Position = Position.Absolute, Inset = 0 },
    });

    // 结束边的操作叠在伸展的标题上面，照常可点
    static Style Raised => Css.Define(static () => new Style { Position = Position.Relative, ZIndex = 1 });

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
        Gap = Space.Xxs,
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
