namespace Rune.E2E;

/// <summary>整条可点的条目：点整条中间等于点标题的按钮或链接；结束边的按钮叠在上面，只触发它自己。</summary>
public class ItemListTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 点整条选中_结束边的按钮只触发它自己(string mode)
    {
        var page = await OpenAsync(mode, "item-list");

        // 可选中的行：点“项目”那一条的中间，按钮带上 aria-current，右边的详情跟着换
        var folders = page.GetByRole(AriaRole.List, new() { Name = "文件夹" });
        var projects = folders.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "项目" });
        await projects.ScrollIntoViewIfNeededAsync();
        var box = await projects.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到“项目”那一条");
        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Assertions.Expect(projects.GetByRole(AriaRole.Button, new() { Name = "项目" })).ToHaveAttributeAsync("aria-current", "true");
        await Assertions.Expect(page.GetByText("3 个进行中", new() { Exact = true })).ToBeVisibleAsync();

        // 通知列表：点“标为已读”只把它标为已读，不打开链接
        var url = page.Url;
        await page.GetByRole(AriaRole.Button, new() { Name = "把“新的待办：资料审核”标为已读" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "把“新的待办：资料审核”标为已读" })).ToHaveCountAsync(0);
        Assert.Equal(url, page.Url);
    }
}
