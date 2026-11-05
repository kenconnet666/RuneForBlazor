using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 输入框外壳一类的控件（TextField、Textarea、NumberField、NativeSelect、Select、Combobox、TagsInput）的基类：
/// 在 <see cref="FormControl{TValue}"/> 之上统一外观参数（样式、字号档位、圆角）与所在的相连组（ControlGroup），
/// 子类用 <see cref="Appearance"/> 取外壳的外观，再按自己的需要加档位（<c>with { Picker = true }</c>）。
/// </summary>
/// <typeparam name="TValue">值的类型。</typeparam>
public abstract class InputControl<TValue> : FormControl<TValue>
{
    /// <summary>样式，默认表面色加描边。</summary>
    [Parameter] public InputVariant? Variant { get; set; }

    /// <summary>字号档位，高度与内边距随之缩放；不写时继承上下文字号。</summary>
    [Parameter] public Size? Size { get; set; }

    /// <summary>圆角，默认中圆角；搜索框常用 Full（胶囊形）。</summary>
    [Parameter] public Radius? Radius { get; set; }

    /// <summary>所在的相连组（ControlGroup）：外壳去掉相邻一侧的圆角、边框叠在一起。</summary>
    [CascadingParameter] AttachedGroup? Attached { get; set; }

    /// <summary>外壳的外观：参数 &gt; 配置的默认值（<see cref="InputDefaults"/>），加上只读与所在的相连组。</summary>
    protected InputAppearance Appearance =>
        InputAppearance.Resolve(Variant, Size, Radius, IsReadOnly, Config) with { Attached = Attached?.Orientation };
}
