using System.Collections;

namespace Rune.UI;

/// <summary>一个组件的默认值（外观档位等），每个组件一个 record，属性都可空（null 表示不改）。</summary>
/// <typeparam name="TSelf">自身类型。</typeparam>
public interface IComponentDefaults<TSelf> where TSelf : class, IComponentDefaults<TSelf>
{
    /// <summary>叠在外层的默认值上：自己写了的项覆盖外层，没写的沿用外层。</summary>
    TSelf Over(TSelf outer);
}

/// <summary>
/// 各组件的默认值：组件上没写的参数取这里的值。
/// <code>
/// o.Defaults = new() { new CollapsibleDefaults { Variant = CollapsibleVariant.Outline } };
/// </code>
/// 只在创建时添加，之后不再修改（同一个实例会被缓存引用）。
/// </summary>
public sealed class ComponentDefaults : IEnumerable
{
    readonly Dictionary<Type, object> byComponent = [];

    /// <summary>空的默认值。</summary>
    public static readonly ComponentDefaults Empty = new();

    /// <summary>添加一个组件的默认值（集合初始化器用）；同一组件写两次时后者叠在前者上。</summary>
    public void Add<T>(T defaults) where T : class, IComponentDefaults<T> =>
        byComponent[typeof(T)] = Get<T>() is { } earlier ? defaults.Over(earlier) : defaults;

    /// <summary>某个组件的默认值；没有设置时返回 null。</summary>
    public T? Get<T>() where T : class, IComponentDefaults<T> =>
        byComponent.TryGetValue(typeof(T), out var value) ? (T)value : null;

    /// <summary>叠上内层的默认值（Provider 嵌套时）：同一组件按项合并，内层优先。</summary>
    public ComponentDefaults Then(ComponentDefaults inner)
    {
        if (inner.byComponent.Count == 0) return this;
        var merged = new ComponentDefaults();
        foreach (var (type, value) in byComponent) merged.byComponent[type] = value;
        foreach (var (type, value) in inner.byComponent)
            merged.byComponent[type] = merged.byComponent.TryGetValue(type, out var outer) ? Over(value, outer) : value;
        return merged;
    }

    // 内外两层是同一个 IComponentDefaults<T> 类型：按接口调用 Over
    static object Over(object inner, object outer) =>
        inner.GetType().GetInterfaces()
            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComponentDefaults<>))
            .GetMethod(nameof(IComponentDefaults<>.Over))!
            .Invoke(inner, [outer])!;

    IEnumerator IEnumerable.GetEnumerator() => byComponent.Values.GetEnumerator();
}
