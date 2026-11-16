using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 拼图滑块验证（同 vue-tsx-pro 的 SlideCaptcha）：还没拿到图时加载中、滑块不可用；交给 JS 的尺寸与状态；
/// JS 交来的松手结果原样报告；判定中、通过后滑块离开 Tab 顺序，结果经实时区域念出。
/// </summary>
public class SlideCaptchaTests : BunitContext
{
    public SlideCaptchaTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly SlideChallenge Challenge = new("c1", "/bg.png", "/piece.png", 320, 160, 57);

    [Fact]
    public void 没有图时加载中_滑块不可用()
    {
        var cut = Render<SlideCaptcha>();

        Assert.Contains(Lang.ZhCn.Patterns.SlideLoading, cut.Markup);
        var handle = cut.Find("[role=\"slider\"]");
        Assert.Equal("-1", handle.GetAttribute("tabindex"));
        Assert.Equal("true", handle.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void 有图时交给JS尺寸与状态_拼图宽度按比例()
    {
        var cut = Render<SlideCaptcha>(p => p.Add(x => x.Challenge, Challenge));

        var root = cut.Find("[data-rune-slide-captcha]");
        Assert.Equal("idle", root.GetAttribute("data-status"));
        Assert.Equal("c1", root.GetAttribute("data-challenge"));
        Assert.Equal("263", root.GetAttribute("data-travel"));
        Assert.Contains("--rune-slide-piece: 17.8125%", cut.Find("[data-part=\"picture\"]").GetAttribute("style"));
        var handle = cut.Find("[role=\"slider\"]");
        Assert.Equal("0", handle.GetAttribute("tabindex"));
        Assert.Equal("263", handle.GetAttribute("aria-valuemax"));
        Assert.Contains(Lang.ZhCn.Patterns.SlideHint, cut.Find("[data-rune-slide-hint]").TextContent);
    }

    [Fact]
    public async Task 松手的结果原样报告_判定中与通过后锁定并念出结果()
    {
        SlideResult? released = null;
        var cut = Render<SlideCaptcha>(p => p
            .Add(x => x.Challenge, Challenge)
            .Add(x => x.OnRelease, r => released = r));

        await cut.InvokeAsync(() => cut.Find("[data-rune-slide-captcha]").TriggerEvent("onrunecommand",
            new CommandEventArgs { Name = "release", Text = """{"x":120,"track":[{"t":0,"x":0},{"t":300,"x":120}],"duration":420}""" }));
        Assert.Equal(120, released?.X);
        Assert.Equal(420, released?.Duration);
        Assert.Equal([new SlidePoint(0, 0), new SlidePoint(300, 120)], released?.Track);

        cut.Render(p => p.Add(x => x.Status, SlideStatus.Verifying));
        Assert.Equal("true", cut.Find("[data-rune-slide-captcha]").GetAttribute("aria-busy"));
        Assert.Equal("-1", cut.Find("[role=\"slider\"]").GetAttribute("tabindex"));
        Assert.True(cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == Lang.ZhCn.Patterns.SlideRefresh).HasAttribute("disabled"));

        cut.Render(p => p.Add(x => x.Status, SlideStatus.Error).Add(x => x.ErrorText, "轨迹不像真人"));
        Assert.Equal("error", cut.Find("[data-rune-slide-captcha]").GetAttribute("data-status"));
        Assert.Equal("轨迹不像真人", cut.Find("[data-part=\"banner\"]").TextContent.Trim());
        Assert.Contains("轨迹不像真人", cut.Find("[aria-live=\"polite\"]").TextContent);
    }
}
