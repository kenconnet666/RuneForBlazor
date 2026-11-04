using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>图标（svg）。</summary>
public partial class Icon : UIComponent
{
    Memo<(IconData Data, string? Label, double? StrokeWidth, bool UserRole), Attrs> managed;

    /// <summary>图标数据：<c>Lucide.Plus</c>、<c>BuiltInIcons.X</c>，或自己的 <see cref="IconData"/>。</summary>
    [Parameter, EditorRequired] public IconData Data { get; set; } = default!;

    /// <summary>图标的名称（读屏读出）；不写时图标是装饰性的，对读屏隐藏。</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>字号档位；不写时跟着上下文字号。</summary>
    [Parameter] public Size? Size { get; set; }

    /// <summary>色调：图标用这个色调的文字色；不写时跟着文字颜色。</summary>
    [Parameter] public Tone? Tone { get; set; }

    /// <summary>线条粗细；不写时取主题的 <see cref="StrokeWidths.Md"/>（2，Lucide 的设计值）。</summary>
    [Parameter] public double? StrokeWidth { get; set; }

    /// <summary>在从右到左的语言里左右翻转（箭头、返回这类有方向的图标）。</summary>
    [Parameter] public bool FlipInRtl { get; set; }

    /// <summary>
    /// svg 上由组件管理的属性；输入不变时复用同一个对象。有名称时角色默认是 img，
    /// 使用方给了 role（加载指示用 status）就让给使用方。
    /// </summary>
    Attrs Managed => managed.Get(
        (Data, Label, StrokeWidth, AdditionalAttributes?.ContainsKey("role") == true),
        static k => new Attrs
        {
            ["viewBox"] = k.Data.ViewBox,
            ["fill"] = "none",
            ["stroke"] = "currentColor",
            // 写了粗细时用行内样式：样式里的 stroke-width 优先于 svg 的属性
            InlineStyle = k.StrokeWidth is { } width ? $"stroke-width:{width.ToString(CultureInfo.InvariantCulture)}" : null,
            ["stroke-linecap"] = "round",
            ["stroke-linejoin"] = "round",
            ["focusable"] = "false",
            ["data-icon"] = k.Data.Name,
            Role = k.Label is null || k.UserRole ? null : "img",
            AriaLabel = k.Label,
            AriaHidden = k.Label is null ? true : null,
        });
}
