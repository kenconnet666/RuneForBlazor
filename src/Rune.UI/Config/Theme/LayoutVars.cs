using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 组件之间约定的布局变量（同 vue-tsx-pro 的 layoutVars）：不属于主题，由外层组件设置、里面的组件读取。
/// </summary>
public static class LayoutVars
{
    /// <summary>
    /// 页面顶部被粘性元素（AppShell 的顶栏）挡住的高度：标题的 scroll-margin、粘性侧栏的 top 据此让开，
    /// 锚点跳转后标题不会藏在顶栏下面。AppShell 设置它，默认 0。
    /// </summary>
    public static readonly CssVar<CssLength> StickyOffset = CssVar.Length("rune-sticky-offset", "0px");
}
