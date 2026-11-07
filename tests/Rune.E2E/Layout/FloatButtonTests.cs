namespace Rune.E2E;

/// <summary>浮动按钮：组里的回到顶部滚过阈值才出现、点了滚回去；速拨点主按钮展开，Esc 收起并把焦点还给主按钮，点到外面也收起。</summary>
public class FloatButtonTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 组里的回到顶部滚下去才出现_点了滚回顶部(string mode)
    {
        var page = await OpenAsync(mode, "float-button");
        var group = page.GetByRole(AriaRole.Group, new() { Name = "帮助" });
        await group.ScrollIntoViewIfNeededAsync();
        var backTop = group.Locator("button[data-rune-back-top]");
        await Assertions.Expect(backTop).ToBeHiddenAsync();

        var panel = page.Locator("#float-button-demo-panel");
        await panel.EvaluateAsync("p => p.scrollTop = 300");
        await Assertions.Expect(backTop).ToBeVisibleAsync();
        await backTop.ClickAsync();
        await page.WaitForFunctionAsync("p => p.scrollTop === 0", await panel.ElementHandleAsync());
        await Assertions.Expect(backTop).ToBeHiddenAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 速拨展开_Esc收起焦点回主按钮_点外面也收起(string mode)
    {
        var page = await OpenAsync(mode, "float-button");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "新建", Exact = true });
        await trigger.ScrollIntoViewIfNeededAsync();
        var newDoc = page.GetByRole(AriaRole.Button, new() { Name = "新建文档" });

        await trigger.ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(newDoc).ToBeVisibleAsync();

        await newDoc.FocusAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
        await Assertions.Expect(newDoc).ToBeHiddenAsync();

        await trigger.ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await page.GetByText("季度经营分析.docx").First.ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");

        // 选了一项：加一个文件、收起
        await trigger.ClickAsync();
        await newDoc.ClickAsync();
        await Assertions.Expect(page.GetByText("共 4 个文件").First).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
