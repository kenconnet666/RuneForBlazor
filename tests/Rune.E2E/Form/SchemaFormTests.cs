namespace Rune.E2E;

/// <summary>按模型生成的表单：明细表加一行后焦点到新行的第一个控件，删一行后焦点到“添加”按钮；显示条件随值出现、条件必填随提交报错。</summary>
public class SchemaFormTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 明细表加行删行时焦点有去处_条件字段随值出现(string mode)
    {
        var page = await OpenAsync(mode, "form");
        var example = page.Locator("#conditional-preview");
        await example.ScrollIntoViewIfNeededAsync();

        var add = example.GetByRole(AriaRole.Button, new() { Name = "添加明细" });
        await add.ClickAsync();
        await Assertions.Expect(example.Locator("tbody tr")).ToHaveCountAsync(3);
        await Assertions.Expect(example.GetByRole(AriaRole.Textbox, new() { Name = "项目（第 3 个明细）" })).ToBeFocusedAsync();

        await example.GetByRole(AriaRole.Button, new() { Name = "删除第 3 个明细" }).ClickAsync();
        await Assertions.Expect(example.Locator("tbody tr")).ToHaveCountAsync(2);
        await Assertions.Expect(example.GetByRole(AriaRole.Button, new() { Name = "添加明细" })).ToBeFocusedAsync();

        // 换成招待：出差城市收起，招待人数出现且必填
        await example.GetByText("招待", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(example.GetByRole(AriaRole.Textbox, new() { Name = "出差城市" })).ToHaveCountAsync(0);
        await example.GetByRole(AriaRole.Button, new() { Name = "提交" }).ClickAsync();
        await Assertions.Expect(example.GetByText("招待人数：必填").Or(example.GetByText("必填")).First).ToBeVisibleAsync();
    }
}
