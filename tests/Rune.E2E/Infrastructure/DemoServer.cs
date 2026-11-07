[assembly: AssemblyFixture(typeof(Rune.E2E.DemoServer))]

namespace Rune.E2E;

/// <summary>
/// 整个测试程序共用的演示项目与浏览器：启动构建好的 Rune.Demo（<see cref="DemoProcess"/>），再启动一个无头 Chromium。
/// 测试类继承 <see cref="E2ETest"/>，用它的 OpenAsync 打开页面。
/// </summary>
public sealed class DemoServer : IAsyncLifetime
{
    DemoProcess? demo;
    IPlaywright? playwright;

    /// <summary>演示项目的地址，如 http://127.0.0.1:5xxxx。</summary>
    public string BaseUrl => demo!.BaseUrl;

    /// <summary>无头 Chromium。</summary>
    public IBrowser Browser { get; private set; } = null!;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        demo = await DemoProcess.StartAsync();

        // 没有浏览器时下载 Chromium（已有就直接返回）
        if (Microsoft.Playwright.Program.Main(["install", "chromium"]) != 0)
            throw new InvalidOperationException("Playwright 的 Chromium 安装失败");
        playwright = await Playwright.CreateAsync();
        Browser = await playwright.Chromium.LaunchAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Browser is not null) await Browser.DisposeAsync();
        playwright?.Dispose();
        if (demo is not null) await demo.DisposeAsync();
    }
}
