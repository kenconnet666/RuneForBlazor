using System.Text.RegularExpressions;

namespace Rune.E2E;

/// <summary>截图基线用的演示项目：“现在”固定在 <see cref="FixedNow"/>，相对时间、倒计时、日历的今天每次一样。</summary>
public sealed class VisualServer : IAsyncLifetime
{
    /// <summary>固定的“现在”（同 tools/parity/clock.mjs）。</summary>
    public const string FixedNow = "2026-10-21T02:00:00Z";

    DemoProcess? demo;

    /// <summary>地址。</summary>
    public string BaseUrl => demo!.BaseUrl;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync() => demo = await DemoProcess.StartAsync($"--FixedNow={FixedNow}");

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (demo is not null) await demo.DisposeAsync();
    }
}

/// <summary>
/// 截图基线：每个文档页（静态 SSR）的每个示例效果区截一张，与 Visual/Baselines/{页}/{示例}.png 比较，防止样式改动意外改了画面。
/// 基线在 CI 的 Linux 上生成（字体与渲染随系统不同），所以只在 Linux 上比；手动运行 CI 并勾选更新基线时（RUNE_VISUAL_UPDATE=1）重新截，
/// 作为构件上传，人工看过后提交。不一致时实际的截图写到 artifacts/visual/{页}/，CI 上传以便对照。
/// </summary>
public partial class VisualTests(DemoServer demo, VisualServer visual) : IClassFixture<VisualServer>
{
    static readonly string Baselines = Path.Combine(DemoProcess.RepositoryRoot, "tests", "Rune.E2E", "Visual", "Baselines");
    static readonly string Failures = Path.Combine(DemoProcess.RepositoryRoot, "artifacts", "visual");
    static bool Updating => Environment.GetEnvironmentVariable("RUNE_VISUAL_UPDATE") == "1";

    // 明显不同的像素（任一通道差 > 16）超过这个比例才算变了：同一台机器、同一个浏览器截的图本应完全相同，留一点给抗锯齿
    const double Threshold = 0.002;

    /// <summary>全部文档页（组件页的地址 /components/{页}）。</summary>
    public static TheoryData<string> Pages =>
    [
        .. Directory.EnumerateFiles(Path.Combine(DemoProcess.RepositoryRoot, "apps", "Rune.Docs.Content", "Components"), "*Page.razor", SearchOption.AllDirectories)
            .Select(file => PageRoute().Match(File.ReadAllText(file)))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .Order(StringComparer.Ordinal),
    ];

    // 页面文件可能是 CRLF：多行模式的 $ 只认 \n
    [GeneratedRegex("""@page "/components/([a-z0-9-]+)"\r?$""", RegexOptions.Multiline)]
    private static partial Regex PageRoute();

