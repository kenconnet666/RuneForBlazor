namespace Rune.E2E;

/// <summary>提及：弹出、过滤、选中写入名字都在 mentions.js 里，@ 后面的文字经 rune-command 交给 C# 过滤，写入后 C# 更新提到的人。</summary>
public class MentionsTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 输入At弹出并过滤_Enter写入名字_Esc关掉后这个At不再弹出(string mode)
    {
        var page = await OpenAsync(mode, "mentions");
        var textarea = page.GetByRole(AriaRole.Textbox, new() { Name = "审批意见" });
        var panel = page.Locator("[data-rune-mentions-panel]").First;

        await textarea.FocusAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.TypeAsync("@林小");
        await Assertions.Expect(panel).ToBeVisibleAsync();
        // 过滤在 C#：等列表只剩一项再按键，焦点一直在文本框里
        await Assertions.Expect(panel.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(textarea).ToHaveValueAsync(new System.Text.RegularExpressions.Regex("@林小满 $"));
        await Assertions.Expect(textarea).ToBeFocusedAsync();
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByText("林小满（E3002）")).ToBeVisibleAsync();

        await page.Keyboard.TypeAsync("@");
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await page.Keyboard.TypeAsync("吴");
        await Assertions.Expect(panel).ToBeHiddenAsync();
    }
}
