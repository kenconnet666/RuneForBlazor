using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class ButtonTests : BunitContext
{
    public ButtonTests() => Services.AddRuneUI();

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    [Fact]
    public void 无外观的按钮_默认type为button_有波纹与使用方的样式()
    {
        var clicks = 0;
        var cut = Render<ButtonBase>(p => p
            .AddChildContent("套餐")
            .Add(x => x.Sx, new Style { Padding = Space.Md })
            .AddUnmatched("aria-pressed", "true")
            .AddUnmatched("onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        var button = cut.Find("button");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("true", button.GetAttribute("aria-pressed"));
        Assert.True(button.HasAttribute("data-rune-ripple"));
        // 组件自己的一段、使用方的一段
        Assert.Equal(2, button.ClassList.Length);
        button.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void 无外观的按钮_不可用与加载中的状态属性_点击不触发()
    {
        var clicks = 0;
        var click = EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++);

        var disabled = Render<ButtonBase>(p => p.Add(x => x.Disabled, true).AddUnmatched("onclick", click));
        Assert.True(disabled.Find("button").HasAttribute("disabled"));
        Assert.True(disabled.Find("button").HasAttribute("data-disabled"));

        var focusable = Render<ButtonBase>(p => p.Add(x => x.Disabled, true).Add(x => x.FocusableWhenDisabled, true).AddUnmatched("onclick", click));
        Assert.False(focusable.Find("button").HasAttribute("disabled"));
        Assert.Equal("true", focusable.Find("button").GetAttribute("aria-disabled"));
        Assert.Throws<MissingEventHandlerException>(() => focusable.Find("button").Click());

        var loading = Render<ButtonBase>(p => p.Add(x => x.Loading, true).Add(x => x.Ripple, false).AddUnmatched("onclick", click));
        Assert.Equal("true", loading.Find("button").GetAttribute("aria-busy"));
        Assert.True(loading.Find("button").HasAttribute("data-rune-inert"));
        Assert.False(loading.Find("button").HasAttribute("data-rune-ripple"));
        Assert.Throws<MissingEventHandlerException>(() => loading.Find("button").Click());
        Assert.Equal(0, clicks);
    }

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
        Assert.False(button.HasAttribute("aria-disabled"));
        Assert.True(button.HasAttribute("data-rune-inert"));
        Assert.Contains("保存", button.TextContent);
        Assert.NotNull(cut.Find("[data-part=spinner] svg"));
        Assert.Throws<MissingEventHandlerException>(() => button.Click());
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void 可聚焦的不可用用aria_disabled_点击无效()
    {
        var clicks = 0;
        var cut = Render<Button>(p => p
            .Add(x => x.Disabled, true)
            .Add(x => x.FocusableWhenDisabled, true)
            .AddChildContent("提交")
            .AddUnmatched("onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        var button = cut.Find("button");
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-disabled"));
        Assert.True(button.HasAttribute("data-rune-inert"));
        Assert.Throws<MissingEventHandlerException>(() => button.Click());
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void 外部链接按钮新窗口打开并提示读屏()
    {
        var cut = Render<LinkButton>(p => p.Add(x => x.Href, "https://vuejs.org").Add(x => x.External, true).AddChildContent("Vue 文档"));

        var link = cut.Find("a");
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
        Assert.Contains("在新窗口打开", link.TextContent);
        Assert.NotNull(cut.Find("[data-part=iconEnd]"));
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
                                                && rule.Contains("""hover:where(:not(:disabled,[aria-disabled="true"],[data-disabled])){background-color:var(--rune-tones-danger-solid-hover)"""));
    }

    [Fact]
    public void 配置的默认值作用于没写参数的按钮()
    {
        var cut = Render<RuneProvider>(p => p
            .Add(x => x.Components, new ComponentConfigs { new ComponentConfig<ButtonDefaults, ButtonPart> { Defaults = new() { Variant = ButtonVariant.Outline, Tone = Tone.Neutral } } })
            .AddChildContent<Button>(b => b.AddChildContent("取消")));

        var className = cut.Find("button").ClassName;
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{className}{{") && rule.Contains("border-color:var(--rune-colors-border)"));
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
    public void 部件是组件时样式作为Sx交下去_按来源分段_使用方的在最后一层()
    {
        var cut = Render<Button>(p => p
            .Add(x => x.Icon, BuiltInIcons.Plus)
            .Add(x => x.PropsIcon, new Attrs { Sx = new Style { Color = "crimson" } })
            .AddChildContent("新建"));

        var svg = cut.Find("svg");
        var classes = svg.ClassList.ToArray();
        Assert.Equal("button", svg.GetAttribute("data-scope"));
        // 图标自己的一段在最低层；使用方的颜色在后面的层里，盖过前面的
        Assert.Contains(Registry.Rules, rule => rule.StartsWith("@layer rune.s0{") && rule.Contains($".{classes[0]}{{") && rule.Contains("width:1em"));
        Assert.Contains(Registry.Rules, rule => !rule.StartsWith("@layer rune.s0{") && rule.Contains($".{classes[^1]}{{") && rule.Contains("color:crimson"));
    }

    [Fact]
    public void 加载指示_Lucide图标旋转_有说明时外层播报()
    {
        var cut = Render<Spinner>(p => p.Add(x => x.Label, "加载中"));

        // 说明放在外层 status 里的视觉隐藏文字中，图标本身是装饰
        var root = cut.Find("span");
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Equal("加载中", root.TextContent.Trim());
        var svg = cut.Find("svg");
        Assert.Equal("loader-circle", svg.GetAttribute("data-icon"));
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Contains(Registry.Rules, rule => svg.ClassList.Any(c => rule.Contains($".{c}{{")) && rule.Contains("@keyframes rk") && rule.Contains("animation-name:rk"));
    }

    [Fact]
    public void 复制按钮把文字交给JS_复制后由JS发出事件()
    {
        bool? copied = null;
        var cut = Render<CopyButton>(p => p
            .Add(x => x.Value, "sk-123")
            .Add(x => x.OnCopied, EventCallback.Factory.Create<bool>(this, ok => copied = ok)));

        var button = cut.Find("button");
        Assert.Equal("sk-123", button.GetAttribute("data-rune-copy"));
        Assert.True(button.HasAttribute("data-rune-copy-on-click"));
        Assert.Equal("复制", button.GetAttribute("aria-label"));
        button.TriggerEvent("onrunecopied", new CopiedEventArgs { Success = true });
        Assert.True(copied);
    }

    [Fact]
    public void 异步取值的复制按钮取到值后换请求号()
    {
        var cut = Render<CopyButton>(p => p.Add(x => x.GetValue, () => Task.FromResult("https://example.com/invite/1")));

        var button = cut.Find("button");
        Assert.False(button.HasAttribute("data-rune-copy-on-click"));
        button.Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("1", cut.Find("button").GetAttribute("data-rune-copy-request"));
            Assert.Equal("https://example.com/invite/1", cut.Find("button").GetAttribute("data-rune-copy"));
        });
    }

    [Fact]
    public void 预设按钮带好图标与名称()
    {
        var close = Render<CloseButton>();
        var clear = Render<ClearButton>(p => p.Add(x => x.Radius, Radius.Full));

        Assert.Equal("关闭", close.Find("button").GetAttribute("aria-label"));
        Assert.Equal("x", close.Find("svg").GetAttribute("data-icon"));
        Assert.Equal("清空", clear.Find("button").GetAttribute("aria-label"));
        Assert.Equal("circle-x", clear.Find("svg").GetAttribute("data-icon"));
    }

    [Fact]
    public void 外部图标链接的名称带上新窗口提示()
    {
        var cut = Render<IconLinkButton>(p => p.Add(x => x.Href, "https://example.com").Add(x => x.External, true)
            .Add(x => x.Icon, BuiltInIcons.X).Add(x => x.Label, "订阅"));

        var link = cut.Find("a");
        Assert.Equal("订阅（在新窗口打开）", link.GetAttribute("aria-label"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
    }
}
