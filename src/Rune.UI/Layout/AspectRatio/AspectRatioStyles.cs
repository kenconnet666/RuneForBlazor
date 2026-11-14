using Rune.Styles;

namespace Rune.UI;

/// <summary>定比例的框的部件。</summary>
public enum AspectRatioPart
{
    /// <summary>外层 div。</summary>
    Root,
}

/// <summary>定比例的框的样式（同 vue-tsx-pro 的 aspectRatioStyles）。</summary>
public static class AspectRatioStyles
{
    /// <summary>某个宽高比下各部件的默认层。</summary>
    public static PartLayers<AspectRatioPart> Of(string ratio, ComponentStyles styles) =>
        Recipe.Of("aspect-ratio", ratio, styles, static (string ratio, AspectRatioPart _) => Sx.Of(Root, RatioOf(ratio)));

    /// <summary>
    /// 里面的元素铺满（基础规则，最前面的级联层，使用方的样式盖得过）：替换元素（img、video、iframe）有自己的固有尺寸，不会被网格拉伸，要写明宽高。
    /// </summary>
    internal static readonly GlobalRule Fill = new(
        """:where([data-scope="aspect-ratio"][data-part="root"]) > *""",
        new Style { Width = CssLength.Percent(100), Height = CssLength.Percent(100) });

    /// <summary>图片、视频按 cover 裁切；要完整显示时在它上面写 contain。</summary>
    internal static readonly GlobalRule Cover = new(
        """:where([data-scope="aspect-ratio"][data-part="root"]) > :where(img, video)""",
        new Style { ObjectFit = ObjectFit.Cover });

    // 网格只有一格：里面的元素拉满这一格；min 是 0，内容再大也不把框撑开
    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Grid,
        GridTemplate = "minmax(0, 1fr) / minmax(0, 1fr)",
        Width = CssLength.Percent(100),
        Overflow = Overflow.Hidden,
    });

    static Style RatioOf(string ratio) => Css.Define(ratio, static ratio => new Style { AspectRatio = ratio });
}
