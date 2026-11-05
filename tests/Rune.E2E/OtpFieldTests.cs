namespace Rune.E2E;

/// <summary>验证码：一个透明的 input 盖在格子上，JS 按它的当前值画格子；快速输入、粘贴不丢字。</summary>
public class OtpFieldTests(DemoServer demo)
{
    static Task<string> Slots(ILocator input) => input.EvaluateAsync<string>(
        "input => [...input.parentElement.querySelectorAll('[data-index]')].map(s => s.dataset.char || '_').join('')");

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 快速输入只留数字_填满自动校验_错了清空_对了登录(string mode)
    {
        var page = await demo.OpenAsync(mode, "otp-field");
        var input = page.GetByTestId("sms-code");
        var message = page.GetByTestId("sms-message");

        await input.FocusAsync();
        await page.Keyboard.TypeAsync("1a2b3");
        await Assertions.Expect(input).ToHaveValueAsync("123");
        Assert.Equal("123___", await Slots(input));

        await page.Keyboard.TypeAsync("456");
        // 校验期间不可用，错了清空并标红
        await Assertions.Expect(input).ToHaveValueAsync("");
        await Assertions.Expect(page.GetByText("验证码不对，请重新输入")).ToBeVisibleAsync();

        await input.FillAsync("246810");
        await Assertions.Expect(message).ToHaveTextAsync("登录成功");
    }

    [Fact]
    public async Task 静态SSR下过滤并画格子_遮罩显示圆点_分组()
    {
        var page = await demo.OpenAsync("static", "otp-field");
        var pin = page.GetByTestId("pin");
        var recovery = page.GetByTestId("recovery");

        await pin.FocusAsync();
        await page.Keyboard.TypeAsync("12x34");
        await Assertions.Expect(pin).ToHaveValueAsync("1234");
        Assert.Equal("••••__", await Slots(pin));

        await recovery.FillAsync("ab-12cd34ef");
        await Assertions.Expect(recovery).ToHaveValueAsync("AB12CD34");
        Assert.Equal("AB12CD34", await Slots(recovery));
    }
}
