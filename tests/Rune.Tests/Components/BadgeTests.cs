using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.Components;

public class BadgeTests : BunitContext
{
    public BadgeTests() => Services.AddRuneUI();

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    [Fact]
    public void 默认浅色底中性色()
    {
        var cut = Render<Badge>(p => p.AddChildContent("草稿"));

        var className = cut.Find("span").ClassName;
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{className}{{")
                                                && rule.Contains("background-color:var(--rune-colors-fill)")
                                                && rule.Contains("font-size:0.85em"));
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
        button.Click();
        Assert.Equal(1, removed);
    }

    [Fact]
    public void 配方按档位组合缓存_同一组合是同一个对象()
    {
        var first = BadgeStyles.Of(new BadgeAppearance(BadgeVariant.Soft, Tone.Info, null, Radius.Full), ComponentStyles.Empty);
        var second = BadgeStyles.Of(new BadgeAppearance(BadgeVariant.Soft, Tone.Info, null, Radius.Full), ComponentStyles.Empty);

        Assert.Same(first, second);
        Assert.Same(first[BadgePart.Root], second[BadgePart.Root]);
    }
}
