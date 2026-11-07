namespace Rune.E2E;

/// <summary>日历：方向键移动焦点、移出当前月时翻页、Enter 选中；标题切到月份视图再回来；区间日历先点开始再点结束、Esc 取消。</summary>
public class CalendarTests(DemoServer demo) : E2ETest(demo)
{
    static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd");

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键盘_翻页_选中_视图(string mode)
    {
        var page = await OpenAsync(mode, "calendar");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var departure = today.AddDays(3);

        var calendar = page.Locator("[data-rune-calendar][aria-label=\"出发日期\"]");
        var focused = calendar.Locator("button[data-focused]");
        await Assertions.Expect(focused).ToHaveAttributeAsync("data-date", Iso(departure));
        await focused.FocusAsync();

        // → 一天、↓ 一周；PageDown 下个月（标题跟着变）
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowDown");
        var moved = departure.AddDays(8);
        await Assertions.Expect(page.Locator(":focus")).ToHaveAttributeAsync("data-date", Iso(moved));
        await page.Keyboard.PressAsync("PageDown");
        var next = moved.AddMonths(1);
        await Assertions.Expect(page.Locator(":focus")).ToHaveAttributeAsync("data-date", Iso(next));
        await Assertions.Expect(calendar.GetByRole(AriaRole.Grid)).ToHaveAttributeAsync("aria-label", $"{next.Year}年{next.Month}月");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.Locator(":focus")).ToHaveAttributeAsync("data-selected", "");
        await Assertions.Expect(page.GetByText($"{next.DayNumber - today.DayNumber} 天后出发")).ToBeVisibleAsync();

        // 标题切到月份视图，点一个月回到日期视图，焦点保留原来的日
        var birthday = page.Locator("[data-rune-calendar][aria-label=\"生日\"]");
        await birthday.GetByRole(AriaRole.Button, new() { Name = "选择月份：1995年6月" }).ClickAsync();
        await Assertions.Expect(birthday.GetByRole(AriaRole.Grid)).ToHaveAttributeAsync("aria-label", "1995年");
        await birthday.Locator("button[data-date=\"1995-09-01\"]").ClickAsync();
        await Assertions.Expect(birthday.Locator("button[data-focused]")).ToHaveAttributeAsync("data-date", "1995-09-18");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 区间_先点开始再点结束_Esc取消(string mode)
    {
        var page = await OpenAsync(mode, "range-calendar");
        var leave = page.Locator("[data-rune-calendar][aria-label=\"请假日期\"]");
        var month = DateOnly.FromDateTime(DateTime.Today) is var t ? new DateOnly(t.Year, t.Month, 1) : default;
        // 这个月的第二个周二到第二个周四
        var tuesday = month.AddDays(((int)DayOfWeek.Tuesday - (int)month.DayOfWeek + 7) % 7 + 7);
        var thursday = tuesday.AddDays(2);

        await leave.Locator($"button[data-date=\"{Iso(tuesday)}\"]").ClickAsync();
        await Assertions.Expect(leave).ToHaveAttributeAsync("data-anchor", Iso(tuesday));
        // 悬停预览在浏览器里画
        await leave.Locator($"button[data-date=\"{Iso(thursday)}\"]").HoverAsync();
        await Assertions.Expect(leave.Locator("td[data-preview]")).ToHaveCountAsync(3);
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(leave).Not.ToHaveAttributeAsync("data-anchor", Iso(tuesday));

        await leave.Locator($"button[data-date=\"{Iso(thursday)}\"]").ClickAsync();
        await leave.Locator($"button[data-date=\"{Iso(tuesday)}\"]").ClickAsync();
        await Assertions.Expect(page.GetByText($"{Iso(tuesday)} 至 {Iso(thursday)}")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("共 3 个工作日")).ToBeVisibleAsync();
        await Assertions.Expect(leave.Locator("td[data-in-range]")).ToHaveCountAsync(3);
    }
}