    [Theory, MemberData(nameof(Pages))]
    public async Task 示例与基线一致(string page)
    {
        // RUNE_VISUAL=1 在别的系统上也跑（试用这套检查；截出来的图与 CI 的基线对不上）
        Assert.SkipUnless(OperatingSystem.IsLinux() || Environment.GetEnvironmentVariable("RUNE_VISUAL") == "1", "基线是在 CI 的 Linux 上截的，只在 Linux 上比");
        var expected = Path.Combine(Baselines, page);
        Assert.SkipUnless(Updating || Directory.Exists(expected), $"{page} 还没有基线（手动运行 CI 并勾选更新基线）");

        var context = await demo.Browser.NewContextAsync(new()
        {
            BaseURL = visual.BaseUrl,
            ViewportSize = new() { Width = 1280, Height = 900 },
            DeviceScaleFactor = 1,
            Locale = "en-US",
            ReducedMotion = ReducedMotion.Reduce,
        });
        try
        {
            await context.Clock.SetFixedTimeAsync(DateTime.Parse(VisualServer.FixedNow, null, System.Globalization.DateTimeStyles.AdjustToUniversal));
            var tab = await context.NewPageAsync();
            await tab.GotoAsync($"/static/{page}", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
            await tab.WaitForSelectorAsync("html[data-rune-ready]", new() { State = WaitForSelectorState.Attached });
            await tab.EvaluateAsync("document.fonts.ready");

            var shots = await ShootAsync(tab);
            if (Updating)
            {
                if (Directory.Exists(expected)) Directory.Delete(expected, recursive: true);
                Directory.CreateDirectory(expected);
                foreach (var (name, png) in shots) await File.WriteAllBytesAsync(Path.Combine(expected, $"{name}.png"), png, TestContext.Current.CancellationToken);
                return;
            }

            var problems = new List<string>();
            var baselineNames = Directory.EnumerateFiles(expected, "*.png").Select(Path.GetFileNameWithoutExtension).ToHashSet();
            foreach (var (name, png) in shots)
            {
                var file = Path.Combine(expected, $"{name}.png");
                if (!File.Exists(file))
                {
                    problems.Add($"{name}：没有基线");
                    continue;
                }
                var different = await DifferenceAsync(tab, await File.ReadAllBytesAsync(file, TestContext.Current.CancellationToken), png);
                if (different <= Threshold) continue;
                problems.Add(different > 1 ? $"{name}：尺寸变了" : $"{name}：{different:P2} 的像素不同");
                Directory.CreateDirectory(Path.Combine(Failures, page));
                await File.WriteAllBytesAsync(Path.Combine(Failures, page, $"{name}-actual.png"), png, TestContext.Current.CancellationToken);
                File.Copy(file, Path.Combine(Failures, page, $"{name}-expected.png"), overwrite: true);
            }
            problems.AddRange(baselineNames.Except(shots.Select(shot => shot.Name)).Select(name => $"{name}：示例不见了"));
            Assert.True(problems.Count == 0, $"{page} 的示例与基线不同：\n{string.Join('\n', problems)}");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    // 每个示例的效果区（排布格，data-demo-canvas）截一张，按小节的锚点命名；整页示例（iframe）等它加载完
    static async Task<List<(string Name, byte[] Png)>> ShootAsync(IPage tab)
    {
        var shots = new List<(string, byte[])>();
        foreach (var canvas in await tab.Locator("[data-demo-canvas]").AllAsync())
        {
            var name = await canvas.EvaluateAsync<string>("element => element.closest('section')?.querySelector('h2')?.id ?? 'example'");
            await canvas.ScrollIntoViewIfNeededAsync();
            if (await canvas.Locator("iframe").CountAsync() > 0)
                await canvas.Locator("iframe").First.ContentFrame.Locator("html[data-rune-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
            shots.Add((name, await canvas.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled })));
        }
        return shots;
    }

    // 两张截图里明显不同的像素的比例；尺寸不同时返回 2。在浏览器里用 canvas 解码与比较，不另装图像库
    static Task<double> DifferenceAsync(IPage tab, byte[] expected, byte[] actual) => tab.EvaluateAsync<double>("""
        async ([a, b]) => {
          const load = async (base64) => createImageBitmap(await (await fetch(`data:image/png;base64,${base64}`)).blob());
          const [x, y] = await Promise.all([load(a), load(b)]);
          if (x.width !== y.width || x.height !== y.height) return 2;
          const pixels = (image) => {
            const canvas = new OffscreenCanvas(image.width, image.height);
            const context = canvas.getContext('2d');
            context.drawImage(image, 0, 0);
            return context.getImageData(0, 0, image.width, image.height).data;
          };
          const [p, q] = [pixels(x), pixels(y)];
          let different = 0;
          for (let i = 0; i < p.length; i += 4)
            if (Math.abs(p[i] - q[i]) > 16 || Math.abs(p[i + 1] - q[i + 1]) > 16 || Math.abs(p[i + 2] - q[i + 2]) > 16) different++;
          return different / (x.width * x.height);
        }
        """, new[] { Convert.ToBase64String(expected), Convert.ToBase64String(actual) });
}
