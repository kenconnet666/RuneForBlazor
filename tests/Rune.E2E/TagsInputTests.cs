namespace Rune.E2E;

/// <summary>标签输入：分隔符、Enter、粘贴、退格由 JS 转成命令或经输入事件交给 C#；建议列表的键盘同 Combobox。</summary>
public class TagsInputTests(DemoServer demo) : E2ETest(demo)
{
    static ILocator Tags(IPage page, string testId) =>
        page.GetByTestId(testId).Locator("xpath=..").Locator("[data-scope=badge][data-part=root]");

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 分隔符与Enter加标签_空时退格删最后一个_粘贴按分隔符拆开(string mode)
    {
        var page = await OpenAsync(mode, "tags-input");
        var input = page.GetByTestId("keywords");
        var summary = page.GetByTestId("basic-summary");

        await input.FocusAsync();
        await page.Keyboard.TypeAsync("Blazor,");
        await Assertions.Expect(summary).ToContainTextAsync("关键词：组件库、Vue、TSX、Blazor");
        await Assertions.Expect(input).ToHaveValueAsync("");

        await input.FillAsync("Rune");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(summary).ToContainTextAsync("TSX、Blazor、Rune ·");
        await Assertions.Expect(input).ToHaveValueAsync("");

        await page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(summary).Not.ToContainTextAsync("Rune");

        // 粘贴多段：拦下整段交给 C# 拆开；重复的（Vue）不加，超过上限（6 个）的不加
        await input.EvaluateAsync("""
            input => {
                const data = new DataTransfer();
                data.setData('text', 'Vue,CSS；Razor\nWasm');
                input.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
            }
            """);
        await Assertions.Expect(summary).ToContainTextAsync("关键词：组件库、Vue、TSX、Blazor、CSS、Razor ·");
        await Assertions.Expect(Tags(page, "keywords")).ToHaveCountAsync(6);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 从建议里选_只允许建议时自由文字不加(string mode)
    {
        var page = await OpenAsync(mode, "tags-input");
        var input = page.GetByTestId("recipients");
        var list = page.Locator($"#{await input.GetAttributeAsync("aria-controls")}");
        var summary = page.GetByTestId("basic-summary");

        await input.FillAsync("陈");
        await Assertions.Expect(list.Locator("[role=option][data-highlighted]")).ToHaveAttributeAsync("data-label", "陈默");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(summary).ToContainTextAsync("抄送：xiaoman@example.com、chenmo@example.com");
        await Assertions.Expect(input).ToHaveValueAsync("");
        await Assertions.Expect(Tags(page, "recipients").Last).ToContainTextAsync("陈默");

        await page.Keyboard.PressAsync("Escape");
        await input.FillAsync("不在名单里");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(input).ToHaveValueAsync("不在名单里");
        await Assertions.Expect(Tags(page, "recipients")).ToHaveCountAsync(2);
    }
}
