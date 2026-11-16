using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>吸底操作栏（同 vue-tsx-pro 的 ActionBar）：起始边的说明、结束边的按钮；贴底时交给 JS 判断是否盖着下面的内容。</summary>
public class ActionBarTests : BunitContext
{
    public ActionBarTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 默认贴底_交给JS看是否盖着内容_说明在起始边_按钮在结束边()
    {
        var cut = Render<ActionBar>(p => p
            .Add(x => x.Start, (RenderFragment)(b => b.AddContent(0, "有 2 处改动")))
            .AddChildContent("<button>保存</button>")
            .AddUnmatched("aria-label", "表单操作"));

        var root = cut.Find("""[data-scope="action-bar"][data-part="root"]""");
        Assert.True(root.HasAttribute("data-rune-action-bar"));
        Assert.Equal("表单操作", root.GetAttribute("aria-label"));
        Assert.Equal(["start", "actions"], root.Children.Select(c => c.GetAttribute("data-part")));
        Assert.Equal("有 2 处改动", root.Children[0].TextContent);
        Assert.Equal("保存", root.Children[1].TextContent);

        var registry = Services.GetRequiredService<Rune.Styles.StyleRegistry>();
        Assert.Contains(registry.Rules, r => r.Contains("position:sticky"));
    }

    [Fact]
    public void 不贴底时是普通的一行_没有说明时不留空的起始边()
    {
        var cut = Render<ActionBar>(p => p.Add(x => x.Sticky, false).AddChildContent("<button>认领</button>"));

        var root = cut.Find("""[data-scope="action-bar"]""");
        Assert.False(root.HasAttribute("data-rune-action-bar"));
        Assert.Equal(["actions"], root.Children.Select(c => c.GetAttribute("data-part")));
    }
}
