namespace Rune.E2E;

/// <summary>时间选择器：打开后焦点在列表、当前值高亮，午休的项不可用；选一项写回、关闭。</summary>
public class TimePickerTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 打开列表_选一项(string mode)
    {
        var page = await OpenAsync(mode, "time-picker");
        var start = page.GetByRole(AriaRole.Group, new() { Name = "会议开始" });
        await start.Locator("..").GetByRole(AriaRole.Button, new() { Name = "选择时间" }).ClickAsync();

        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "选择时间" }).Filter(new() { Visible = true });
        await Assertions.Expect(list).ToBeFocusedAsync();
        await Assertions.Expect(list.GetByRole(AriaRole.Option, new() { Name = "10:30" })).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(list.GetByRole(AriaRole.Option, new() { Name = "12:30" })).ToHaveAttributeAsync("aria-disabled", "true");

        await list.GetByRole(AriaRole.Option, new() { Name = "14:00" }).ClickAsync();
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(start).ToHaveTextAsync("14:00");
    }
}
