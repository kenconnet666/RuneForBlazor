namespace Rune.E2E;

/// <summary>虚拟列表：一万项只渲染看得见的一段，滚到中间换成那一段；横向跳到指定项；滚到末尾时加载下一页（骨架接在末尾）。</summary>
public class VirtualListTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 只渲染看得见的一段_滚到中间换成那一段(string mode)
    {
        var page = await OpenAsync(mode, "virtual-list");
        var list = page.GetByTestId("contacts");
        var items = list.GetByRole(AriaRole.Listitem);

        await Assertions.Expect(items.First).ToHaveAttributeAsync("aria-setsize", "10000");
        Assert.InRange(await items.CountAsync(), 1, 40);

        await list.EvaluateAsync("element => element.scrollTop = 56 * 5000");
        await Assertions.Expect(list.Locator("[role=listitem][aria-posinset='5001']")).ToBeVisibleAsync();
        Assert.InRange(await items.CountAsync(), 1, 40);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 横向跳到第500张(string mode)
    {
        var page = await OpenAsync(mode, "virtual-list");
        var album = page.GetByTestId("album");

        await page.GetByRole(AriaRole.Button, new() { Name = "跳过去" }).ClickAsync();
        var target = album.Locator("[role=listitem][aria-posinset='500']");
        await Assertions.Expect(target).ToBeVisibleAsync();
        var albumBox = (await album.BoundingBoxAsync())!;
        var targetBox = (await target.BoundingBoxAsync())!;
        Assert.InRange(targetBox.X - albumBox.X, -2, 2);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 滚到末尾时加载下一页_失败时重试(string mode)
    {
        var page = await OpenAsync(mode, "virtual-list");
        var feed = page.GetByTestId("feed");

        await Assertions.Expect(page.GetByText("已加载 30 / 150 条")).ToBeVisibleAsync();
        await feed.EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        await Assertions.Expect(page.GetByText("已加载 60 / 150 条")).ToBeVisibleAsync();

        // 第 3 页第一次失败：列表下方出现“重试”，点了接着取
        await feed.EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        var retry = page.GetByRole(AriaRole.Button, new() { Name = "重试" });
        await Assertions.Expect(retry).ToBeVisibleAsync();
        await retry.ClickAsync();
        await Assertions.Expect(page.GetByText("已加载 90 / 150 条")).ToBeVisibleAsync();
    }
}
