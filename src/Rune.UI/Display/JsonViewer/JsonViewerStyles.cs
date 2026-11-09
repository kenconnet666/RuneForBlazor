using System.Text.Json;
using Rune.Styles;

namespace Rune.UI;

/// <summary>JSON 查看器的部件（同 vue-tsx-pro 的 JsonViewer）。</summary>
public enum JsonViewerPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>搜索框与“全部展开 / 收起”那一行。</summary>
    Toolbar,

    /// <summary>树（Tree）。</summary>
    Tree,

    /// <summary>树的一行：键名、值、收起时的项数。</summary>
    Row,

    /// <summary>键名（数组的序号淡一些）。</summary>
    Name,

    /// <summary>值（按类型着色）。</summary>
    Value,

    /// <summary>对象、数组收起时的项数。</summary>
    Summary,

    /// <summary>路径栏：选中一项后的路径与复制按钮。</summary>
    PathBar,

    /// <summary>路径。</summary>
    Path,
}

/// <summary>JSON 查看器的样式（同 vue-tsx-pro 的 jsonViewerStyles）：值按类型取代码高亮的颜色。</summary>
public static class JsonViewerStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<JsonViewerPart> Of(ComponentStyles styles) =>
        Recipe.Of("json-viewer", styles, static (JsonViewerPart part) => part switch
        {
            JsonViewerPart.Root => Root,
            JsonViewerPart.Toolbar => Toolbar,
            JsonViewerPart.Tree => Tree,
            JsonViewerPart.Row => Row,
            JsonViewerPart.Name => Name,
            JsonViewerPart.Value => Value,
            JsonViewerPart.Summary => Muted,
            JsonViewerPart.PathBar => PathBar,
            _ => Path,
        });

    internal static readonly Attrs Search = new() { Sx = SearchStyle };
    internal static readonly Attrs Index = new() { Sx = IndexStyle };

    /// <summary>值按类型的颜色。</summary>
    internal static Attrs ValueKind(JsonValueKind kind) => kind switch
    {
        JsonValueKind.String => StringLayer,
        JsonValueKind.Number => NumberLayer,
        JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null => KeywordLayer,
        _ => MutedLayer,
    };

    static readonly Attrs StringLayer = new() { Sx = Css.Define(static () => new Style { Color = CodeColors.String }) };
    static readonly Attrs NumberLayer = new() { Sx = Css.Define(static () => new Style { Color = CodeColors.Number }) };
    static readonly Attrs KeywordLayer = new() { Sx = Css.Define(static () => new Style { Color = CodeColors.Keyword }) };
    static readonly Attrs MutedLayer = new() { Sx = Muted };

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Xs, MinWidth = 0 });

    static Style Toolbar => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Xs, FlexWrap = FlexWrap.Wrap });

    static Style SearchStyle => Css.Define(static () => new Style { Flex = "1 1 12em", MinWidth = 0 });

    // 等宽字体的连字会把 >= 画成 ≥：看原始数据时关掉
    static Style Tree => Css.Define(static () => new Style { FontFamily = Fonts.Mono, FontSize = FontSizes.Sm, FontVariantLigatures = FontVariantLigatures.None });

    static Style Row => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Baseline,
        Gap = CssLength.Ch(0.5),
        MinWidth = 0,
        WhiteSpace = WhiteSpace.Nowrap,
    });

    static Style Name => Css.Define(static () => new Style { Color = CodeColors.Attribute, FlexShrink = 0 });

    static Style IndexStyle => Css.Define(static () => new Style { Color = CodeColors.Comment });

    static Style Value => Css.Define(static () => new Style { Overflow = Overflow.Hidden, TextOverflow = TextOverflow.Ellipsis, MinWidth = 0 });

    static Style Muted => Css.Define(static () => new Style { Color = CodeColors.Comment, FlexShrink = 0 });

    static Style PathBar => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        MinHeight = CssLength.Em(2.25),
        PaddingInline = Space.Xs,
        FontSize = FontSizes.Sm,
        Color = Colors.TextMuted,
    });

    static Style Path => Css.Define(static () => new Style
    {
        FontFamily = Fonts.Mono,
        Color = Colors.Text,
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
        MinWidth = 0,
    });
}
