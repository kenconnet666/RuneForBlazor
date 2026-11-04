using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class ToastTests : BunitContext
{
    public ToastTests() => Services.AddRuneUI();

    ToastService Toasts => Services.GetRequiredService<ToastService>();

    [Fact]
    public void 显示后区域跟着信号重新渲染_关闭按钮移除()
    {
        var cut = Render<Toaster>();
        Assert.Empty(cut.FindAll("[data-part=toast]"));

        Toasts.Show("已保存", Tone.Success, duration: TimeSpan.FromMinutes(1));

        cut.WaitForAssertion(() => Assert.Equal("已保存", cut.Find("[data-part=description]").TextContent));
        // 每条提示是 Alert，部件标记属于 Toaster（toaster / toast）
        cut.Find("[data-part=toast] [data-part=close]").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-part=toast]")));
    }

    [Fact]
    public void 错误默认不自动消失_并且立即播报()
    {
        var cut = Render<Toaster>();

        Toasts.Show("保存失败", Tone.Danger);

        cut.WaitForAssertion(() => Assert.Equal("alert", cut.Find("[data-part=toast]").GetAttribute("role")));
    }

    [Fact]
    public async Task 到时间自动消失()
    {
        Toasts.Show("已复制", duration: TimeSpan.FromMilliseconds(20));
        Assert.Single(Toasts.Items);

        for (var i = 0; i < 50 && Toasts.Items.Count > 0; i++) await Task.Delay(20, Xunit.TestContext.Current.CancellationToken);

        Assert.Empty(Toasts.Items);
    }

    [Fact]
    public void 超过上限时移走最早的()
    {
        Toasts.MaxVisible = 2;

        Toasts.Show("1");
        Toasts.Show("2");
        Toasts.Show("3");

        Assert.Equal(["2", "3"], Toasts.Items.Select(t => t.Message));
    }
}
