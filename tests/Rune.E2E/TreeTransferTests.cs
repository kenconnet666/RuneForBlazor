namespace Rune.E2E;

/// <summary>左边是树的穿梭框：点部门勾上它下面还没移过去的团队，点中间的按钮移到右边（值按先后追加），读屏播报移了几项。</summary>
public class TreeTransferTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 勾部门移过去_只移还没移的叶子(string mode)
    {
        var page = await OpenAsync(mode, "transfer");
        var transfer = page.GetByTestId("departments");

        await transfer.GetByRole(AriaRole.Treeitem, new() { Name = "研发中心" }).ClickAsync();
        await transfer.GetByRole(AriaRole.Button, new() { Name = "移到通知的团队" }).ClickAsync();

        await Assertions.Expect(page.GetByText("值：rd-fe、mfg-quality、rd-be、rd-qa")).ToBeVisibleAsync();
        await Assertions.Expect(transfer.GetByRole(AriaRole.Listbox).GetByRole(AriaRole.Option)).ToHaveCountAsync(4);
        await Assertions.Expect(transfer.Locator("[aria-live=polite]")).ToContainTextAsync("2");
    }
}
