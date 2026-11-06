namespace Rune.E2E;

/// <summary>树形下拉：↓ 打开进搜索框、输入过滤、↓ 进树、Enter 选中并关闭（焦点还给触发按钮）；多选点父节点勾上整枝，标签只列整枝。</summary>
public class TreeSelectTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键盘打开_搜索_进树_Enter选中并关闭(string mode)
    {
        var page = await OpenAsync(mode, "tree-select");
        var trigger = page.GetByTestId("org");

        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        var search = page.GetByRole(AriaRole.Searchbox, new() { Name = "搜索" }).First;
        await Assertions.Expect(search).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("后端");
        await Assertions.Expect(search).ToHaveValueAsync("后端");
        var tree = page.Locator($"#{await trigger.GetAttributeAsync("aria-controls")}");
        await Assertions.Expect(tree.GetByRole(AriaRole.Treeitem)).ToHaveCountAsync(2);

        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(tree).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(trigger).ToHaveTextAsync("后端团队");
        await Assertions.Expect(tree).Not.ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 多选点父节点勾上整枝_标签只列整枝(string mode)
    {
        var page = await OpenAsync(mode, "tree-select");
        var trigger = page.GetByTestId("teams");

        // 多选时标签排在触发按钮上面：用键盘打开
        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        var tree = page.Locator($"#{await trigger.GetAttributeAsync("aria-controls")}");
        await tree.GetByRole(AriaRole.Treeitem, new() { Name = "研发中心" }).ClickAsync();
        await Assertions.Expect(tree.GetByRole(AriaRole.Treeitem, new() { Name = "研发中心" })).ToHaveAttributeAsync("aria-checked", "true");
        // 多选不关
        await Assertions.Expect(tree).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("勾上的全部：finance-budget、rd、rd-frontend、rd-backend、mfg、mfg-assembly、mfg-testing、mfg-packing")).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToContainTextAsync("研发中心");
    }
}
