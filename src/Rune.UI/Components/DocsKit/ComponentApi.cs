namespace Rune.UI;

/// <summary>参数的种类（同 vue-tsx-pro 的 ApiPropKinds）：文档里分开列出。</summary>
public enum ApiPropKind
{
    /// <summary>普通参数：外观、状态、数据。</summary>
    Prop,

    /// <summary>部件属性（PropsXxx）：交给内部的某个部件。</summary>
    Part,

    /// <summary>事件（EventCallback，含双向绑定的 XxxChanged）。</summary>
    Event,

    /// <summary>内容：ChildContent 与其他 RenderFragment。</summary>
    Content,
}

/// <summary>档位类参数（枚举）的一个可选值。</summary>
/// <param name="Value">取值。</param>
/// <param name="Description">这个值的含义；没有注释时为空。</param>
public sealed record ApiValue(string Value, string Description);

/// <summary>一个参数的文档数据（同 vue-tsx-pro 的 ApiProp）。</summary>
/// <param name="Name">名称。</param>
/// <param name="Kind">种类。</param>
/// <param name="Type">类型（C# 的写法，如 <c>ButtonVariant?</c>）。</param>
/// <param name="Description">说明。</param>
public sealed record ApiProp(string Name, ApiPropKind Kind, string Type, string Description)
{
    /// <summary>必填（[EditorRequired]）。</summary>
    public bool Required { get; init; }

    /// <summary>默认值（C# 的写法，如 <c>true</c>、<c>ButtonVariant.Solid</c>）；没有时为 null，不显示。</summary>
    public string? Default { get; init; }

    /// <summary>能用 <c>@bind-Xxx</c> 双向绑定（有对应的 XxxChanged）。</summary>
    public bool Bindable { get; init; }

    /// <summary>档位类（枚举）的可选值与各自的含义；不是档位时为 null。</summary>
    public IReadOnlyList<ApiValue>? Values { get; init; }
}

/// <summary>一个组件的 API 文档数据（同 vue-tsx-pro 的 ComponentApi）。</summary>
/// <param name="Component">组件名。</param>
/// <param name="Description">组件的说明（如“未声明的属性落到 button 上”）。</param>
/// <param name="Props">各个参数。</param>
public sealed record ComponentApi(string Component, string Description, IReadOnlyList<ApiProp> Props);
