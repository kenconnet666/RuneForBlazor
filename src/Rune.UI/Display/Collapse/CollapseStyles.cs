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

    // 展开按 data-state 取（不是另加一个类）：触发器在 JS 里切换（data-rune-disclose）时只改这个属性，C# 还没运行也能展开
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateRows = "0fr",
        Transition = $"grid-template-rows {Durations.Md} {Easings.Standard}",
        [When.Attribute("[data-state=\"open\"]")] = new() { GridTemplateRows = "1fr" },
        [Media.ReducedMotion] = new() { Transition = Transition.None },
    });

    // 网格项默认的最小高度是内容高度，要设为 0 才能收到 0fr
    // 过渡中与收起时裁掉溢出（内容跟着高度出现）；完全展开后（JS 加 data-expanded）不裁，贴着边的焦点环、阴影不被裁掉。
    // 最小宽度同样设 0：不裁时网格项的最小宽度变回内容宽度，里面一行很长的代码会把它撑宽、横向滚动条出不来（CodePreview 的源码）
    static Style Content => Css.Define(static () => new Style
    {
        MinHeight = 0,
        MinWidth = 0,
        Overflow = Overflow.Hidden,
        [When.Parent(When.Attribute("[data-expanded]"))] = new() { Overflow = Overflow.Visible },
    });
}
