namespace Rune.E2E;

/// <summary>截断的文字：JS 量出是否真的截断，截断的才可聚焦、才弹全文提示；展开按钮只在截断时出现。</summary>
public class EllipsisTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 真的截断了才可聚焦_才弹提示_才有展开按钮(string mode)
    {
        var page = await OpenAsync(mode, "ellipsis");
        var longName = page.Locator("[data-rune-ellipsis]").Filter(new() { HasText = "萧山二厂" });
        var shortName = page.Locator("[data-rune-ellipsis]").Filter(new() { HasText = "无锡恒通" });
        await Assertions.Expect(longName).ToHaveAttributeAsync("data-truncated", "");
        await Assertions.Expect(longName.Locator("[data-rune-ellipsis-text]")).ToHaveAttributeAsync("tabindex", "0");
        await Assertions.Expect(shortName).Not.ToHaveAttributeAsync("data-truncated", new System.Text.RegularExpressions.Regex(".*"));
        await Assertions.Expect(shortName.Locator("[data-rune-ellipsis-text]")).Not.ToHaveAttributeAsync("tabindex", "0");

        await longName.Locator("[data-rune-ellipsis-text]").FocusAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Tooltip).Filter(new() { HasText = "萧山二厂" })).ToBeVisibleAsync();

        var description = page.Locator("[data-rune-ellipsis]").Filter(new() { HasText = "压铸件" });
        await Assertions.Expect(description.GetByRole(AriaRole.Button, new() { Name = "展开" })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-rune-ellipsis]").Filter(new() { HasText = "紧固件" }).GetByRole(AriaRole.Button)).ToBeHiddenAsync();
    }
}
