using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using Rune.Docs.Site;

namespace Rune.Desktop;

/// <summary>
/// 自检：环境变量 RUNE_DESKTOP_SELFCHECK 给出报告文件的路径时，启动后逐个打开文档页，每页记下脚本错误、.NET 的错误日志与 Rune 的状态
/// （初始化器装好没有、根元素的主题、标题），写完报告关闭应用。用来核对 Rune 在 Hybrid 模式下的表现。
/// 不用 Playwright：MAUI 在 Windows 上不把启动选项交给 WebView2，远程调试端口开不出来。
/// </summary>
static class SelfCheck
{
    /// <summary>报告文件的路径；没有时不自检。</summary>
    public static string? Report { get; } = Environment.GetEnvironmentVariable("RUNE_DESKTOP_SELFCHECK") is { Length: > 0 } path ? path : null;

    // RUNE_DESKTOP_SELFCHECK_QUICK=1：每个分类只查第一页（CI 用，全部 110 页要几分钟）
    static IEnumerable<DocEntry> Pages => Environment.GetEnvironmentVariable("RUNE_DESKTOP_SELFCHECK_QUICK") == "1"
        ? DocsCatalog.Sections.Select(section => section.Pages[0])
        : DocsCatalog.All;

    static readonly ConcurrentQueue<string> dotnetErrors = new();

    // 页面里的错误：未捕获的异常、未处理的 Promise 拒绝、console.error
    const string Capture = """
        window.__runeErrors = [];
        // ResizeObserver 的这条是浏览器的提示（本轮没送完的通知下一帧再送），不是错误，时机凑巧就会出现
        addEventListener('error', (e) => { if (!e.message?.startsWith('ResizeObserver loop')) __runeErrors.push('error: ' + e.message); });
        addEventListener('unhandledrejection', (e) => __runeErrors.push('rejection: ' + (e.reason?.message ?? e.reason)));
        const consoleError = console.error;
        console.error = (...args) => { __runeErrors.push('console: ' + args.map(String).join(' ')); consoleError(...args); };
        """;

    const string State = """
        JSON.stringify({
          ready: document.documentElement.hasAttribute('data-rune-ready'),
          theme: document.documentElement.dataset.theme ?? null,
          background: document.documentElement.style.backgroundColor,
          h1: document.querySelector('h1')?.textContent ?? null,
          viewport: innerWidth + 'x' + innerHeight,
          errors: (window.__runeErrors ?? []).splice(0),
        })
        """;

    /// <summary>收集 .NET 的错误日志（渲染、事件处理里没接住的异常）。</summary>
    public static void AddLogging(ILoggingBuilder logging) => logging.AddProvider(new ErrorCollector());

    /// <summary>WebView2 准备好后开始自检。</summary>
    public static void Attach(BlazorWebView view)
    {
        var started = false;
        view.BlazorWebViewInitialized += async (_, e) =>
        {
            var web = e.WebView.CoreWebView2;
            await web.AddScriptToExecuteOnDocumentCreatedAsync(Capture);
            web.NavigationCompleted += async (_, _) =>
            {
                if (started) return;
                started = true;
                await RunAsync(web);
            };
        };
    }

    static async Task RunAsync(Microsoft.Web.WebView2.Core.CoreWebView2 web)
    {
        var lines = new List<string>();
        async Task<JsonElement> StateAsync() => JsonDocument.Parse(JsonSerializer.Deserialize<string>(await web.ExecuteScriptAsync(State))!).RootElement;

        // 初始化器装好（最多等 30 秒）
        var waited = 0;
        while (!(await StateAsync()).GetProperty("ready").GetBoolean() && waited < 30_000)
        {
            await Task.Delay(250);
            waited += 250;
        }
        lines.Add($"start\tready after {waited}ms\t{(await StateAsync()).GetRawText()}");

        // 标题栏：顶栏是拖动区域、里面的按钮不可拖、结束边三个窗口按钮；点最大化真的最大化（按钮名称变成“还原”），再点还原
        async Task<string> Eval(string script) => JsonSerializer.Deserialize<string>(await web.ExecuteScriptAsync(script)) ?? "";
        const string TitleBar = """
            (() => {
              const header = document.querySelector('[data-desktop] > header');
              const region = (el) => el ? getComputedStyle(el).getPropertyValue('app-region') : 'missing';
              const buttons = [...document.querySelectorAll('[data-window-command]')].map((b) => b.dataset.windowCommand + ':' + b.getAttribute('aria-label') + ':' + region(b));
              return `header=${region(header)} height=${header?.offsetHeight} firstButton=${region(header?.querySelector('button'))} buttons=${buttons.join(' ')}`;
            })()
            """;
        lines.Add("titlebar\t" + await Eval(TitleBar));
        const string Maximize = "document.querySelector('[data-window-command=maximize]')?.click(), ''";
        const string MaximizeLabel = "document.querySelector('[data-window-command=maximize]')?.getAttribute('aria-label') ?? ''";
        await Eval(Maximize);
        await Task.Delay(800);
        var afterMaximize = await Eval(MaximizeLabel);
        await Eval(Maximize);
        await Task.Delay(800);
        lines.Add($"maximize\tafter click={afterMaximize} after second click={await Eval(MaximizeLabel)}");

        foreach (var page in Pages)
        {
            await web.ExecuteScriptAsync($"Blazor.navigateTo({JsonSerializer.Serialize(page.Path)})");
            await Task.Delay(1500);
            var loaded = await StateAsync();
            // 打开页面上第一个浮层（菜单、下拉、气泡）再关上：浮层定位的脚本（floating-ui）第一次打开时才加载
            var opened = JsonSerializer.Deserialize<string>(await web.ExecuteScriptAsync(
                "(() => { const b = document.querySelector('main [aria-haspopup]:not([disabled])'); b?.click(); return b ? b.getAttribute('aria-label') ?? b.textContent.trim().slice(0, 20) : ''; })()"));
            await Task.Delay(600);
            await web.ExecuteScriptAsync("document.activeElement?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))");
            await Task.Delay(200);
            var popover = await StateAsync();
            List<string> Errors(JsonElement state) => [.. state.GetProperty("errors").EnumerateArray().Select(e => e.GetString()!).Concat(Drain()).Distinct()];
            var (onLoad, onPopover) = (Errors(loaded), Errors(popover));
            lines.Add($"{page.Path}	h1={popover.GetProperty("h1")}	errors={onLoad.Count + onPopover.Count}");
            lines.AddRange(onLoad.Select(error => $"    load: {error}"));
            lines.AddRange(onPopover.Select(error => $"    popover「{opened}」: {error}"));
        }

        await File.WriteAllLinesAsync(Report!, lines);
        Application.Current?.Quit();
    }

    static IEnumerable<string> Drain()
    {
        while (dotnetErrors.TryDequeue(out var error)) yield return "dotnet: " + error;
    }

    sealed class ErrorCollector : ILoggerProvider, ILogger
    {
        public ILogger CreateLogger(string categoryName) => this;
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) dotnetErrors.Enqueue($"{formatter(state, exception)} {exception?.GetType().Name}: {exception?.Message}");
        }
    }
}
