using System.Text.RegularExpressions;

namespace Rune.E2E;

/// <summary>时间显示：同一时刻的几种写法跟着语言变；相对时间换详略；验证码按钮倒数时禁用。</summary>
public class DateTimeTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 写法_相对时间_倒计时(string mode)
    {
        var page = await OpenAsync(mode, "date-time");

        var formats = page.GetByRole(AriaRole.Table, new() { Name = "日期时间的写法" }).Locator("tbody tr");
        await Assertions.Expect(formats.Nth(0).Locator("td")).ToHaveTextAsync(["2026年10月20日 14:30", "Oct 20, 2026, 2:30 PM"]);
        await Assertions.Expect(formats.Nth(2).Locator("td")).ToHaveTextAsync(["2026年10月20日星期二 14:30", "Tuesday, October 20, 2026 at 2:30 PM"]);
        await Assertions.Expect(formats.Nth(4).Locator("time").First).ToHaveAttributeAsync("datetime", "14:30:00");

        // 一周以前的直接写日期，近的写相对时间；Narrow 是最短的写法
        var times = page.Locator("li time");
        await Assertions.Expect(times.Nth(1)).ToHaveTextAsync("7分钟前");
        await Assertions.Expect(times.Nth(4)).ToHaveTextAsync(new Regex(@"^\d{4}年\d{1,2}月\d{1,2}日$"));
        await page.GetByRole(AriaRole.Group, new() { Name = "详略" }).GetByRole(AriaRole.Button, new() { Name = "Narrow" }).ClickAsync();
        await Assertions.Expect(times.Nth(3)).ToHaveTextAsync("昨天");
        await Assertions.Expect(times.Nth(2)).ToHaveTextAsync("3小时前");

        var send = page.GetByRole(AriaRole.Button, new() { Name = "发送验证码" });
        await send.ClickAsync();
        // 计时器（role=timer）的数字不算进按钮的名字
        var resend = page.GetByRole(AriaRole.Button, new() { Name = "秒后重发" });
        await Assertions.Expect(resend).ToBeDisabledAsync();
        await Assertions.Expect(resend.GetByRole(AriaRole.Timer)).ToHaveTextAsync(new Regex(@"^\d+$"));
        await Assertions.Expect(page.GetByRole(AriaRole.Timer, new() { Name = "距抢购开始" })).ToContainTextAsync(":");

        // API 表的说明来自 Rune.UI.Dates 自己的 XML 文档注释
        await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = "自定义显示：拿到拆好的天、时、分、秒。" })).ToBeVisibleAsync();
    }
}
