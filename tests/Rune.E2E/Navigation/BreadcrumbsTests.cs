namespace Rune.E2E;

/// <summary>面包屑的折叠菜单列出被收起的各级链接；步骤条点回已完成的步骤。</summary>
public class BreadcrumbsTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 折叠处的菜单列出中间几级_步骤条点回已完成的步骤(string mode)
    {
        var page = await OpenAsync(mode, "breadcrumbs");
        var deep = page.GetByTestId("deep");

        await deep.GetByRole(AriaRole.Button, new() { Name = "显示被折叠的路径" }).ClickAsync();
        var menu = page.GetByRole(AriaRole.Menu);
        await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem)).ToHaveTextAsync(["项目", "官网改版", "设计稿"]);
        await page.Keyboard.PressAsync("Escape");

        var checkout = page.GetByTestId("checkout");
        await checkout.GetByRole(AriaRole.Button, new() { Name = "购物车" }).ClickAsync();
        await Assertions.Expect(page.GetByText("第 1 步：购物车。已完成的步骤可以点回去修改。")).ToBeVisibleAsync();
        await Assertions.Expect(checkout.Locator("li").First).ToHaveAttributeAsync("aria-current", "step");
    }
}
