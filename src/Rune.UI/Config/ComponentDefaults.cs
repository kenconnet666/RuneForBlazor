namespace Rune.UI;

/// <summary>一个组件的默认值（外观档位等），每个组件一个 record，属性都可空（null 表示不改）。</summary>
/// <typeparam name="TSelf">自身类型。</typeparam>
public interface IComponentDefaults<TSelf> : IComponentDefaults where TSelf : class, IComponentDefaults<TSelf>
{
    /// <summary>叠在外层的默认值上：自己写了的项覆盖外层，没写的沿用外层。</summary>
    TSelf Over(TSelf outer);

    object IComponentDefaults.Over(object outer) => Over((TSelf)outer);
}

/// <summary>不带类型参数的 <see cref="IComponentDefaults{TSelf}"/>：<see cref="ComponentDefaults"/> 合并两层时按它调用，不用反射（WebAssembly 裁剪后也能用）。</summary>
public interface IComponentDefaults
{
    /// <summary>叠在外层（同一类型）的默认值上。</summary>
    object Over(object outer);
}

/// <summary>
/// 合成好的组件默认值（由 <see cref="ComponentConfigs"/> 的各项拆出，组件经 <see cref="UIConfig.Defaults"/> 读取）：组件上没写的参数取这里的值。
/// 配置写在 <see cref="ComponentConfigs"/>（<c>new ComponentConfig&lt;CardDefaults, CardPart&gt; { Defaults = … }</c>），这里只读。
/// </summary>
public sealed class ComponentDefaults
{
    readonly Dictionary<Type, object> byComponent = [];

    /// <summary>空的默认值。</summary>
    public static readonly ComponentDefaults Empty = new();

    /// <summary>添加一个组件的默认值；同一组件写两次时后者叠在前者上。</summary>
    internal void Add<T>(T defaults) where T : class, IComponentDefaults<T> =>
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

    // 内外两层是同一个 IComponentDefaults<T> 类型
    static object Over(object inner, object outer) => ((IComponentDefaults)inner).Over(outer);
}
