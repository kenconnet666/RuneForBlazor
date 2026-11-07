using System.Runtime.CompilerServices;

namespace Rune.Styles;

/// <summary>
/// 一个作用域（服务端渲染的一次请求、交互模式的一个电路或一个 WebAssembly 应用）用到的样式规则，只追加不删除。
/// 组件渲染时登记，<see cref="RuneStyles"/> 负责输出。以 scoped 服务注册（<c>services.AddRune()</c>）。
/// </summary>
/// <remarks>
/// <para>合并样式（<see cref="Attrs.Merge"/>）是静态方法，要靠“当前作用域”找到登记表。不能只在 RuneComponent 渲染时设置：
/// 写在内容片段里的样式可能由别的组件执行（CascadingValue、Virtualize、EditForm 的 ChildContent）。所以按渲染器找：</para>
/// <list type="bullet">
/// <item><description>Server 与服务端渲染：每个电路、每次请求的渲染都在它自己的同步上下文上执行，登记表绑定到这个上下文；</description></item>
/// <item><description>WebAssembly：单线程、只有一个作用域，用唯一的那个登记表。</description></item>
/// </list>
/// <para>登记表创建时（组件实例化、在渲染器的线程上注入）与 RuneComponent、RuneStyles 渲染时绑定。</para>
/// </remarks>
public sealed class StyleRegistry
{
    [ThreadStatic] static StyleRegistry? current;
    static readonly ConditionalWeakTable<SynchronizationContext, StyleRegistry> byContext = new();
    static StyleRegistry? browserRegistry;

    readonly HashSet<string> registered = [];
    readonly List<string> rules = [];
    readonly Lock rulesLock = new();

    /// <summary>创建登记表，并绑定到当前渲染器。</summary>
    public StyleRegistry() => BindToRenderer();

    /// <summary>由 DI 按作用域创建：先登记应用注册的基础规则（<see cref="GlobalRule"/>），每个作用域输出的第一块样式里就有它们。</summary>
    public StyleRegistry(IEnumerable<GlobalRule> globalRules) : this()
    {
        foreach (var rule in globalRules) Add(rule.Compiled);
    }

    /// <summary>新登记了规则。</summary>
    public event Action? Changed;

    /// <summary>到目前为止登记的全部规则，按登记顺序。</summary>
    public IReadOnlyList<string> Rules
    {
        get
        {
            lock (rulesLock) return [.. rules];
        }
    }

    /// <summary>从第 <paramref name="start"/> 条起新登记的规则（<see cref="RuneStyles"/> 每次只取新增的部分）。</summary>
    internal IReadOnlyList<string> RulesFrom(int start)
    {
        lock (rulesLock) return start >= rules.Count ? [] : rules[start..];
    }

    /// <summary>当前作用域的登记表：显式设置的（<see cref="Enter"/>）优先，其次按渲染器找；都没有时为 null。</summary>
    internal static StyleRegistry? Current =>
        current
        ?? (SynchronizationContext.Current is { } context && byContext.TryGetValue(context, out var bound) ? bound : null)
        ?? browserRegistry;

    /// <summary>把这个登记表绑定到当前渲染器（见类说明）。重复调用无害。</summary>
    internal void BindToRenderer()
    {
        if (SynchronizationContext.Current is { } context) byContext.AddOrUpdate(context, this);
        else if (OperatingSystem.IsBrowser()) browserRegistry = this;
    }

    /// <summary>在这段代码里把 <paramref name="registry"/> 设为当前登记表，结束后恢复外层的（测试与组件渲染用）。</summary>
    internal static Scope Enter(StyleRegistry registry)
    {
        var previous = current;
        current = registry;
        return new Scope(previous);
    }

    /// <summary>把规则登记到当前作用域；找不到时报错（否则样式会悄悄丢失）。</summary>
    internal static void Register(CompiledStyle style)
    {
        var registry = Current ?? throw new InvalidOperationException(
            "找不到样式登记表：样式要在组件渲染时使用，并且应用要注册 Rune（services.AddRune() 或 AddRuneUI()）");
        if (ReferenceEquals(style.LastRegistry, registry)) return;
        registry.Add(style);
        style.LastRegistry = registry;
    }

    void Add(CompiledStyle style)
    {
        lock (rulesLock)
        {
            if (!registered.Add(style.ClassName)) return;
            rules.Add(style.Css);
        }
        Changed?.Invoke();
    }

    /// <summary>恢复外层登记表。</summary>
    internal readonly struct Scope(StyleRegistry? previous) : IDisposable
    {
        /// <summary>恢复。</summary>
        public void Dispose() => current = previous;
    }
}
