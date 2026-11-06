using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

[assembly: AssemblyFixture(typeof(Rune.E2E.DemoServer))]

namespace Rune.E2E;

/// <summary>
/// 整个测试程序共用的演示项目与浏览器：启动构建好的 Rune.Demo（Development 环境，静态资源走开发时的清单），
/// 再启动一个无头 Chromium。测试类继承 <see cref="E2ETest"/>，用它的 OpenAsync 打开页面。
/// </summary>
public sealed class DemoServer : IAsyncLifetime
{
    Process? server;
    IPlaywright? playwright;

    /// <summary>演示项目的地址，如 http://127.0.0.1:5xxxx。</summary>
    public string BaseUrl { get; private set; } = "";

    /// <summary>无头 Chromium。</summary>
    public IBrowser Browser { get; private set; } = null!;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        var root = RepositoryRoot();
        // 测试程序在 tests/Rune.E2E/bin/{配置}/net10.0/：演示项目用同一个配置
        var configuration = new DirectoryInfo(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory)).Parent!.Name;
        var assembly = Path.Combine(root, "apps", "Rune.Demo", "bin", configuration, "net10.0", "Rune.Demo.dll");
        if (!File.Exists(assembly)) throw new InvalidOperationException($"找不到演示项目的输出 {assembly}：先构建解决方案");

        BaseUrl = $"http://127.0.0.1:{FreePort()}";
        var start = new ProcessStartInfo("dotnet", [assembly, "--urls", BaseUrl])
        {
            // 内容根是项目目录（同 dotnet run），开发环境下静态资源按清单从各项目里取
            WorkingDirectory = Path.Combine(root, "apps", "Rune.Demo"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        server = Process.Start(start) ?? throw new InvalidOperationException("演示项目没有启动");
        server.OutputDataReceived += (_, _) => { };
        server.ErrorDataReceived += (_, _) => { };
        server.BeginOutputReadLine();
        server.BeginErrorReadLine();
        await WaitUntilServingAsync();

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
        if (server is { HasExited: false })
        {
            server.Kill(entireProcessTree: true);
            await server.WaitForExitAsync();
        }
        server?.Dispose();
    }

    async Task WaitUntilServingAsync()
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (server!.HasExited) throw new InvalidOperationException($"演示项目退出了（{server.ExitCode}）");
            try
            {
                if ((await http.GetAsync($"{BaseUrl}/static/button")).IsSuccessStatusCode) return;
            }
            catch (HttpRequestException)
            {
                // 还没开始监听
            }
            await Task.Delay(250);
        }
        throw new TimeoutException("演示项目 60 秒内没有响应");
    }

    static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "RuneForBlazor.slnx"))) return dir.FullName;
        throw new InvalidOperationException("找不到仓库根目录（RuneForBlazor.slnx）");
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
