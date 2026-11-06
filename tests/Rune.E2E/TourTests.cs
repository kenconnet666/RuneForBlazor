namespace Rune.E2E;

/// <summary>新手引导：第一步没有目标时卡片居中；→ 下一步时镂空盖住目标、卡片贴着它；Esc 关闭后焦点回到打开前；走完点“完成”调用 OnFinish。</summary>
public class TourTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 一步一步高亮目标_Esc关闭_完成时回调(string mode)
    {
        var page = await OpenAsync(mode, "tour");
        var start = page.GetByRole(AriaRole.Button, new() { Name = "开始引导" });
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "欢迎使用订单中心" });

        await start.ClickAsync();
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(dialog.Locator("[data-rune-tour-panel]")).ToHaveAttributeAsync("data-centered", "");

        await page.Keyboard.PressAsync("ArrowRight");
        var search = page.GetByRole(AriaRole.Dialog, new() { Name = "搜索" });
        await Assertions.Expect(search).ToBeVisibleAsync();
        var spotlight = search.Locator("[data-rune-tour-spotlight]");
        await Assertions.Expect(spotlight).Not.ToHaveAttributeAsync("data-dimmed", "");
        // 镂空盖住目标（四周留 6px）
        await page.WaitForFunctionAsync("""
            () => {
              const target = document.querySelector('[data-tour=search]').getBoundingClientRect();
              const hole = document.querySelector('[data-rune-tour-spotlight]').getBoundingClientRect();
              return Math.abs(hole.left - (target.left - 6)) < 2 && Math.abs(hole.width - (target.width + 12)) < 2;
            }
            """);

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(search).ToBeHiddenAsync();
        await Assertions.Expect(start).ToBeFocusedAsync();

        await start.ClickAsync();
        foreach (var title in new[] { "欢迎使用订单中心", "搜索", "筛选", "导出" })
        {
            var current = page.GetByRole(AriaRole.Dialog, new() { Name = title });
            await Assertions.Expect(current).ToBeVisibleAsync();
            await current.GetByRole(AriaRole.Button, new() { Name = "下一步" }).ClickAsync();
        }
        await page.GetByRole(AriaRole.Dialog, new() { Name = "订单列表" }).GetByRole(AriaRole.Button, new() { Name = "完成" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        // 示例的源码里也有这几个字：精确匹配显示的那一行
        await Assertions.Expect(page.GetByText("已完成引导", new() { Exact = true })).ToBeVisibleAsync();
    }
}
