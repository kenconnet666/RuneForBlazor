namespace Rune.E2E;

/// <summary>
/// 文档页的首屏分批接管（Queued 的 Hold 与 Rune.Docs.Content 的 JS 初始化器）：交互模式接管预渲染的页面时，示例的效果先是占位、
/// 里面留着预渲染的内容，再一个一个换成真正渲染的组件。接管过程中效果区不能空过（先消失再出现），排完后占位都换掉、示例能响应。
/// </summary>
public class TakeoverTests(DemoServer demo) : E2ETest(demo)
{
    // 每一帧看一眼各个效果区：有没有空过、有没有出现过占位、占位里有没有放回预渲染的内容
    const string Watch = """
        window.__takeover = { emptyFrames: 0, holds: 0, filledHolds: 0 };
        document.addEventListener('DOMContentLoaded', () => {
          const seen = new WeakSet();
          const frame = () => {
            const canvases = [...document.querySelectorAll('[data-demo-canvas]')];
            if (canvases.length > 0 && canvases.some((canvas) => canvas.childElementCount === 0)) window.__takeover.emptyFrames++;
            for (const slot of document.querySelectorAll('[data-docs-hold]')) {
              if (seen.has(slot)) continue;
              seen.add(slot);
              window.__takeover.holds++;
              if (slot.childNodes.length > 0) window.__takeover.filledHolds++;
            }
            requestAnimationFrame(frame);
          };
          requestAnimationFrame(frame);
        });
        """;

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 交互接管时示例的预渲染内容一直在_排完后都是真正的组件(string mode)
    {
        var page = await OpenAsync(mode, "button", context => context.AddInitScriptAsync(Watch));

        var seen = await page.EvaluateAsync<int[]>("[window.__takeover.emptyFrames, window.__takeover.holds, window.__takeover.filledHolds]");
        var (emptyFrames, holds, filledHolds) = (seen[0], seen[1], seen[2]);
        // 出现过占位，而且每个占位里都放回了预渲染的内容；效果区没有一帧是空的
        Assert.True(holds > 0, "接管时没有出现占位：示例没有排队");
        Assert.Equal(holds, filledHolds);
        Assert.Equal(0, emptyFrames);

        // 排完了：占位都换成了真正渲染的内容，预渲染时的标记也不在了
        await Assertions.Expect(page.Locator("[data-docs-hold]")).ToHaveCountAsync(0);
        Assert.False(await page.EvaluateAsync<bool>("""
            (() => { const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_COMMENT);
              for (let c = walker.nextNode(); c; c = walker.nextNode()) if (c.data.includes('docs-hold:')) return true;
              return false; })()
            """));
        // 示例是活的：点“发布”，C# 把状态改成已发布
        var basic = page.Locator("#basic-preview");
        await basic.GetByTestId("publish").ClickAsync();
        await Assertions.Expect(basic.GetByText("已发布", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task 静态渲染的页面留着标记_没有占位()
    {
        var page = await OpenAsync("static", "button");
        await Assertions.Expect(page.Locator("[data-docs-hold]")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("#basic-preview [data-demo-canvas] button").First).ToBeVisibleAsync();
    }
}
