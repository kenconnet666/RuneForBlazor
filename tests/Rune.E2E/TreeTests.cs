namespace Rune.E2E;

/// <summary>树：焦点落在已选项，方向键、类型搜索、← 回到父节点与收起（JS 移动高亮，展开收起经命令交给 C#），空格勾选，展开时懒加载。</summary>
public class TreeTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键盘移动_类型搜索_Enter选中_左键回到父节点再收起(string mode)
    {
        var page = await OpenAsync(mode, "tree");
        var tree = page.GetByTestId("files");
        var active = async () => await page.Locator($"#{await tree.GetAttributeAsync("aria-activedescendant")}").InnerTextAsync();

        await tree.FocusAsync();
        // 第一次进入：落在已选的节点上
        await Assertions.Expect(tree).ToHaveAttributeAsync("aria-activedescendant", new System.Text.RegularExpressions.Regex("-2$"));
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal("Card.tsx", await active());
        await page.Keyboard.PressAsync("r");
        Assert.Equal("router.ts", await active());
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.GetByText("选中：/src/router.ts")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("ArrowLeft");
        Assert.Equal("src", await active());
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(tree.GetByRole(AriaRole.Treeitem)).ToHaveCountAsync(4);
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(tree.GetByRole(AriaRole.Treeitem)).ToHaveCountAsync(11);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 空格勾选_点击勾选联动父节点(string mode)
    {
        var page = await OpenAsync(mode, "tree");
        var tree = page.GetByTestId("regions");

        await tree.GetByRole(AriaRole.Treeitem, new() { Name = "温州" }).ClickAsync();
        await Assertions.Expect(tree.GetByRole(AriaRole.Treeitem, new() { Name = "浙江" })).ToHaveAttributeAsync("aria-checked", "true");
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(tree.GetByRole(AriaRole.Treeitem, new() { Name = "华南" })).ToHaveAttributeAsync("aria-checked", "true");
        await Assertions.Expect(page.GetByText("已勾选 8 个节点")).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 拖进另一章的中间_成为它的最后一节_落点在拖动中标出(string mode)
    {
        var page = await OpenAsync(mode, "tree");
        var tree = page.GetByTestId("outline");
        var culture = tree.GetByRole(AriaRole.Treeitem, new() { Name = "公司文化" });
        var process = tree.GetByRole(AriaRole.Treeitem, new() { Name = "第二章 采购流程" });
        // 鼠标按视口坐标移动：先滚进视口再取位置
        await tree.ScrollIntoViewIfNeededAsync();
        var from = (await culture.BoundingBoxAsync())!;
        var to = (await process.BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height / 2, new() { Steps = 8 });
        await Assertions.Expect(process).ToHaveAttributeAsync("data-drop", "inside");
        await page.Mouse.UpAsync();

        await Assertions.Expect(page.GetByText("已移动 1 次")).ToBeVisibleAsync();
        var titles = await tree.GetByRole(AriaRole.Treeitem).AllInnerTextsAsync();
        Assert.Equal("公司文化", titles[titles.ToList().IndexOf("合同审批") + 1].Trim());
        await Assertions.Expect(process).Not.ToHaveAttributeAsync("data-drop", "inside");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 展开时懒加载子节点(string mode)
    {
        var page = await OpenAsync(mode, "tree");
        var tree = page.GetByTestId("org");
        var product = tree.GetByRole(AriaRole.Treeitem).First;

        await product.Locator("span[aria-hidden=true]").First.ClickAsync();
        await Assertions.Expect(product).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(tree.GetByRole(AriaRole.Treeitem, new() { Name = "产品设计组" })).ToBeVisibleAsync();
        await Assertions.Expect(product).Not.ToHaveAttributeAsync("aria-busy", "true");
    }
}
