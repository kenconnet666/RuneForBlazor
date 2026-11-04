using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.Tests.TestComponents;
using Rune.UI;

namespace Rune.Tests.UI;

public class CollapsibleTests : BunitContext
{
    public CollapsibleTests() => Services.AddRuneUI();

    IRenderedComponent<Collapsible> RenderCollapsible(Action<ComponentParameterCollectionBuilder<Collapsible>>? parameters = null) =>
        Render<Collapsible>(p =>
        {
            p.Add(c => c.Title, "标题").AddChildContent("内容");
            parameters?.Invoke(p);
        });

    [Fact]
    public void 非受控_点击后自己展开()
    {
        var cut = RenderCollapsible();

        cut.Find("button").Click();

        Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));
        Assert.Equal("open", cut.Find("[data-part=root]").GetAttribute("data-state"));
        Assert.Contains("内容", cut.Markup);
    }

    [Fact]
    public void 非受控_父级传来新值时跟着父级()
    {
        var cut = RenderCollapsible();

        cut.Render(p => p.Add(c => c.Open, true));

        Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void 受控_双向绑定写回父级()
    {
        var host = Render<BindHost>();

        host.Find("button").Click();

        Assert.Equal("True", host.Find("#host-open").TextContent);
        Assert.Equal("true", host.Find("button").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void 受控_父级不采纳时保持原状()
    {
        var requested = new List<bool>();
        var cut = RenderCollapsible(p => p.Add(c => c.OpenChanged, open => requested.Add(open)));

        cut.Find("button").Click();

        Assert.Equal([true], requested);
        Assert.Equal("false", cut.Find("button").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void 部件带标记与样式_使用方的样式排在后面()
    {
        var plain = RenderCollapsible();
        var customized = RenderCollapsible(p => p.Add(c => c.PropsTrigger, new Attrs
        {
            Sx = new Style { [When.Expanded] = new() { Color = "crimson" } },
            AriaLabel = "筛选",
        }));

        var trigger = customized.Find("[data-scope=collapsible][data-part=trigger]");
        Assert.Equal("筛选", trigger.GetAttribute("aria-label"));
        Assert.NotEqual(plain.Find("button").ClassName, trigger.ClassName);
        Assert.Contains(Services.GetRequiredService<StyleRegistry>().Rules, rule => rule.Contains("crimson"));
    }
}
