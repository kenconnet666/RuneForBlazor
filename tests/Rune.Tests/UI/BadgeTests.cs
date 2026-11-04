using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class BadgeTests : BunitContext
{
    public BadgeTests() => Services.AddRuneUI();

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    [Fact]
    public void 默认浅色底中性色_Xs字号小圆角()
    {
        var cut = Render<Badge>(p => p.AddChildContent("草稿"));

        var className = cut.Find("span").ClassName;
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{className}{{")
                                                && rule.Contains("background-color:var(--rune-colors-fill)")
                                                && rule.Contains("font-size:var(--rune-font-sizes-xs)")
                                                && rule.Contains("border-radius:var(--rune-radii-sm)"));
    }

    [Fact]
    public void 圆点在没有图标时显示_读屏忽略()
    {
        var cut = Render<Badge>(p => p.Add(x => x.Dot, true).AddChildContent("在线"));

        Assert.Equal("true", cut.Find("[data-part=dot]").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void 状态标记按映射显示_不认识的状态用键当名字()
    {
        var statuses = new StatusMap<string> { ["shipped"] = new("已发货", Tone.Success) };
        var known = Render<StatusBadge<string>>(p => p.Add(x => x.Statuses, statuses).Add(x => x.Value, "shipped"));
        var unknown = Render<StatusBadge<string>>(p => p.Add(x => x.Statuses, statuses).Add(x => x.Value, "archived"));

        Assert.Equal("已发货", known.Find("span").TextContent);
        Assert.Equal("shipped", known.Find("span").GetAttribute("data-status"));
        Assert.NotNull(known.Find("[data-part=dot]"));
        Assert.Equal("archived", unknown.Find("span").TextContent);
        Assert.Equal(0, statuses.Order("shipped"));
        Assert.Equal(-1, statuses.Order("archived"));
    }

    [Fact]
    public void 同一外观的标记共用同一个类()
    {
        var a = Render<Badge>(p => p.Add(x => x.Tone, Tone.Success).AddChildContent("A"));
        var b = Render<Badge>(p => p.Add(x => x.Tone, Tone.Success).AddChildContent("B"));
        var c = Render<Badge>(p => p.Add(x => x.Tone, Tone.Danger).AddChildContent("C"));

        Assert.Equal(a.Find("span").ClassName, b.Find("span").ClassName);
        Assert.NotEqual(a.Find("span").ClassName, c.Find("span").ClassName);
    }

    [Fact]
    public void 标签给了移除回调才有移除按钮()
    {
        var removed = 0;
        var plain = Render<Tag>(p => p.AddChildContent("张三"));
        var removable = Render<Tag>(p => p
            .AddChildContent("张三")
            .Add(x => x.RemoveLabel, "移除 张三")
            .Add(x => x.OnRemove, EventCallback.Factory.Create(this, () => removed++)));

        Assert.Empty(plain.FindAll("button"));
        var button = removable.Find("button");
        Assert.Equal("移除 张三", button.GetAttribute("aria-label"));
        Assert.True(button.HasAttribute("data-rune-dismiss"));
        // 离场动画结束后 JS 初始化器在标签上发出 rune-dismiss
        removable.Find("[data-rune-dismissible]").TriggerEvent("onrunedismiss", EventArgs.Empty);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void 配方按档位组合缓存_同一组合是同一个对象()
    {
        var first = BadgeStyles.Of(new BadgeAppearance(StatusVariant.Soft, Tone.Info, Size.Xs, Radius.Full), ComponentStyles.Empty);
        var second = BadgeStyles.Of(new BadgeAppearance(StatusVariant.Soft, Tone.Info, Size.Xs, Radius.Full), ComponentStyles.Empty);

        Assert.Same(first, second);
        Assert.Same(first[BadgePart.Root], second[BadgePart.Root]);
    }
}
