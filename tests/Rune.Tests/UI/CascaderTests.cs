using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>级联选择：路径的显示、一列一列展开、选到叶子或任意一级、搜索、懒加载。</summary>
public class CascaderTests : BunitContext
{
    public CascaderTests()
    {
        Services.AddRuneUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed class Node(string key, string label, List<Node>? children = null, bool lazy = false)
    {
        public string Key { get; } = key;
        public string Label { get; } = label;
        public List<Node>? Children { get; set; } = children;
        public bool Lazy { get; } = lazy;
    }

    static List<Node> Regions() =>
    [
        new("zj", "浙江省", [new("hz", "杭州市", [new("xh", "西湖区"), new("bj", "滨江区")])]),
        new("js", "江苏省", [new("nj", "南京市", [new("gl", "鼓楼区")])]),
    ];

    IRenderedComponent<Cascader<Node>> Render(List<Node> items, IReadOnlyList<string>? value, Action<IReadOnlyList<string>?> changed, Action<ComponentParameterCollectionBuilder<Cascader<Node>>>? more = null) =>
        Render<Cascader<Node>>(p =>
        {
            p.Add(x => x.Items, items)
                .Add(x => x.GetKey, (n, _) => n.Key)
                .Add(x => x.GetLabel, n => n.Label)
                .Add(x => x.GetChildren, n => n.Children)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, EventCallback.Factory.Create(this, changed))
                .AddUnmatched("aria-label", "地区");
            more?.Invoke(p);
        });

    static void Open<T>(IRenderedComponent<Cascader<T>> cut) =>
        cut.Find("[data-rune-cascader]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true });

    [Fact]
    public void 触发框显示整条路径_打开时沿已选路径展开各列()
    {
        var cut = Render(Regions(), ["zj", "hz", "xh"], _ => { });

        Assert.Equal("浙江省 / 杭州市 / 西湖区", cut.Find("button[role=combobox]").TextContent.Trim());
        Open(cut);

        var columns = cut.FindAll("[role=listbox]");
        Assert.Equal(3, columns.Count);
        Assert.Equal(["地区", "浙江省", "杭州市"], columns.Select(c => c.GetAttribute("aria-label")));
        // 已选的那一列先拿到焦点；高亮按路径
        Assert.True(columns[2].HasAttribute("autofocus"));
        Assert.Equal(columns[2].Id + "-0", columns[2].GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public void 点有下一级的项只展开_点叶子选中整条路径()
    {
        IReadOnlyList<string>? value = null;
        var cut = Render(Regions(), null, v => value = v);
        Open(cut);

        cut.FindAll("[role=option]").Single(o => o.TextContent.Contains("江苏省")).Click();
        Assert.Null(value);
        Assert.Equal(2, cut.FindAll("[role=listbox]").Count);

        cut.FindAll("[role=option]").Single(o => o.TextContent.Contains("南京市")).Click();
        cut.FindAll("[role=option]").Single(o => o.TextContent.Contains("鼓楼区")).Click();
        Assert.Equal(["js", "nj", "gl"], value);
    }

    [Fact]
    public void 每一级都能选_JS发来的高亮与进入换下一列()
    {
        IReadOnlyList<string>? value = null;
        var cut = Render(Regions(), null, v => value = v, p => p.Add(x => x.ChangeOnSelect, true));
        Open(cut);

        cut.FindAll("[role=option]").Single(o => o.TextContent.Contains("浙江省")).Click();
        Assert.Equal(["zj"], value);

        cut.Find("[role=listbox]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "highlight", Text = "1" });
        Assert.Equal("江苏省", cut.FindAll("[role=listbox]")[1].GetAttribute("aria-label"));
    }

    [Fact]
    public void 搜索列出匹配的完整路径_选中()
    {
        IReadOnlyList<string>? value = null;
        var cut = Render(Regions(), null, v => value = v, p => p.Add(x => x.Searchable, true));
        Open(cut);

        cut.Find("input[type=search]").Input("鼓楼");
        var hits = cut.FindAll("[role=option]");
        Assert.Equal("江苏省 / 南京市 / 鼓楼区", Assert.Single(hits).TextContent.Trim());
        hits[0].Click();
        Assert.Equal(["js", "nj", "gl"], value);
    }

    [Fact]
    public async Task 懒加载_进入时取回下一级_期间列带aria_busy()
    {
        var gate = new TaskCompletionSource();
        var items = new List<Node> { new("hq", "总部", lazy: true) };
        var cut = Render(items, null, _ => { }, p => p
            .Add(x => x.HasChildren, n => n.Lazy || n.Children is { Count: > 0 })
            .Add(x => x.LoadChildren, async n =>
            {
                await gate.Task;
                n.Children = [new("rd", "研发中心")];
            }));
        Open(cut);

        var click = cut.InvokeAsync(() => cut.FindAll("[role=option]")[0].Click());
        cut.WaitForAssertion(() => Assert.Equal("true", cut.FindAll("[role=listbox]")[1].GetAttribute("aria-busy")));
        gate.SetResult();
        await click;
        cut.WaitForAssertion(() => Assert.Contains("研发中心", cut.FindAll("[role=listbox]")[1].TextContent));
    }
}
