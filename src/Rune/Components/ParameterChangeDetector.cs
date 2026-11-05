using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune;

/// <summary>
/// 判断父级这次传来的参数和组件现有的是否一样，一样就跳过渲染。
/// </summary>
/// <remarks>
/// Blazor 自己只认基本类型：只要有一个参数是对象（RenderFragment、回调、样式），子组件就跟着父级重新渲染。
/// 这里多认几类“比较结果可信”的值：
/// <list type="bullet">
/// <item><description>回调（Action、Func 等委托）：目标对象与方法都相同就相等；</description></item>
/// <item><description>内容片段（RenderFragment）：只有同一个实例才算没变。父级每次渲染都新建片段（Razor 的 lambda），同一个实例说明父级没有重新渲染：
/// 级联值通知（RuneProvider、AppShell 重新渲染时，Blazor 的 CascadingValue 对对象值一律通知下级）时，Blazor 把上次的参数原样再交一遍，
/// 不认这一点的话，所有带子内容的组件都会白白重新渲染。不能用 Equals：同一处 lambda 每次新建的委托，目标与方法都相同，会被当成相等。
/// 由此有一条约定：缓存在字段里、渲染时读状态的片段，不要反复传给子组件（状态变了子组件会跳过），要每次渲染现建；</description></item>
/// <item><description>EventCallback、结构体、record：按值比较；</description></item>
/// <item><description>信号（<see cref="State{T}"/> 等）、<see cref="Style"/>、<see cref="Sx"/>、<see cref="Attrs"/>：本身不可变或会自己通知，比较引用即可。</description></item>
/// </list>
/// 其他引用类型（List、普通类）可能被原地修改，一律当作变了，保持 Blazor 原来的语义。
/// </remarks>
internal static class ParameterChangeDetector
{
    static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> parameterProperties = new();
    static readonly ConcurrentDictionary<Type, Comparison> comparisons = new();

    enum Comparison
    {
        /// <summary>比较不出来：一律当作变了。</summary>
        None,

        /// <summary>Equals 能说明内容没变。</summary>
        Value,

        /// <summary>只有同一个实例才算没变（内容片段）。</summary>
        Reference,
    }

    /// <summary>参数里有任何一个变了（或无法判断）就返回 true。</summary>
    public static bool HasChanged(IComponent component, ParameterView parameters, IReadOnlyDictionary<string, object>? unmatched)
    {
        var properties = parameterProperties.GetOrAdd(component.GetType(), FindParameterProperties);
        var unmatchedCount = 0;
        foreach (var parameter in parameters)
        {
            // 级联值与普通参数同样比较：可比较的（record 形式的配置）没变就跳过；
            // EditContext 这类会被原地修改的对象不在可比较的类型里，照常当作变了
            object? current;
            if (properties.TryGetValue(parameter.Name, out var property))
            {
                current = property.GetValue(component);
            }
            else
            {
                // 没声明的属性：落在 AdditionalAttributes 里
                unmatchedCount++;
                if (unmatched is null || !unmatched.TryGetValue(parameter.Name, out current)) return true;
            }
            if (!AreSame(current, parameter.Value)) return true;
        }
        // 父级去掉了某个没声明的属性
        return unmatchedCount != (unmatched?.Count ?? 0);
    }

    static bool AreSame(object? current, object? next)
    {
        if (current is null || next is null) return current is null && next is null;
        if (current.GetType() != next.GetType()) return false;
        // 先看类型：同一个 List 实例可能被原地改过，引用相同也不能说明没变
        return comparisons.GetOrAdd(next.GetType(), KindOf) switch
        {
            Comparison.Value => ReferenceEquals(current, next) || current.Equals(next),
            Comparison.Reference => ReferenceEquals(current, next),
            _ => false,
        };
    }

    static Comparison KindOf(Type type) =>
        IsRenderFragment(type) ? Comparison.Reference
        : IsComparable(type) ? Comparison.Value
        : Comparison.None;

    /// <summary>这个类型的 Equals 能说明“内容没变”（内容片段已在 <see cref="KindOf"/> 里先分出去）。</summary>
    static bool IsComparable(Type type) =>
        type.IsValueType
        || type == typeof(string)
        // 回调在调用时才读状态，同一个方法与目标就是同一个回调
        || typeof(Delegate).IsAssignableFrom(type)
        || typeof(Signal).IsAssignableFrom(type)
        || type == typeof(Style)
        || type == typeof(Sx)
        // 一层属性只在初始化器里赋值、创建后不变：同一个实例（静态的、Memo 住的）就是同样的属性
        || type == typeof(Attrs)
        || IsRecord(type);

    static bool IsRenderFragment(Type type) =>
        type == typeof(RenderFragment)
        || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>));

    // record 有编译器生成的 EqualityContract 属性
    static bool IsRecord(Type type) =>
        type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    // 沿继承链逐层取：GetProperties 不返回基类的私有属性（基类里私有的级联参数也要认）
    static IEnumerable<PropertyInfo> DeclaredProperties(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
            foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                yield return property;
    }

    static IReadOnlyDictionary<string, PropertyInfo> FindParameterProperties(Type type) =>
        DeclaredProperties(type)
            .Where(p => p.IsDefined(typeof(CascadingParameterAttributeBase), inherit: true)
                        || (p.IsDefined(typeof(ParameterAttribute), inherit: true)
                            && p.GetCustomAttribute<ParameterAttribute>()?.CaptureUnmatchedValues != true))
            // Blazor 的参数名不区分大小写
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
}
