using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class ProgressTests : BunitContext
{
    public ProgressTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 条形_值夹在范围内_读屏念百分比与目标_标签是名称()
    {
        var cut = Render<Progress>(p => p.Add(x => x.Value, 120).Add(x => x.Target, 80).Add(x => x.Label, "完成率"));
        var bar = cut.Find("[role=progressbar]");
        Assert.Equal(("100", "100"), (bar.GetAttribute("aria-valuenow"), bar.GetAttribute("aria-valuemax")));
        Assert.Contains("目标", bar.GetAttribute("aria-valuetext"));
        Assert.Equal("complete", bar.GetAttribute("data-state"));
        Assert.Equal("完成率", cut.Find($"#{bar.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Contains("--rune-progress-value:100%", cut.Markup.Replace(" ", ""));
        Assert.Contains("--rune-progress-target:80%", cut.Markup.Replace(" ", ""));
    }

    [Fact]
    public void 不确定进度没有当前值_只有进度条时不渲染标签行()
    {
        var cut = Render<Progress>(p => p.AddUnmatched("aria-label", "加载中"));
        var bar = cut.Find("[role=progressbar]");
        Assert.Null(bar.GetAttribute("aria-valuenow"));
        Assert.Equal(("indeterminate", "加载中"), (bar.GetAttribute("data-state"), bar.GetAttribute("aria-label")));
        Assert.Empty(cut.FindAll("[aria-hidden=true]"));
    }

    [Fact]
    public void 环形_中间显示数值_自定义读屏文字()
    {
        var cut = Render<Progress>(p => p.Add(x => x.Shape, ProgressShape.Circle).Add(x => x.Value, 3).Add(x => x.Max, 8)
            .Add(x => x.ValueText, (v, m) => $"{v} / {m}").Add(x => x.ValueLabel, "3 个"));
        var ring = cut.Find("[role=progressbar]");
        Assert.Equal("3 / 8", ring.GetAttribute("aria-valuetext"));
        Assert.Equal("3 个", cut.Find("[role=progressbar] > [aria-hidden=true]:not(svg)").TextContent);
        Assert.Equal(2, cut.FindAll("circle").Count);
    }

    [Fact]
    public void 状态色点_有含义时是一张图_否则装饰_任意颜色经变量()
    {
        var label = Render<StatusDot>(p => p.Add(x => x.Label, "未读"));
        Assert.Equal(("img", "未读"), (label.Find("span").GetAttribute("role"), label.Find("span").GetAttribute("aria-label")));
        Assert.Equal("true", Render<StatusDot>().Find("span").GetAttribute("aria-hidden"));
        var custom = Render<StatusDot>(p => p.Add(x => x.Color, "#ff6600"));
        Assert.Contains("--rune-status-dot-color:#ff6600", custom.Markup.Replace(" ", ""));
        Assert.Null(custom.Find("span").GetAttribute("data-tone"));
    }

    [Fact]
    public void 加载遮罩_一开始就在加载时立即盖上_开始加载时aria_busy立即生效而遮罩推迟出现()
    {
        var cut = Render<LoadingOverlay>(p => p.Add(x => x.Loading, true).Add(x => x.Label, "正在保存").AddChildContent("<p>表单</p>"));
        Assert.Equal("true", cut.Find("div").GetAttribute("aria-busy"));
        Assert.False(cut.Find("[data-part=overlay]").HasAttribute("hidden"));
        Assert.Equal("正在保存", cut.Find("[role=status]").TextContent.Trim());

        var later = Render<LoadingOverlay>(p => p.AddChildContent("<p>表单</p>"));
        Assert.False(later.Find("div").HasAttribute("aria-busy"));
        later.Render(p => p.Add(x => x.Loading, true));
        Assert.Equal("true", later.Find("div").GetAttribute("aria-busy"));
        Assert.True(later.Find("[data-part=overlay]").HasAttribute("hidden"));
        later.WaitForAssertion(() => Assert.False(later.Find("[data-part=overlay]").HasAttribute("hidden")));
    }
}
