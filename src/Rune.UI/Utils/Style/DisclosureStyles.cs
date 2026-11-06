using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 展开指示（箭头）的共用样式（同 vue-tsx-pro 的 disclosureStyles）：图标给 <see cref="Icon"/>（旋转的过渡），展开时再加 <see cref="Flip"/> 或 <see cref="Turn"/>。
/// 用独立的 <c>rotate</c> 属性而不是 transform：与图标自己的 <c>FlipInRtl</c>（transform 镜像）叠在一起，不互相覆盖。
/// </summary>
public static class DisclosureStyles
{
    /// <summary>旋转在 Sm 内过渡。</summary>
    public static Style Icon => Css.Define(static () => new Style { Transition = $"rotate {Durations.Sm} {Easings.Standard}" });

    /// <summary>向下的箭头展开时朝上（下拉框、折叠面板、代码块）。</summary>
    public static Style Flip => Css.Define(static () => new Style { Rotate = CssValue.Deg(180) });

    /// <summary>
    /// 朝行尾的箭头展开时朝下（导航、树、表格的子行）。图标要带 <c>FlipInRtl</c>：从右往左时图标先镜像成朝左，再反着转才朝下。
    /// </summary>
    public static Style Turn => Css.Define(static () => new Style
    {
        Rotate = CssValue.Deg(90),
        [When.Rtl] = new() { Rotate = CssValue.Deg(-90) },
    });
}
