namespace Rune.E2E;

/// <summary>横向导航放不下时收进“更多”菜单；菜单导航按树渲染，空目录不显示，当前页跟着地址变。</summary>
public class NavBarTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 顶栏变窄时后面的项收进更多菜单(string mode)
    {
        var page = await OpenAsync(mode, "nav-list");
        var nav = page.GetByTestId("header");
        var more = nav.GetByRole(AriaRole.Button, new() { Name = "更多" });

        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "设置" })).ToBeVisibleAsync();
        await Assertions.Expect(more).ToBeHiddenAsync();

        await nav.EvaluateAsync("element => element.closest('[data-scope=card]').style.width = '280px'");
        await Assertions.Expect(more).ToBeVisibleAsync();
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "设置" })).ToBeHiddenAsync();
        await more.ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Menu).Last.GetByRole(AriaRole.Menuitem, new() { Name = "设置" })).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 菜单树_空目录不显示_含当前页的分组自动展开(string mode)
    {
        var page = await OpenAsync(mode, "nav-list");
        var menu = page.GetByTestId("menu");

        await Assertions.Expect(menu.GetByText("审批")).ToHaveCountAsync(0);
        await Assertions.Expect(menu.GetByRole(AriaRole.Link, new() { Name = "降本项目" })).ToHaveAttributeAsync("aria-current", "page");

        // 顶层换成可收起的分组：含当前页的“降本管理”自动展开，“系统”收着
        await page.GetByRole(AriaRole.Button, new() { Name = "分组", Exact = true }).ClickAsync();
        await Assertions.Expect(menu.GetByRole(AriaRole.Button, new() { Name = "降本管理" })).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(menu.GetByRole(AriaRole.Button, new() { Name = "系统" })).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
