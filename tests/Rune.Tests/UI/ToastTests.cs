using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>通知：ToastService（控制器）与 Toaster（显示区域）。</summary>
public class ToastTests : BunitContext
{
    public ToastTests()
    {
        Services.AddRuneUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    ToastService Toasts => Services.GetRequiredService<ToastService>();

    static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(20, Token);
    }

    [Fact]
    public void 显示后区域跟着信号重新渲染_关闭按钮关掉()
    {
        var cut = Render<Toaster>();
        Assert.Empty(cut.FindAll("[data-rune-toast]"));

        Toasts.Show(new() { Tone = Tone.Success, Title = "已保存", Description = "改动已同步", Duration = TimeSpan.FromMinutes(1) });

        cut.WaitForAssertion(() => Assert.Equal("改动已同步", cut.Find("[data-part=description]").TextContent));
        var close = cut.Find("[data-rune-toast] [data-part=close]");
        Assert.True(close.HasAttribute("data-rune-toast-close"));
        cut.Find("[data-part=toast]").TriggerEvent("onrunedismiss", EventArgs.Empty);
        cut.WaitForAssertion(() => Assert.Equal("closing", cut.Find("[data-rune-toast]").GetAttribute("data-state")));
    }

    [Fact]
    public void 出错的读屏立即播报_JS发来dismiss时关闭()
    {
        var cut = Render<Toaster>();

        var id = Toasts.Error("同步失败");

        cut.WaitForAssertion(() => Assert.Equal("assertive", cut.Find("[data-rune-toast]").GetAttribute("aria-live")));
        cut.Find("[data-rune-toast]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "dismiss" });
        Assert.True(Toasts.Items.Single(t => t.Id == id).Closing);
    }

    [Fact]
    public async Task 到时间自动消失_关闭原因是Timeout()
    {
        ToastCloseReason? reason = null;
        Toasts.Show(new() { Title = "已复制", Duration = TimeSpan.FromMilliseconds(20), OnClose = r => reason = r });
        Assert.Single(Toasts.Items);

        await WaitUntil(() => Toasts.Items.Count == 0);

        Assert.Empty(Toasts.Items);
        Assert.Equal(ToastCloseReason.Timeout, reason);
    }

    [Fact]
    public async Task 同一个Id只留一条_换掉内容并重新计时()
    {
        Toasts.Loading("正在导出", new() { Id = "export" });
        Assert.Null(Toasts.Items.Single().Countdown);
        Assert.False(Toasts.Items.Single().Options.Closable);

        Toasts.Success("已导出", new() { Id = "export", Duration = TimeSpan.FromMinutes(1) });

        var toast = Assert.Single(Toasts.Items);
        Assert.Equal(("export", "已导出", Tone.Success, false), (toast.Id, toast.Options.Title, toast.Options.Tone, toast.Options.Loading));
        Assert.NotNull(toast.Countdown);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task 只给显示出来的计时_暂停时都停()
    {
        Toasts.Limit = 1;
        Toasts.Show(new() { Title = "早的", Duration = TimeSpan.FromMilliseconds(30) });
        Toasts.Show(new() { Title = "新的", Duration = TimeSpan.FromMinutes(1) });

        // 早的那条没显示出来：不计时
        await Task.Delay(120, Token);
        Assert.Equal(2, Toasts.Items.Count(t => !t.Closing));

        Toasts.Limit = 5;
        Toasts.Pause();
        await Task.Delay(120, Token);
        Assert.Equal(2, Toasts.Items.Count(t => !t.Closing));

        Toasts.Resume();
        await WaitUntil(() => Toasts.Items.Count == 1);
        Assert.Equal("新的", Toasts.Items.Single().Options.Title);
    }

    [Fact]
    public async Task 跟踪任务_成功与失败依次换掉同一条()
    {
        var done = await Toasts.PromiseAsync(Task.FromResult("report.pdf"), new(
            new() { Title = "正在上传" }, name => new() { Title = $"{name} 上传完成" }, e => new() { Title = e.Message }));
        Assert.Equal("report.pdf", done);
        var success = Assert.Single(Toasts.Items);
        Assert.Equal(("report.pdf 上传完成", Tone.Success, true), (success.Options.Title, success.Options.Tone, success.Options.Closable));

        await Assert.ThrowsAsync<IOException>(() => Toasts.PromiseAsync(Task.FromException<string>(new IOException("网络中断")), new(
            new() { Title = "正在上传" }, name => new() { Title = name }, e => new() { Title = e.Message })));
        Assert.Contains(Toasts.Items, t => t.Options is { Title: "网络中断", Tone: Tone.Danger });
    }

    [Fact]
    public void 只显示最新的Max条_位置与叠放()
    {
        var cut = Render<Toaster>(p => p.Add(x => x.Max, 2).Add(x => x.Stack, true).Add(x => x.Placement, ToastPlacement.TopEnd));

        foreach (var title in new[] { "一", "二", "三" }) Toasts.Show(new() { Title = title, Duration = TimeSpan.FromMinutes(1) });

        cut.WaitForAssertion(() => Assert.Equal(["二", "三"], cut.FindAll("[data-part=title]").Select(t => t.TextContent.Trim())));
        Assert.Equal("false", cut.Find("[data-rune-toaster]").GetAttribute("data-expanded"));
        // 叠放：更早的一条往后错开、缩小
        Assert.Contains("scale:0.95", cut.FindAll("[data-rune-toast]")[0].GetAttribute("style"));
    }
}
