using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class AvatarTests : BunitContext
{
    public AvatarTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Theory]
    [InlineData("林小满", "满")]
    [InlineData("Zhao Qing", "ZQ")]
    [InlineData("ada", "A")]
    [InlineData("  ", "")]
    public void 首字母_中文取最后一个字_其他取前后两个词的首字母(string name, string expected) =>
        Assert.Equal(expected, AvatarStyles.Initials(name));

    [Fact]
    public void 有名字时是一张图_图片是装饰_没有名字时对读屏隐藏()
    {
        var cut = Render<Avatar>(p => p.Add(x => x.Name, "陈默").Add(x => x.Src, "/a.png"));
        var root = cut.Find("span");
        Assert.Equal(("img", "陈默"), (root.GetAttribute("role"), root.GetAttribute("aria-label")));
        Assert.Equal("", cut.Find("img").GetAttribute("alt"));
        Assert.True(cut.Find("img").HasAttribute("data-rune-avatar-image"));
        Assert.Equal("默", cut.Find("[aria-hidden=true]").TextContent.Trim());

        var anonymous = Render<Avatar>();
        Assert.Equal("true", anonymous.Find("span").GetAttribute("aria-hidden"));
        Assert.NotNull(anonymous.Find("svg[data-icon=user]"));
    }

    [Fact]
    public void 头像组_超过Max折叠成加N_有Total时按它算()
    {
        string[] names = ["甲", "乙", "丙", "丁", "戊"];
        RenderFragment<string> avatar = name => builder =>
        {
            builder.OpenComponent<Avatar>(0);
            builder.AddAttribute(1, nameof(Avatar.Name), name);
            builder.CloseComponent();
        };
        var cut = Render<AvatarGroup<string>>(p => p.Add(x => x.Items, names).Add(x => x.Max, 3).Add(x => x.ChildContent, avatar));
        Assert.Equal(3, cut.FindAll("[role=group] > span[aria-label]").Count(s => s.GetAttribute("aria-label")!.Length == 1));
        Assert.Equal("还有 2 人", cut.Find("[role=group] > span:last-child").GetAttribute("aria-label"));

        var partial = Render<AvatarGroup<string>>(p => p.Add(x => x.Items, names[..3]).Add(x => x.Total, 128).Add(x => x.ChildContent, avatar));
        Assert.Equal("+125", partial.Find("[role=group] > span:last-child").TextContent.Trim());
    }

    [Fact]
    public void 角标_超过上限显示加号_0时不显示_圆点只有给读屏的说明()
    {
        Assert.Equal("99+", Render<Indicator>(p => p.Add(x => x.Count, 120)).Find("[aria-hidden=true]").TextContent);
        Assert.Single(Render<Indicator>(p => p.Add(x => x.Count, 0)).FindAll("span"));
        Assert.Contains("0 条新消息", Render<Indicator>(p => p.Add(x => x.Count, 0).Add(x => x.ShowZero, true)).Markup);

        var dot = Render<Indicator>(p => p.Add(x => x.Dot, true).Add(x => x.Label, "在线").Add(x => x.Placement, IndicatorPlacement.BottomEnd));
        Assert.Equal("bottom-end", dot.Find("[data-placement]").GetAttribute("data-placement"));
        Assert.Contains("在线", dot.Markup);
        Assert.Empty(dot.FindAll("[aria-hidden=true]"));
    }
}
