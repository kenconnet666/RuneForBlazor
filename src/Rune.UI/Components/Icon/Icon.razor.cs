using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>图标（svg）。</summary>
public partial class Icon : UIComponent
{
    Memo<(IconData Data, string? Label, double StrokeWidth, bool UserRole), Attrs> managed;

    /// <summary>图标数据：<c>Lucide.Plus</c>、<c>BuiltInIcons.X</c>，或自己的 <see cref="IconData"/>。</summary>
    [Parameter, EditorRequired] public IconData Data { get; set; } = default!;

    /// <summary>图标的名称（读屏读出）；不写时图标是装饰性的，对读屏隐藏。</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>字号档位；不写时跟着上下文字号。</summary>
    [Parameter] public Size? Size { get; set; }

    /// <summary>线条粗细，默认 2（Lucide 的设计值）。</summary>
    [Parameter] public double StrokeWidth { get; set; } = 2;

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
            ["stroke-width"] = k.StrokeWidth.ToString(CultureInfo.InvariantCulture),
            ["stroke-linecap"] = "round",
            ["stroke-linejoin"] = "round",
            ["focusable"] = "false",
            ["data-icon"] = k.Data.Name,
            Role = k.Label is null || k.UserRole ? null : "img",
            AriaLabel = k.Label,
            AriaHidden = k.Label is null ? true : null,
        });
}
