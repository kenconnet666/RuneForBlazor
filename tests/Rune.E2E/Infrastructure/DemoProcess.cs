using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Rune.E2E;

/// <summary>
/// 一个运行中的演示项目：构建好的 Rune.Demo（Development 环境，静态资源走开发时的清单），监听本机的空闲端口。
/// 端到端测试共用一个（<see cref="DemoServer"/>）；截图基线另起一个，把“现在”固定住（<see cref="VisualServer"/>）。
/// </summary>
public sealed class DemoProcess : IAsyncDisposable
{
    readonly Process process;

    DemoProcess(Process process, string baseUrl) => (this.process, BaseUrl) = (process, baseUrl);

    /// <summary>地址，如 http://127.0.0.1:5xxxx。</summary>
    public string BaseUrl { get; }

    /// <summary>仓库根目录（有 RuneForBlazor.slnx 的那一层）。</summary>
    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    /// <summary>启动演示项目并等它能响应；<paramref name="arguments"/> 追加到命令行（配置项，如 <c>--FixedNow=…</c>）。</summary>
    public static async Task<DemoProcess> StartAsync(params string[] arguments)
    {
        // 测试程序在 tests/Rune.E2E/bin/{配置}/net10.0/：演示项目用同一个配置
        var configuration = new DirectoryInfo(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory)).Parent!.Name;
        var assembly = Path.Combine(RepositoryRoot, "apps", "Rune.Demo", "bin", configuration, "net10.0", "Rune.Demo.dll");
        if (!File.Exists(assembly)) throw new InvalidOperationException($"找不到演示项目的输出 {assembly}：先构建解决方案");

        var baseUrl = $"http://127.0.0.1:{FreePort()}";
        var start = new ProcessStartInfo("dotnet", [assembly, "--urls", baseUrl, .. arguments])
        {
            // 内容根是项目目录（同 dotnet run），开发环境下静态资源按清单从各项目里取
            WorkingDirectory = Path.Combine(RepositoryRoot, "apps", "Rune.Demo"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        var process = Process.Start(start) ?? throw new InvalidOperationException("演示项目没有启动");
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var demo = new DemoProcess(process, baseUrl);
        await demo.WaitUntilServingAsync();
        return demo;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        process.Dispose();
    }

    async Task WaitUntilServingAsync()
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited) throw new InvalidOperationException($"演示项目退出了（{process.ExitCode}）");
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

    static string FindRepositoryRoot()
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
