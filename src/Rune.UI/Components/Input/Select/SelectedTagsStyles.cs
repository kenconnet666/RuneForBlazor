using Rune.Styles;

namespace Rune.UI;

/// <summary>已选标签那一排的部件（标签本身是 Tag，部件标记与定制同 Tag）。</summary>
public enum SelectedTagsPart
{
    /// <summary>这一排（span）。</summary>
    Root,
}

/// <summary>已选标签那一排的样式（同 vue-tsx-pro 的 selectedTagsStyles）。</summary>
public static class SelectedTagsStyles
{
    /// <summary>各部件的默认层；<paramref name="responsive"/> 时排成一行、放不下的由 JS 藏起来。</summary>
    public static PartLayers<SelectedTagsPart> Of(bool responsive, ComponentStyles styles) =>
        Recipe.Of("selected-tags", responsive, styles, static (bool responsive, SelectedTagsPart _) => Sx.Of(Root, responsive ? Line : null));

    // 占触发框里剩下的宽度；触发按钮里只剩读屏用的文字
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        FlexGrow = 1,
        MinWidth = 0,
        PaddingBlock = Space.Xs,
    });

    // 按宽度放：一行，放不下的移出排版（要求容器 position: relative）；
    // contain: inline-size 让这一行不按内容撑宽外壳（不然按内容宽度排版的祖先会被整行标签撑开，永远“放得下”）
    static Style Line => Css.Define(static () => new Style
    {
        FlexWrap = FlexWrap.Nowrap,
        Position = Position.Relative,
        Overflow = Overflow.Hidden,
        Contain = "inline-size",
    });

    /// <summary>每个标签：放不下的（JS 写 data-overflow-hidden）藏起来但仍可测量。</summary>
    internal static Style Tag => Css.Define(static () => new Style
    {
        [When.Attribute("[data-overflow-hidden]")] = new()
        {
            Position = Position.Absolute,
            InsetInlineStart = 0,
            InsetBlockStart = 0,
            Visibility = "hidden",
            PointerEvents = PointerEvents.None,
        },
    });
}
