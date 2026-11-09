using Rune.Styles;

namespace Rune.UI;

/// <summary>树的部件（同 vue-tsx-pro 的 Tree）。</summary>
public enum TreePart
{
    /// <summary>树（div，role="tree"），也是 ScrollArea 的视口：焦点与 aria-activedescendant 在它上面。</summary>
    Root,

    /// <summary>一项（div，role="treeitem"）。</summary>
    Item,

    /// <summary>展开箭头（只是外观，键盘与读屏走 treeitem）。</summary>
    Toggle,

    /// <summary>勾选框（只是外观，状态在 treeitem 的 aria-checked 上）。</summary>
    Checkbox,

    /// <summary>文字。</summary>
    Label,

    /// <summary>加载中的骨架行。</summary>
    Skeleton,
}

/// <summary>一项的外观档位：交给 <see cref="TreeStyles.Of"/>。</summary>
/// <param name="Selected">已选。</param>
/// <param name="Matched">搜索时匹配的节点（加粗）。</param>
public readonly record struct TreeItemLook(bool Selected = false, bool Matched = false) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [];
}

/// <summary>
/// 树的样式（配方，同 vue-tsx-pro 的 treeStyles）：一行用列表行的共用样式（ItemStyles），键盘高亮由 JS 写 data-highlighted；
/// 拖动时被拖的行（data-dragging）与落点（data-drop：before、after、inside）也由 JS 写在行上。
/// </summary>
public static class TreeStyles
{
    /// <summary>某一项的外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<TreePart> Of(TreeItemLook look, ComponentStyles styles) =>
        Recipe.Of("tree", look, styles, static (TreeItemLook a, TreePart part) => part switch
        {
            // 树一般在滚动区域、穿梭框里：焦点环画在里面
            TreePart.Root => Sx.Of(Root, SharedStyles.FocusRingInset),
            TreePart.Item => Sx.Of(ItemStyles.Row, ItemStyles.Hoverable, Item, ItemStyles.Highlighted, a.Selected ? ItemStyles.Current : null, ItemStyles.Disabled),
            TreePart.Toggle => Toggle,
            TreePart.Checkbox => Sx.Of(SharedStyles.ToneFill(Tone.Accent), Checkbox),
            TreePart.Label => a.Matched ? Sx.Of(ItemStyles.Label, Matched) : ItemStyles.Label,
            _ => Skeleton,
        });

    /// <summary>缩进：每一层的宽度（em）乘层级，写在行上给缩进占位与落点线用。</summary>
    internal static readonly CssVar<CssLength> Indent = CssVar.Length("rune-tree-indent", 0);

    /// <summary>缩进占位（一层一个宽度）。</summary>
    internal static Style IndentSpacer => Css.Define(static () => new Style { Width = Indent.Reference, FlexShrink = 0 });

    // 树元素就是 ScrollArea 的视口（浮层滚动条）；焦点与 aria-activedescendant 在它上面
    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        OverscrollBehavior = OverscrollBehavior.Contain,
        Padding = Space.Xs,
        OutlineStyle = OutlineStyle.None,
    });

    static Style Item => Css.Define(static () => new Style
    {
        // 画落点线时作定位参照
        Position = Position.Relative,
        Gap = Space.Xs,
        MinHeight = Sizes.MinTarget,
        // 行与行之间留一道缝：相邻的已选、高亮底色不连成一片
        MarginBlock = BorderWidths.Xs,
        PaddingInlineStart = Space.Xs,
        // 拖动：被拖的那一项变淡；放进某一项里时它带品牌色的框；放在前后时画一条线（从这一层的缩进处开始）
        [When.Attribute("[data-dragging]")] = new() { Opacity = 0.5 },
        [When.Attribute("[data-drop]")] = new()
        {
            [Pseudo.After] = new()
            {
                Content = "\"\"",
                Position = Position.Absolute,
                InsetInlineStart = $"calc({Indent.Reference} + 1.75em)",
                InsetInlineEnd = 0,
                Height = BorderWidths.Sm,
                BackgroundColor = Tones.Accent.Solid,
                BorderRadius = Radii.Full,
                PointerEvents = PointerEvents.None,
            },
        },
        [When.Attribute("[data-drop=inside]")] = new()
        {
            OutlineStyle = OutlineStyle.Solid,
            OutlineWidth = BorderWidths.Sm,
            OutlineColor = Tones.Accent.Solid,
            OutlineOffset = $"calc(-1 * {BorderWidths.Sm})",
            BackgroundColor = Tones.Accent.Soft,
            [Pseudo.After] = new() { Content = Content.None },
        },
        [When.Attribute("[data-drop=before]")] = new() { [Pseudo.After] = new() { Top = $"calc(-1 * {BorderWidths.Sm})" } },
        [When.Attribute("[data-drop=after]")] = new() { [Pseudo.After] = new() { Bottom = $"calc(-1 * {BorderWidths.Sm})" } },
    });

    static Style Toggle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        FlexShrink = 0,
        Width = CssLength.Em(1.5),
        Height = CssLength.Em(1.5),
        Color = Colors.TextMuted,
    });

    static Style Checkbox => Css.Define(static () => new Style { MarginInlineEnd = Space.Xs });

    static Style Matched => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold });

    static Style Skeleton => Css.Define(static () => new Style { Display = Display.Block, Height = CssLength.Em(1), MarginBlock = CssLength.Em(0.625), MarginInline = Space.Sm });
}
