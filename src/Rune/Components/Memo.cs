namespace Rune;

/// <summary>
/// 按输入记住上一次的结果：输入没变就返回同一个对象。组件上由组件管理的属性层用它，渲染路径不分配，
/// 交给子组件时参数也不会因为“新对象”被当作变了：
/// <code>
/// Memo&lt;(string Href, bool External), Attrs&gt; managed;
/// Attrs Managed => managed.Get((Href, External), static k => new Attrs { ["href"] = k.Href, … });
/// </code>
/// </summary>
/// <remarks>是可变的结构体：声明成组件的普通字段（不能是 readonly，也不要复制出去用）。</remarks>
/// <typeparam name="TKey">输入；用元组组合多个值。</typeparam>
/// <typeparam name="T">结果。</typeparam>
public struct Memo<TKey, T> where T : class
{
    TKey key;
    T? value;

    /// <summary>输入与上次相同时返回上次的结果，否则用 <paramref name="create"/> 创建（写成 static lambda）。</summary>
    public T Get(TKey key, Func<TKey, T> create)
    {
        if (value is not null && EqualityComparer<TKey>.Default.Equals(this.key, key)) return value;
        this.key = key;
        return value = create(key);
    }

    /// <summary>
    /// 同 <see cref="Get(TKey, Func{TKey, T})"/>，创建时要用到组件本身（事件处理器）时把它作为 <paramref name="state"/> 传入，
    /// lambda 仍可写成 static（捕获 this 的 lambda 每次渲染都会新建委托）。
    /// </summary>
    public T Get<TState>(TKey key, TState state, Func<TKey, TState, T> create)
    {
        if (value is not null && EqualityComparer<TKey>.Default.Equals(this.key, key)) return value;
        this.key = key;
        return value = create(key, state);
    }
}
