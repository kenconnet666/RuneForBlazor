namespace Rune.E2E;

/// <summary>侧边导航：当前项随点击移动且锚点链接不离开本页；分组可以收起；紧凑时图标项有提示，分组悬停时子项弹出来。</summary>
public class NavListTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 点击换当前项_锚点不离开本页_分组可以收起(string mode)
    {
        var page = await OpenAsync(mode, "nav-list");
        var nav = page.GetByTestId("admin");
        var refund = nav.GetByRole(AriaRole.Link, new() { Name = "退款售后" });

        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "全部订单" })).ToHaveAttributeAsync("aria-current", "page");
        await refund.ClickAsync();
        await Assertions.Expect(refund).ToHaveAttributeAsync("aria-current", "page");
        Assert.EndsWith($"/{mode}/nav-list#orders-refund", page.Url);

        var orders = nav.GetByRole(AriaRole.Button, new() { Name = "订单" });
        await Assertions.Expect(orders).ToHaveAttributeAsync("aria-expanded", "true");
        await orders.ClickAsync();
        await Assertions.Expect(orders).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(refund).ToBeHiddenAsync();
    }

    [Fact]
    public async Task 紧凑时图标项悬停有提示_分组悬停弹出子项_关掉紧凑后显示文字()
    {
        var page = await OpenAsync("server", "nav-list");
        var nav = page.GetByTestId("compact");

        await nav.GetByRole(AriaRole.Link, new() { Name = "单价台账" }).HoverAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "单价台账" })).ToBeVisibleAsync();

        await nav.GetByRole(AriaRole.Button, new() { Name = "审批" }).HoverAsync();
        var mine = page.GetByRole(AriaRole.Link, new() { Name = "我发起的" });
        await Assertions.Expect(mine).ToBeVisibleAsync();
        await mine.ClickAsync();
        await Assertions.Expect(mine).ToHaveAttributeAsync("aria-current", "page");
        // 组里有当前页：分组的图标高亮
        await Assertions.Expect(nav.GetByRole(AriaRole.Button, new() { Name = "审批" })).ToHaveAttributeAsync("data-state", "active");

        await page.GetByRole(AriaRole.Switch, new() { Name = "只显示图标" }).ClickAsync();
        // 文字不再视觉隐藏
        await Assertions.Expect(nav.GetByRole(AriaRole.Tooltip)).ToHaveCountAsync(0);
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "库存台账" }).Locator("> span")).Not.ToHaveCSSAsync("position", "absolute");
        await Assertions.Expect(nav.GetByRole(AriaRole.Button, new() { Name = "审批" })).ToHaveAttributeAsync("aria-expanded", "true");
    }
}
