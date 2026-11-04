using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.Components;

public class ButtonTests : BunitContext
{
    public ButtonTests() => Services.AddRuneUI();

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    [Fact]
    public void 点击触发使用方的处理()
    {
        var clicks = 0;
        var cut = Render<Button>(p => p
            .AddChildContent("保存")
            .AddUnmatched("onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        cut.Find("button").Click();

        Assert.Equal(1, clicks);
        Assert.Equal("button", cut.Find("button").GetAttribute("type"));
    }

    [Fact]
    public void 加载中可聚焦但点击无效_文字保留()
    {
        var clicks = 0;
        var cut = Render<Button>(p => p
            .Add(x => x.Loading, true)
            .AddChildContent("保存")
            .AddUnmatched("onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        var button = cut.Find("button");
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-busy"));
        Assert.Equal("true", button.GetAttribute("aria-disabled"));
        Assert.Contains("保存", button.TextContent);
        Assert.NotNull(cut.Find("[data-part=spinner] svg"));
        Assert.Throws<MissingEventHandlerException>(() => button.Click());
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void 不可用时是原生disabled()
    {
        var cut = Render<Button>(p => p.Add(x => x.Disabled, true).AddChildContent("删除"));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void 样式与色调决定颜色token()
    {
        Render<Button>(p => p.Add(x => x.Variant, ButtonVariant.Solid).Add(x => x.Tone, Tone.Danger).AddChildContent("删除"));

        Assert.Contains(Registry.Rules, rule => rule.Contains("background-color:var(--rune-tones-danger-solid)")
                                                && rule.Contains("""hover:not(:disabled,[aria-disabled="true"]){background-color:var(--rune-tones-danger-solid-hover)"""));
    }

    [Fact]
    public void 配置的默认值作用于没写参数的按钮()
    {
        var cut = Render<RuneProvider>(p => p
            .Add(x => x.Defaults, new ComponentDefaults { new ButtonDefaults { Variant = ButtonVariant.Outline, Tone = Tone.Neutral } })
            .AddChildContent<Button>(b => b.AddChildContent("取消")));

        var className = cut.Find("button").ClassName;
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{className}{{") && rule.Contains("border-color:var(--rune-tones-neutral-border)"));
    }

    [Fact]
    public void 图标按钮用Label作为名称_图标对读屏隐藏()
    {
        var cut = Render<IconButton>(p => p.Add(x => x.Icon, BuiltInIcons.X).Add(x => x.Label, "关闭"));

        var button = cut.Find("button");
        Assert.Equal("关闭", button.GetAttribute("aria-label"));
        Assert.Equal("true", cut.Find("svg").GetAttribute("aria-hidden"));
        Assert.Equal("x", cut.Find("svg").GetAttribute("data-icon"));
    }

    [Fact]
    public void 链接按钮不可用时去掉href()
    {
        var enabled = Render<LinkButton>(p => p.Add(x => x.Href, "/orders").AddChildContent("订单"));
        var disabled = Render<LinkButton>(p => p.Add(x => x.Href, "/orders").Add(x => x.Disabled, true).AddChildContent("订单"));

        Assert.Equal("/orders", enabled.Find("a").GetAttribute("href"));
        Assert.False(disabled.Find("a").HasAttribute("href"));
        Assert.Equal("true", disabled.Find("a").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void 部件是组件时样式作为Sx交下去_只合成一个类()
    {
        var cut = Render<Button>(p => p
            .Add(x => x.Icon, BuiltInIcons.Plus)
            .Add(x => x.PropsIcon, new Attrs { Sx = new Style { Color = "crimson" } })
            .AddChildContent("新建"));

        var svg = cut.Find("svg");
        Assert.Single(svg.ClassList);
        Assert.Equal("button", svg.GetAttribute("data-scope"));
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{svg.ClassName}{{") && rule.Contains("color:crimson") && rule.Contains("width:1em"));
    }

    [Fact]
    public void 加载指示_Lucide图标旋转_有说明时播报()
    {
        var cut = Render<Spinner>(p => p.Add(x => x.Label, "加载中"));

        var svg = cut.Find("svg");
        Assert.Equal("loader-circle", svg.GetAttribute("data-icon"));
        // Icon 有名称时默认 role="img"，加载指示让给 status
        Assert.Equal("status", svg.GetAttribute("role"));
        Assert.Equal("加载中", svg.GetAttribute("aria-label"));
        Assert.Single(svg.ClassList);
        Assert.Contains(Registry.Rules, rule => rule.Contains("@keyframes rk") && rule.Contains("animation-name:rk"));
    }
}
