using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// SVG 的 text 元素：Razor 的代码块里 &lt;text&gt; 是保留的标签（输出纯文字用），刻度、标签这类 SVG 文字经这里画。
/// </summary>
internal static class ChartSvg
{
    /// <summary>一段 SVG 文字：位置、对齐（text-anchor、dominant-baseline，null 时不写）与样式层；key 不为 null 时作为渲染的键。</summary>
    public static RenderFragment Text(string text, double x, double y, Attrs layer, string? anchor = null, string? baseline = null, object? key = null) => builder =>
    {
        builder.OpenElement(0, "text");
        if (key is not null) builder.SetKey(key);
        builder.AddAttribute(1, "x", SvgFormat.Number(x));
        builder.AddAttribute(2, "y", SvgFormat.Number(y));
        if (anchor is not null) builder.AddAttribute(3, "text-anchor", anchor);
        if (baseline is not null) builder.AddAttribute(4, "dominant-baseline", baseline);
        builder.AddMultipleAttributes(5, Attrs.Merge(layer));
        builder.AddContent(6, text);
        builder.CloseElement();
    };
}
