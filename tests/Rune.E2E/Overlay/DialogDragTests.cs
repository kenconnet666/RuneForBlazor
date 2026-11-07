namespace Rune.E2E;

/// <summary>可拖动的对话框：按住标题栏挪开，拖到窗口外时停在边上；下次打开回到正中；标题栏上的关闭按钮照常点。</summary>
public class DialogDragTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 按住标题栏拖动_不超出窗口_下次打开回到正中(string mode)
    {
        var page = await OpenAsync(mode, "dialog");
        var opener = page.GetByRole(AriaRole.Button, new() { Name = "核对付款信息" });
        await opener.ScrollIntoViewIfNeededAsync();
        await opener.ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "付款信息" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        // 进场的放大动画跑完再量
        await dialog.EvaluateAsync("d => Promise.all(d.getAnimations().map(a => a.finished))");
        var centered = await dialog.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到对话框");

        // 往左上拖 40px：跟着走
        var title = dialog.GetByRole(AriaRole.Heading, new() { Name = "付款信息" });
        var grab = await title.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到标题");
        await page.Mouse.MoveAsync(grab.X + 4, grab.Y + grab.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(grab.X - 36, grab.Y + grab.Height / 2 - 40, new() { Steps = 4 });
        var moved = await dialog.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到对话框");
        Assert.InRange(moved.X - centered.X, -41, -39);
        Assert.InRange(moved.Y - centered.Y, -41, -39);

        // 拖到窗口外：停在左上角
        await page.Mouse.MoveAsync(-500, -500, new() { Steps = 4 });
        await page.Mouse.UpAsync();
        var clamped = await dialog.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到对话框");
        Assert.InRange(clamped.X, -1, 1);
        Assert.InRange(clamped.Y, -1, 1);

        // 关闭按钮照常点；再打开回到正中
        await dialog.GetByRole(AriaRole.Button, new() { Name = "关闭" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await opener.ClickAsync();
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await dialog.EvaluateAsync("d => Promise.all(d.getAnimations().map(a => a.finished))");
        var reopened = await dialog.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到对话框");
        Assert.InRange(reopened.X - centered.X, -1, 1);
        Assert.InRange(reopened.Y - centered.Y, -1, 1);
    }
}
