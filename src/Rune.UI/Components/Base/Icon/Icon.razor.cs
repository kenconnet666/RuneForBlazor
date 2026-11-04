using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>图标（svg）。</summary>
public partial class Icon : UIComponent
{
    Memo<(IconData Data, string? Label, double? Pixels, double? StrokeWidth, bool? Draw, bool UserRole), Attrs> managed;

    /// <summary>图标数据：<c>Lucide.Plus</c>、<c>BuiltInIcons.X</c>，或自己的 <see cref="IconData"/>。</summary>
    [Parameter, EditorRequired] public IconData Data { get; set; } = default!;

    /// <summary>图标的名称（读屏读出）；不写时图标是装饰性的，对读屏隐藏。</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>大小：字号档位（<c>Size.Xl</c>）或像素（<c>40</c>）；不写时是 1em，跟着所在文字。</summary>
    [Parameter] public IconSize? Size { get; set; }

    /// <summary>色调：图标用这个色调的文字色；不写时跟着文字颜色。</summary>
    [Parameter] public TextTone? Tone { get; set; }

    /// <summary>线条粗细；不写时取主题的 <see cref="StrokeWidths.Md"/>（2，Lucide 的设计值）。</summary>
    [Parameter] public double? StrokeWidth { get; set; }

    /// <summary>在从右到左的语言里左右翻转（箭头、返回这类有方向的图标）。</summary>
    [Parameter] public bool FlipInRtl { get; set; }

    /// <summary>
    /// 描线动画（一笔画出）：第一次渲染时为 true 就画一次，之后每次由 false 变为 true 再画（JS 初始化器做，时长取主题，
    /// 减少动态效果时直接画好）。不写的图标没有动画。
    /// </summary>
    [Parameter] public bool? Draw { get; set; }

    /// <summary>
    /// svg 上由组件管理的属性；输入不变时复用同一个对象。有名称时角色默认是 img，
    /// 使用方给了 role（加载指示用 status）就让给使用方。
    /// </summary>
    Attrs Managed => managed.Get(
        (Data, Label, Size?.Pixels, StrokeWidth, Draw, AdditionalAttributes?.ContainsKey("role") == true),
        static k => new Attrs
        {
            ["viewBox"] = k.Data.ViewBox,
            ["fill"] = "none",
            ["stroke"] = "currentColor",
            // 像素大小、线条粗细用行内样式（样式里的 stroke-width 优先于 svg 的属性）
            InlineStyle = InlineStyleOf(k.Pixels, k.StrokeWidth),
            ["stroke-linecap"] = "round",
            ["stroke-linejoin"] = "round",
            ["focusable"] = "false",
            ["data-icon"] = k.Data.Name,
            Role = k.Label is null || k.UserRole ? null : "img",
            AriaLabel = k.Label,
            AriaHidden = k.Label is null ? true : null,
            ["data-rune-draw"] = k.Draw switch { true => "true", false => "false", null => null },
        });

    static string? InlineStyleOf(double? pixels, double? strokeWidth)
    {
        var parts = new List<string>(2);
        if (pixels is { } px) parts.Add($"font-size:{px.ToString(CultureInfo.InvariantCulture)}px");
        if (strokeWidth is { } width) parts.Add($"stroke-width:{width.ToString(CultureInfo.InvariantCulture)}");
        return parts.Count == 0 ? null : string.Join(';', parts);
    }
}
