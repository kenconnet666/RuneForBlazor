using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>图标（svg）。</summary>
public partial class Icon : UIComponent
{
    Attrs? managed;
    (IconData, string?, double) managedFor;

    /// <summary>图标数据：<c>Lucide.Plus</c>、<c>BuiltInIcons.X</c>，或自己的 <see cref="IconData"/>。</summary>
    [Parameter, EditorRequired] public IconData Data { get; set; } = default!;

    /// <summary>图标的名称（读屏读出）；不写时图标是装饰性的，对读屏隐藏。</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>字号档位；不写时跟着上下文字号。</summary>
    [Parameter] public Size? Size { get; set; }

    /// <summary>线条粗细，默认 2（Lucide 的设计值）。</summary>
    [Parameter] public double StrokeWidth { get; set; } = 2;

    /// <summary>svg 上由组件管理的属性；图标、名称、粗细不变时复用同一个对象。</summary>
    Attrs Managed
    {
        get
        {
            var inputs = (Data, Label, StrokeWidth);
            if (managed is not null && managedFor == inputs) return managed;
            managedFor = inputs;
            return managed = new Attrs
            {
                ["viewBox"] = Data.ViewBox,
                ["fill"] = "none",
                ["stroke"] = "currentColor",
                ["stroke-width"] = StrokeWidth.ToString(CultureInfo.InvariantCulture),
                ["stroke-linecap"] = "round",
                ["stroke-linejoin"] = "round",
                ["focusable"] = "false",
                ["data-icon"] = Data.Name,
                Role = Label is null ? null : "img",
                AriaLabel = Label,
                AriaHidden = Label is null ? true : null,
            };
        }
    }
}
