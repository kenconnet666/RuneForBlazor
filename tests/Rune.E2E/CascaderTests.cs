namespace Rune.E2E;

/// <summary>级联选择：列里的移动由列表的 JS 处理，列之间（→ ←）与进入后的聚焦由 cascader.js 处理，展开与选中由 C# 处理。</summary>
public class CascaderTests(DemoServer demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键盘逐级进入与返回_Enter选中叶子后关闭(string mode)
    {
        var page = await demo.OpenAsync(mode, "cascader");
        var trigger = page.GetByRole(AriaRole.Combobox, new() { Name = "开票地区" });

        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        // 可搜索：先在搜索框里，搜索框空着时 ↓ 进入第一列并高亮第一项
        await Assertions.Expect(page.GetByRole(AriaRole.Searchbox).Last).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        var first = page.Locator("[data-rune-cascader]:popover-open [data-rune-cascader-column='0']");
        await Assertions.Expect(first).ToBeFocusedAsync();

        // ↓ 到江苏省：下一列跟着换；→ 进入，焦点到第二列
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "江苏省" })).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "江苏省" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(first).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "苏州市" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(trigger).ToHaveTextAsync("江苏省 / 苏州市 / 虎丘区");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }
}
