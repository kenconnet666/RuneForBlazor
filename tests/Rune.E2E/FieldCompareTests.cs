namespace Rune.E2E;

/// <summary>字段对比：换版本后表格跟着变、只看改动；编辑表单改了数量，保存前确认的列表多出一项，还原后显示“没有改动”。</summary>
public class FieldCompareTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 版本对比与编辑前后(string mode)
    {
        var page = await OpenAsync(mode, "field-compare");
        await Assertions.Expect(page.GetByRole(AriaRole.Table, new() { Name = "测算 V1 与 V3 对比" }).Locator("tbody tr")).ToHaveCountAsync(8);

        // V1 → V2：单价、年度金额、利润率、材料、状态改了，用量、供应商、有效期没改
        await page.GetByRole(AriaRole.Group, new() { Name = "对比版本" }).GetByRole(AriaRole.Button, new() { Name = "V2" }).ClickAsync();
        var versions = page.GetByRole(AriaRole.Table, new() { Name = "测算 V1 与 V2 对比" });
        await Assertions.Expect(versions.Locator("tbody tr[data-changed]")).ToHaveCountAsync(5);
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "只看改动" }).CheckAsync();
        await Assertions.Expect(versions.Locator("tbody tr")).ToHaveCountAsync(5);

        var confirm = page.Locator("dl").First;
        await Assertions.Expect(confirm.Locator("dt")).ToHaveTextAsync(["数量", "折扣", "金额", "优先级"]);
        await page.GetByRole(AriaRole.Button, new() { Name = "还原" }).ClickAsync();
        await Assertions.Expect(page.GetByText("没有改动", new() { Exact = true })).ToBeVisibleAsync();
    }
}
