namespace Rune.E2E;

/// <summary>
/// 端到端测试类的基类：<see cref="OpenAsync"/> 每次开一个新的浏览器上下文（互不影响），测试结束时（xunit 每个测试一个实例，用完即释放）全部关掉。
/// 不关的话整个程序跑完之前所有页面都还开着：WebAssembly 的运行时、Server 的电路一直占着内存，CI 的机器跑到一半被关停。
/// </summary>
public abstract class E2ETest(DemoServer demo) : IAsyncDisposable
{
    readonly List<IBrowserContext> contexts = [];

    /// <summary>
    /// 开一个新的浏览器上下文并打开演示页（<c>/{模式}/{演示}</c>）；交互模式下等到演示接管（data-demo-interactive）再返回。
    /// 剪贴板权限已给（复制按钮的测试要读剪贴板）。
    /// </summary>
    protected async Task<IPage> OpenAsync(string mode, string page, Func<IBrowserContext, Task>? prepare = null)
    {
        // 浏览器语言固定成 en-US（同 CI 的 Linux）：界面语言来自语言包，不随浏览器；本地的中文浏览器会掩盖这类问题
        var context = await demo.Browser.NewContextAsync(new() { BaseURL = demo.BaseUrl, ViewportSize = new() { Width = 1280, Height = 900 }, Locale = "en-US" });
        contexts.Add(context);
        await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        // 打开页面之前要做的（如拦下某些请求）
        if (prepare is not null) await prepare(context);
        var tab = await context.NewPageAsync();
        // 各测试类并行、共用一个演示进程：CI 的机器忙时一页偶尔要三十多秒才加载完。等到 DOM 就绪即可（后面还等初始化器），时限放宽到 60 秒
        await tab.GotoAsync($"/{mode}/{page}", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        // JS 初始化器装好之前，浮层、复制这类客户端行为还没人处理
        await tab.WaitForSelectorAsync("html[data-rune-ready]", new() { State = WaitForSelectorState.Attached, Timeout = 60_000 });
        if (mode != "static")
            await tab.WaitForSelectorAsync("[data-demo-interactive=true]", new() { Timeout = 60_000 });
        return tab;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        foreach (var context in contexts) await context.CloseAsync();
        GC.SuppressFinalize(this);
    }
}
