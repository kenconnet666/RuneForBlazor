namespace Rune.Styles;

/// <summary>
/// 一个作用域（服务端渲染的一次请求、交互模式的一个电路或一个 WebAssembly 应用）用到的样式规则，只追加不删除。
/// 组件渲染时登记，<see cref="RuneStyles"/> 负责输出。以 scoped 服务注册（<c>services.AddRune()</c>）。
/// </summary>
public sealed class StyleRegistry
{
    [ThreadStatic] static StyleRegistry? current;

    readonly HashSet<string> registered = [];
    readonly List<string> rules = [];
    readonly Lock rulesLock = new();

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

    /// <summary>
    /// 当前正在渲染的组件所在作用域的登记表。<see cref="RuneComponent"/> 渲染期间设置；
    /// 在别处合并样式会拿不到，见 <see cref="Register"/>。
    /// </summary>
    internal static StyleRegistry? Current => current;

    /// <summary>在这段代码里把 <paramref name="registry"/> 设为当前登记表，结束后恢复外层的。</summary>
    internal static Scope Enter(StyleRegistry registry)
    {
        var previous = current;
        current = registry;
        return new Scope(previous);
    }

    /// <summary>把规则登记到当前作用域；不在 RuneComponent 的渲染里时报错（否则样式会悄悄丢失）。</summary>
    internal static void Register(CompiledStyle style)
    {
        var registry = current ?? throw new InvalidOperationException(
            "样式只能在 RuneComponent 的渲染中使用：组件请继承 RuneComponent（@inherits RuneComponent）");
        registry.Add(style);
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
