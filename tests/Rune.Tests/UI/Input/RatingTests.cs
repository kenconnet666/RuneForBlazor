using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

// 已选的填充与悬停预览在 CSS / JS 里，这里测单选组的结构、取值与只读的读法
public class RatingTests : BunitContext
{
    public RatingTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static string? Checked(IRenderedComponent<Rating> cut) =>
        cut.FindAll("input[type=radio]").FirstOrDefault(i => i.HasAttribute("checked"))?.GetAttribute("value");

    [Fact]
    public void 可编辑时是单选组_每个分数一个radio_名称是几星_半星时每颗两个()
    {
        var cut = Render<Rating>(p => p.Add(x => x.Value, 2.5).Add(x => x.AllowHalf, true).Add(x => x.Name, "score").AddUnmatched("aria-label", "质量"));

        Assert.Equal("radiogroup", cut.Find("[data-rune-rating]").GetAttribute("role"));
        var radios = cut.FindAll("input[type=radio]");
        Assert.Equal(10, radios.Count);
        Assert.All(radios, r => Assert.Equal("score", r.GetAttribute("name")));
        Assert.Equal(["0.5 星", "1 星"], radios.Take(2).Select(r => r.GetAttribute("aria-label")));
        Assert.Equal(["start", "end"], radios.Take(2).Select(r => r.GetAttribute("data-half")));
        Assert.Equal("2.5", Checked(cut));
        Assert.Equal(["full", "full", "half", "empty", "empty"], cut.FindAll("[data-rune-rating] > span").Select(s => s.GetAttribute("data-state")));
    }

    [Fact]
    public void 点选分数_再点一次清空_不清空时保持()
    {
        var cut = Render<Rating>(p => p.Add(x => x.Value, 3));
        cut.Find("input[value='4']").Click();
        Assert.Equal("4", Checked(cut));
        cut.Find("input[value='4']").Click();
        Assert.Null(Checked(cut));

        var keep = Render<Rating>(p => p.Add(x => x.Value, 3).Add(x => x.Clearable, false));
        keep.Find("input[value='3']").Click();
        Assert.Equal("3", Checked(keep));
    }

    [Fact]
    public void 只读时是一张图_读几星满分几星_填充写在星上()
    {
        var cut = Render<Rating>(p => p.Add(x => x.Value, 3.5).Add(x => x.ReadOnly, true));

        var root = cut.Find("[role=img]");
        Assert.Equal("3.5 星，满分 5 星", root.GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("input"));
        Assert.Equal(["100%", "100%", "100%", "50%", "0%"],
            cut.FindAll("[role=img] > span").Select(s => s.GetAttribute("style")!.Split(':')[1].TrimEnd(';')));

        var empty = Render<Rating>(p => p.Add(x => x.Value, null).Add(x => x.ReadOnly, true));
        Assert.Equal("未评分", empty.Find("[role=img]").GetAttribute("aria-label"));
    }

    [Fact]
    public void 只读时名称里总有分数_自己的名称或Field的标签后面接上分数()
    {
        // 自己写了 aria-label：名称后面接上分数（之前被分数盖掉）
        var labelled = Render<Rating>(p => p.Add(x => x.Value, 4).Add(x => x.ReadOnly, true).AddUnmatched("aria-label", "服务态度"));
        Assert.Equal("服务态度 4 星，满分 5 星", labelled.Find("[role=img]").GetAttribute("aria-label"));

        // 在 Field 里：名称指向标签与一个隐藏的分数文字（之前只念标签，听不到分数）
        var cut = Render<Field>(p => p.Add(x => x.Label, "交期").AddChildContent<Rating>(r => r.Add(x => x.Value, 2).Add(x => x.ReadOnly, true)));
        var image = cut.Find("[role=img]");
        Assert.Null(image.GetAttribute("aria-label"));
        var ids = image.GetAttribute("aria-labelledby")!.Split(' ');
        Assert.Equal(["交期", "2 星，满分 5 星"], ids.Select(id => cut.Find($"#{id}").TextContent.Trim()));
    }

    [Fact]
    public void 在Field里以标签为名称_不可用时radio不可用_点了不变()
    {
        var cut = Render<Field>(p => p.Add(x => x.Label, "交期").Add(x => x.Disabled, true).AddChildContent<Rating>(r => r.Add(x => x.Value, 2)));

        var group = cut.Find("[role=radiogroup]");
        Assert.Equal("交期", cut.Find($"#{group.GetAttribute("aria-labelledby")}").TextContent.Trim());
        Assert.True(group.HasAttribute("data-disabled"));
        Assert.All(cut.FindAll("input[type=radio]"), r => Assert.True(r.HasAttribute("disabled")));
        cut.Find("input[value='5']").Click();
        Assert.Equal("2", cut.FindAll("input[type=radio]").First(i => i.HasAttribute("checked")).GetAttribute("value"));
    }
}
