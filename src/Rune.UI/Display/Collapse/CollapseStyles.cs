using Rune.Styles;

namespace Rune.UI;

/// <summary>折叠内容区的部件。</summary>
public enum CollapsePart
{
    /// <summary>外层（高度过渡）。</summary>
    Root,

    /// <summary>内容层（裁掉溢出）。</summary>
    Content,
}

/// <summary>折叠内容区的样式（同 vue-tsx-pro 的 collapseStyles）：grid-template-rows 0fr ↔ 1fr 做高度过渡。</summary>
public static class CollapseStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<CollapsePart> Of(ComponentStyles styles) =>
        Recipe.Of("collapse", styles, static (CollapsePart part) => part == CollapsePart.Root ? Root : Content);

    /// <summary>展开时。</summary>
    internal static Style Open => Css.Define(static () => new Style { GridTemplateRows = "1fr" });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateRows = "0fr",
        Transition = $"grid-template-rows {Durations.Md} {Easings.Standard}",
        [Media.ReducedMotion] = new() { Transition = Transition.None },
    });

    // 网格项默认的最小高度是内容高度，要设为 0 才能收到 0fr
    // 过渡中与收起时裁掉溢出（内容跟着高度出现）；完全展开后（JS 加 data-expanded）不裁，贴着边的焦点环、阴影不被裁掉
    static Style Content => Css.Define(static () => new Style
    {
        MinHeight = 0,
        Overflow = Overflow.Hidden,
        [When.Parent(When.Attribute("[data-expanded]"))] = new() { Overflow = Overflow.Visible },
    });
}
