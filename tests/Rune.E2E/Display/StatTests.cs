namespace Rune.E2E;

/// <summary>会滚动的数字：值变了时浏览器里逐帧过渡（不经过 C# 渲染），停下时是 C# 格式化的终值；读屏的终值文字马上就是新的。</summary>
public class StatTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 值变化时滚动过渡_停在终值(string mode)
    {
        var page = await OpenAsync(mode, "stat");
        var balance = page.Locator("[data-rune-tween]").Filter(new() { HasText = "¥" });
        var before = await balance.TextContentAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "换一组数" }).ClickAsync();
        // 记下过渡中出现过的文字，直到 data-value 变了且文字停住
        var shown = await balance.EvaluateAsync<string[]>("""
            async (el) => {
              const seen = new Set();
              const until = performance.now() + 1500;
              while (performance.now() < until) {
                seen.add(el.textContent);
                await new Promise(requestAnimationFrame);
              }
              return [...seen];
            }
            """);
        var final = await balance.TextContentAsync();
        var finalValue = decimal.Parse(await balance.GetAttributeAsync("data-value") ?? "0", System.Globalization.CultureInfo.InvariantCulture);

        Assert.NotEqual(before, final);
        Assert.Equal($"¥{finalValue:#,0.00}", final);
        // 起点与终点之外还出现过中间值
        Assert.True(shown.Except([before, final]).Any(), $"没有中间帧：{string.Join(" | ", shown)}");
        await Assertions.Expect(balance.Locator("xpath=following-sibling::span[1]")).ToHaveTextAsync(final!);
    }
}
