using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Rune.UI;

/// <summary>
/// 带值的表单元素（input、textarea）：像 <c>@bind</c> 那样告诉渲染器“这个事件会改元素的 value / checked”。
/// 登记了，派发 oninput / onchange 时 Blazor 先把渲染树里的值换成浏览器里的当前值，再执行处理器；
/// 不登记，处理上一个按键后渲染出的值会与旧树比出差异、写回浏览器，盖掉用户之后打的字（Server 模式下快速输入丢字，E2E 发现）。
/// 组件用 Attrs 展开属性、写不了 <c>@bind</c>，所以经这里渲染：<c>@ValueElement.Input(Type, Attrs.Merge(...))</c>。
/// 只用在随输入（oninput）改值的输入框上；只在 change 时改值的（失焦一次）、勾选类的离散点击没有这个问题。
/// </summary>
internal static class ValueElement
{
    /// <summary>渲染 input：值属性是 value；改值的事件有 oninput 时是它，否则是 onchange。</summary>
    /// <param name="type">input 的 type；null 时是文本。</param>
    /// <param name="attributes">其余属性。</param>
    public static RenderFragment Input(string? type, Attrs attributes) => builder => Build(builder, type, attributes);

    static void Build(RenderTreeBuilder builder, string? type, Attrs attributes)
    {
        var pairs = attributes.ToList();
        var updatingEvent = pairs.Exists(static p => p.Key == "oninput") ? "oninput" : "onchange";

        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "type", type);
        foreach (var (name, value) in pairs)
        {
            builder.AddAttribute(2, name, value);
            // 登记要紧跟在处理器那一帧后面；空的 EventCallback 不会添加帧，这时不登记
            if (name == updatingEvent && HasHandler(value)) builder.SetUpdatesAttributeName("value");
        }
        builder.CloseElement();
    }

    static bool HasHandler(object? value) => value switch
    {
        Delegate => true,
        EventCallback callback => callback.HasDelegate,
        EventCallback<ChangeEventArgs> callback => callback.HasDelegate,
        _ => false,
    };
}
