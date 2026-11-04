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
/// <item><description>委托（RenderFragment、Action 等）：目标对象与方法都相同就相等，只引用组件成员的 ChildContent 每次都相等；</description></item>
/// <item><description>EventCallback、结构体、record：按值比较；</description></item>
/// <item><description>信号（<see cref="State{T}"/> 等）、<see cref="Style"/>、<see cref="Sx"/>：本身不可变或会自己通知，比较引用即可。</description></item>
/// </list>
/// 其他引用类型（List、普通类）可能被原地修改，一律当作变了，保持 Blazor 原来的语义。
/// </remarks>
internal static class ParameterChangeDetector
{
    static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> parameterProperties = new();
    static readonly ConcurrentDictionary<Type, bool> comparableTypes = new();

    /// <summary>参数里有任何一个变了（或无法判断）就返回 true。</summary>
    public static bool HasChanged(IComponent component, ParameterView parameters, IReadOnlyDictionary<string, object>? unmatched)
    {
        var properties = parameterProperties.GetOrAdd(component.GetType(), FindParameterProperties);
        var unmatchedCount = 0;
        foreach (var parameter in parameters)
        {
            // 级联值可能是被原地修改的对象（EditContext 之类），交给组件自己处理
            if (parameter.Cascading) return true;

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
        return comparableTypes.GetOrAdd(next.GetType(), IsComparable)
               && (ReferenceEquals(current, next) || current.Equals(next));
    }

    /// <summary>这个类型的 Equals 能说明“内容没变”。</summary>
    static bool IsComparable(Type type) =>
        type.IsValueType
        || type == typeof(string)
        || typeof(Delegate).IsAssignableFrom(type)
        || typeof(Signal).IsAssignableFrom(type)
        || type == typeof(Style)
        || type == typeof(Sx)
        || IsRecord(type);

    // record 有编译器生成的 EqualityContract 属性
    static bool IsRecord(Type type) =>
        type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    static IReadOnlyDictionary<string, PropertyInfo> FindParameterProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.IsDefined(typeof(ParameterAttribute), inherit: true)
                        && p.GetCustomAttribute<ParameterAttribute>()?.CaptureUnmatchedValues != true)
            // Blazor 的参数名不区分大小写
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
}
