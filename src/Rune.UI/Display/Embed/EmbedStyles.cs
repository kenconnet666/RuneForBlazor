using Rune.Styles;

namespace Rune.UI;

/// <summary>嵌入的页面的部件。</summary>
public enum EmbedPart
{
    /// <summary>外层（AspectRatio 的 div）。</summary>
    Root,

    /// <summary>iframe。</summary>
    Frame,

    /// <summary>加载中占位的骨架。</summary>
    Placeholder,
}

/// <summary>嵌入的页面的样式（同 vue-tsx-pro 的 embedStyles）。加载完没有由外层的 data-state 表示，JS 在 iframe 的 load 时改它。</summary>
public static class EmbedStyles
{
    static readonly Condition Loading = When.Parent(When.Attribute("[data-state=\"loading\"]"));
    static readonly Condition Loaded = When.Parent(When.Attribute("[data-state=\"loaded\"]"));

    /// <summary>各部件的默认层。</summary>
    public static PartLayers<EmbedPart> Of(ComponentStyles styles) =>
        Recipe.Of("embed", styles, static (EmbedPart part) => part switch
        {
            EmbedPart.Frame => Frame,
            EmbedPart.Placeholder => Placeholder,
            _ => Root,
        });

    static Style Root => Css.Define(static () => new Style
    {
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Surface,
    });

    // 铺满外层（iframe 有自己的固有尺寸，不会被网格拉伸）。加载完之前先不显示：不少页面加载中是一片白，暗色主题里很刺眼
    static Style Frame => Css.Define(static () => new Style
    {
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        BorderWidth = 0,
        [Loading] = new() { Opacity = 0 },
    });

    // 骨架叠在 iframe 下面（外层只有一格，iframe 占着它）
    static Style Placeholder => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        BorderRadius = 0,
        [Loaded] = new() { Display = Display.None },
    });
}
