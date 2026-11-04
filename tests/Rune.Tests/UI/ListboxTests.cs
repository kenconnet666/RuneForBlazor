using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class ListboxTests : BunitContext
{
    public ListboxTests()
    {
        Services.AddRuneUI();
        // Listbox 按渲染模式决定是否虚拟滚动
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly ListboxOption[] plans =
    [
        new("team", "团队版", Group: "团队"),
        new("free", "免费版", Group: "个人"),
        new("basic", "基础版"),
        new("enterprise", "企业版", Group: "团队", Disabled: true),
    ];

    [Fact]
    public void 列表状态_没分组的在前_各组按第一次出现的顺序()
    {
        var entries = ListState.Entries(plans, null);
        Assert.Equal(["basic", "team", "enterprise", "free"], entries.Select(e => e.Option.Value));
    }

    [Fact]
    public void 列表状态_字符串直接当选项_别的类型要给ToOption()
    {
        Assert.Equal(new ListboxOption("北京", "北京"), ListState.OptionOf("北京", null));
        Assert.Throws<InvalidOperationException>(() => ListState.OptionOf(42, null));
        Assert.Equal("7", ListState.OptionOf(7, n => new(n.ToString(), $"#{n}")).Value);
    }

    [Theory]
    [InlineData(SelectionMode.Single, false, "a", "")]
    [InlineData(SelectionMode.Single, true, "a", null)]
    [InlineData(SelectionMode.Single, false, "b", "b")]
    [InlineData(SelectionMode.Multiple, false, "b", "a,b")]
    [InlineData(SelectionMode.Multiple, false, "a", "")]
    [InlineData(SelectionMode.Multiple, true, "a", null)]
    public void 列表状态_切换选中_必选时不能取消最后一项(SelectionMode mode, bool required, string value, string? expected)
    {
        var next = ListState.Toggle(["a"], value, mode, required);
        Assert.Equal(expected, next is null ? null : string.Join(',', next));
    }

    [Fact]
    public void 列表框_角色与位置_组名经describedby念出_点击选中()
    {
        string? value = null;
        var cut = Render<Listbox<ListboxOption>>(p => p
            .Add(x => x.Items, plans)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v))
            .AddUnmatched("aria-label", "套餐"));

        var list = cut.Find("[role=listbox]");
        Assert.Equal("0", list.GetAttribute("tabindex"));
        Assert.Equal("space typeahead", list.GetAttribute("data-rune-list"));
        Assert.Null(list.GetAttribute("aria-labelledby"));

        var options = cut.FindAll("[role=option]");
        Assert.Equal(["基础版", "团队版", "企业版", "免费版"], options.Select(o => o.GetAttribute("data-label")));
        Assert.All(options, o => Assert.Equal("4", o.GetAttribute("aria-setsize")));
        Assert.Equal($"{list.Id}-1", options[1].Id);
        Assert.Equal("true", options[2].GetAttribute("aria-disabled"));
        // 团队组的两项指向同一个组名，没分组的不指
        var team = cut.Find($"#{options[1].GetAttribute("aria-describedby")}");
        Assert.Equal("团队", team.TextContent);
        Assert.Equal(options[1].GetAttribute("aria-describedby"), options[2].GetAttribute("aria-describedby"));
        Assert.Null(options[0].GetAttribute("aria-describedby"));

        options[1].Click();
        Assert.Equal("team", value);
        // 不可用的项点了也不选
        cut.FindAll("[role=option]")[2].Click();
        Assert.Equal("team", value);
    }

    [Fact]
    public void 列表框_多选_切换加入移出_点选回调()
    {
        IReadOnlyList<string> values = ["a"];
        var picks = new List<string>();
        var cut = Render<Listbox<string>>(p => p
            .Add(x => x.Items, ["a", "b", "c"])
            .Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.Values, values)
            .Add(x => x.ValuesChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => values = v))
            .Add(x => x.OnPick, EventCallback.Factory.Create<string>(this, picks.Add)));

        Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-multiselectable"));
        Assert.Equal("true", cut.FindAll("[role=option]")[0].GetAttribute("aria-selected"));

        cut.FindAll("[role=option]")[2].Click();
        Assert.Equal(["a", "c"], values);
        cut.Render(p => p.Add(x => x.Values, values));
        cut.FindAll("[role=option]")[0].Click();
        Assert.Equal(["c"], values);
        Assert.Equal(["c", "a"], picks);
    }

    [Fact]
    public void 列表框_在必填的Field里_名称取标签_再点已选项不取消()
    {
        string? value = "a";
        var cut = Render<Field>(p => p
            .Add(x => x.Label, "套餐")
            .Add(x => x.Required, true)
            .AddChildContent<Listbox<string>>(list => list
                .Add(x => x.Items, ["a", "b"])
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v))));

        var list = cut.Find("[role=listbox]");
        Assert.Equal(cut.Find("label").Id, list.GetAttribute("aria-labelledby"));
        Assert.Equal("true", list.GetAttribute("aria-required"));
        cut.FindAll("[role=option]")[0].Click();
        Assert.Equal("a", value);
    }

    [Fact]
    public void 列表框_没有选项时显示空状态_文案取语言包()
    {
        var cut = Render<Listbox<string>>(p => p.Add(x => x.Items, []));
        Assert.Empty(cut.FindAll("[role=option]"));
        Assert.Contains(Lang.ZhCn.SelectEmpty, cut.Find("[role=listbox] p").TextContent);
        // 没有图标：Empty 的 HideIcon
        Assert.Empty(cut.FindAll("[role=listbox] svg"));
    }

    [Fact]
    public void 列表框_选项多时虚拟滚动_全部选项的文字与不可用交给JS()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var items = Enumerable.Range(0, 500).Select(i => new ListboxOption($"v{i}", $"第 {i} 项", Disabled: i == 3)).ToArray();
        var cut = Render<Listbox<ListboxOption>>(p => p.Add(x => x.Items, items).Add(x => x.Value, "v42"));

        var list = cut.Find("[role=listbox]");
        Assert.True(cut.FindAll("[role=option]").Count < items.Length);
        var table = list.GetAttribute("data-rune-options")!;
        Assert.StartsWith("{\"labels\":[\"第 0 项\",", table);
        Assert.EndsWith("\"disabled\":[3]}", table);
        Assert.Equal("42", list.GetAttribute("data-rune-selected"));
    }

    [Fact]
    public void 列表框_静态渲染时不虚拟滚动()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var items = Enumerable.Range(0, 150).Select(i => $"第 {i} 项").ToArray();
        var cut = Render<Listbox<string>>(p => p.Add(x => x.Items, items));
        Assert.Equal(150, cut.FindAll("[role=option]").Count);
        Assert.Null(cut.Find("[role=listbox]").GetAttribute("data-rune-options"));
    }
}
