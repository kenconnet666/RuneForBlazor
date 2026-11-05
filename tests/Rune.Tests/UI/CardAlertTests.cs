using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class CardAlertTests : BunitContext
{
    public CardAlertTests() => Services.AddRuneUI();

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    string RuleOf(string className) => Assert.Single(Registry.Rules, rule => rule.Contains($".{className}{{"));

    [Fact]
    public void 卡片只渲染给了的分区()
    {
        var plain = Render<Card>(p => p.AddChildContent("正文"));
        var full = Render<Card>(p => p
            .Add(x => x.Header, "订单 #1024")
            .Add(x => x.Extra, "<a href=\"/all\">全部</a>")
            .Add(x => x.Footer, "<button>查看</button>")
            .AddChildContent("正文"));

        Assert.Equal(["root", "body"], plain.FindAll("[data-part]").Select(e => e.GetAttribute("data-part")));
        Assert.Equal(["root", "header", "body", "footer"], full.FindAll("[data-part]").Select(e => e.GetAttribute("data-part")));
        Assert.Equal("订单 #1024全部", full.Find("[data-part=header]").TextContent);
    }

    [Fact]
    public void 有装饰效果时外面多一层_属性落在外层_光层对读屏隐藏()
    {
        var spotlight = Render<Card>(p => p.Add(x => x.Effect, CardEffect.Spotlight).AddUnmatched("id", "feature").AddChildContent("正文"));
        var beam = Render<Card>(p => p.Add(x => x.Effect, CardEffect.Beam).AddChildContent("正文"));

        var frame = spotlight.Find("[data-part=frame]");
        Assert.Equal(("feature", "sm"), (frame.Id, frame.GetAttribute("data-rune-pointer")));
        Assert.Equal(["frame", "root", "body", "effect", "ring"], spotlight.FindAll("[data-part]").Select(e => e.GetAttribute("data-part")));
        Assert.All(spotlight.FindAll("[data-part=effect],[data-part=ring]"), layer => Assert.Equal("true", layer.GetAttribute("aria-hidden")));
        // 流光不跟指针：没有 data-rune-pointer，光带在轨道里
        Assert.False(beam.Find("[data-part=frame]").HasAttribute("data-rune-pointer"));
        Assert.NotNull(beam.Find("[data-part=ring] > [data-part=beam]"));
    }

    [Fact]
    public void 有Extra时页头两端对齐()
    {
        var withExtra = Render<Card>(p => p.Add(x => x.Header, "收货地址").Add(x => x.Extra, "<a href=\"#\">修改</a>"));
        var plain = Render<Card>(p => p.Add(x => x.Header, "收货地址"));

        Assert.Contains("justify-content:space-between", RuleOf(withExtra.Find("[data-part=header]").ClassName!));
        Assert.DoesNotContain("space-between", RuleOf(plain.Find("[data-part=header]").ClassName!));
    }

    [Fact]
    public void 卡片的默认值可以配置()
    {
        Services.AddRuneUI(o => o.Defaults = new() { new CardDefaults { Variant = CardVariant.Elevated } });

        var cut = Render<Card>(p => p.AddChildContent("正文"));

        Assert.Contains("box-shadow:var(--rune-shadows-md)", RuleOf(cut.Find("[data-scope=card]").ClassName!));
    }

    [Fact]
    public void 提示条按色调选图标与颜色()
    {
        var cut = Render<Alert>(p => p.Add(x => x.Tone, Tone.Warning).Add(x => x.Title, "即将过期").AddChildContent("3 天后到期"));

        Assert.Contains("background-color:var(--rune-tones-warning-soft)", RuleOf(cut.Find("[data-scope=alert]").ClassName!));
        Assert.Equal(BuiltInIcons.TriangleExclamationPoint, Tones.StatusIcon(Tone.Warning));
        Assert.Equal("即将过期", cut.Find("[data-part=title]").TextContent);
        Assert.Equal("3 天后到期", cut.Find("[data-part=description]").TextContent);
        // 页面加载时就在的提示不主动播报
        Assert.Null(cut.Find("[data-scope=alert]").GetAttribute("role"));
    }

    [Fact]
    public void 提示条的播报方式与加载中()
    {
        var cut = Render<Alert>(p => p.Add(x => x.Live, AlertLive.Assertive).Add(x => x.Loading, true).AddChildContent("正在上传"));

        var root = cut.Find("[data-scope=alert]");
        Assert.Equal("alert", root.GetAttribute("role"));
        Assert.Equal("true", root.GetAttribute("aria-busy"));
        // 图标位置换成加载指示（Lucide 的 loader-circle）；部件标记属于提示条
        Assert.Equal("loader-circle", cut.Find("svg[data-part=icon]").GetAttribute("data-icon"));
    }

    [Fact]
    public void 提示条给了关闭回调才有关闭按钮()
    {
        var closed = 0;
        var plain = Render<Alert>(p => p.AddChildContent("说明"));
        var closable = Render<Alert>(p => p
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closed++))
            .AddChildContent("说明"));

        Assert.Empty(plain.FindAll("button"));
        var button = closable.Find("button");
        Assert.Equal("关闭", button.GetAttribute("aria-label"));
        Assert.True(button.HasAttribute("data-rune-dismiss"));
        // 淡出结束后 JS 初始化器在提示条上发出 rune-dismiss
        closable.Find("[data-rune-dismissible]").TriggerEvent("onrunedismiss", EventArgs.Empty);
        Assert.Equal(1, closed);
    }

}
