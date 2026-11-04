namespace Rune.Styles;

/// <summary>
/// 规则在类里的书写顺序（后写的覆盖先写的）。沿用 StyleX 的优先级（@stylexjs/shared 0.19.1，经 vue-tsx-pro 的 priority.ts）：
/// <list type="bullet">
/// <item><description>属性：简写的简写 1000 &lt; 简写 2000 &lt; 长属性 3000 &lt; 物理长属性 4000，所以 <c>padding</c> 不会盖掉同一块里的 <c>padding-top</c>；</description></item>
/// <item><description>条件：@supports 30、@media 200、@container 300，伪类按表（:hover 130 &lt; :focus 150 &lt; :active 170），伪元素 5000；
/// 嵌套的条件相加。无条件的块最先写，所以 @media、:hover 能覆盖基础样式。</description></item>
/// </list>
/// </summary>
internal static class StylePriority
{
    static readonly HashSet<string> shorthandsOfShorthands = Words(
        "animation background border border-block border-inline margin padding font grid grid-template grid-area all inset scroll-margin scroll-padding");

    static readonly HashSet<string> shorthandsOfLonghands = Words(
        "animation-range scroll-timeline view-timeline background-position border-color border-style border-width border-block-start border-top " +
        "border-block-end border-bottom border-inline-color border-inline-style border-inline-width border-inline-start border-left border-inline-end " +
        "border-right border-image border-radius corner-shape caret outline grid-gap gap place-content place-items place-self margin-block margin-inline " +
        "overscroll-behavior padding-block padding-inline columns column-rule contain-intrinsic-size container flex flex-flow font-variant " +
        "grid-template-areas grid-row grid-column list-style mask mask-border offset overflow inset-block inset-inline scroll-margin-block " +
        "scroll-margin-inline scroll-padding-block scroll-padding-inline scroll-snap-type text-decoration text-emphasis transition");

    static readonly HashSet<string> physicalLonghands = Words(
        "border-top-color border-top-style border-top-width border-bottom-color border-bottom-style border-bottom-width border-left-color " +
        "border-left-style border-left-width border-right-color border-right-style border-right-width border-top-left-radius border-top-right-radius " +
        "border-bottom-left-radius border-bottom-right-radius height width max-height max-width min-height min-width margin-top margin-bottom " +
        "margin-left margin-right overscroll-behavior-y overscroll-behavior-x padding-top padding-bottom padding-left padding-right overflow-y " +
        "overflow-x top bottom left right scroll-margin-top scroll-margin-bottom scroll-margin-left scroll-margin-right scroll-padding-top " +
        "scroll-padding-bottom scroll-padding-left scroll-padding-right");

    /// <summary>伪类的优先级；表外的（含 :is、:where 等）为 40。</summary>
    static readonly Dictionary<string, int> pseudoClasses = new()
    {
        [":is"] = 40, [":where"] = 40, [":not"] = 40, [":has"] = 45, [":dir"] = 50, [":lang"] = 51,
        [":first-child"] = 52, [":first-of-type"] = 53, [":last-child"] = 54, [":last-of-type"] = 55,
        [":only-child"] = 56, [":only-of-type"] = 57, [":nth-child"] = 60, [":nth-last-child"] = 61,
        [":nth-of-type"] = 62, [":nth-last-of-type"] = 63, [":empty"] = 70, [":link"] = 80, [":any-link"] = 81,
        [":target"] = 84, [":visited"] = 85, [":enabled"] = 91, [":disabled"] = 92, [":required"] = 93,
        [":optional"] = 94, [":read-only"] = 95, [":read-write"] = 96, [":placeholder-shown"] = 97,
        [":in-range"] = 98, [":out-of-range"] = 99, [":default"] = 100, [":checked"] = 101, [":indeterminate"] = 101,
        [":valid"] = 103, [":invalid"] = 104, [":user-invalid"] = 105, [":autofill"] = 110, [":modal"] = 121,
        [":fullscreen"] = 122, [":hover"] = 130, [":focus-within"] = 140, [":focus"] = 150,
        [":focus-visible"] = 160, [":active"] = 170,
    };

    const int PseudoElement = 5000;

    /// <summary>属性名（连字符写法）的优先级。</summary>
    public static int OfProperty(string property)
    {
        if (property.StartsWith("--", StringComparison.Ordinal)) return 1;
        if (shorthandsOfShorthands.Contains(property)) return 1000;
        if (shorthandsOfLonghands.Contains(property)) return 2000;
        return physicalLonghands.Contains(property) ? 4000 : 3000;
    }

    /// <summary>一个条件的优先级。</summary>
    public static int OfCondition(string condition)
    {
        if (condition.StartsWith("@supports", StringComparison.Ordinal)) return 30;
        if (condition.StartsWith("@media", StringComparison.Ordinal)) return 200;
        if (condition.StartsWith("@container", StringComparison.Ordinal)) return 300;
        if (condition.StartsWith("::", StringComparison.Ordinal)) return PseudoElement;
        // :is([aria-selected="true"]) 取括号前的名字查表
        var name = condition.Split('(')[0];
        return pseudoClasses.GetValueOrDefault(name, 40);
    }

    static HashSet<string> Words(string text) => [.. text.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
}
