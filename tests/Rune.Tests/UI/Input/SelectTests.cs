using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class SelectTests : BunitContext
{
    public SelectTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly string[] cities = ["北京", "上海", "广州"];

    static void Toggle<T>(IRenderedComponent<T> cut, bool open) where T : IComponent =>
        cut.Find("[popover]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = open });

    [Fact]
    public void 触发按钮_combobox的角色与状态_弹层贴着外壳_列表由触发按钮控制()
    {
        var cut = Render<Select<string>>(p => p.Add(x => x.Items, cities).AddUnmatched("aria-label", "城市"));

        var trigger = cut.Find("button[role=combobox]");
        var panel = cut.Find("[popover]");
        var list = cut.Find("[role=listbox]");
        Assert.Equal("button", trigger.GetAttribute("type"));
        Assert.Equal("listbox", trigger.GetAttribute("aria-haspopup"));
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        Assert.Equal(panel.Id, trigger.GetAttribute("popovertarget"));
        Assert.Equal(list.Id, trigger.GetAttribute("aria-controls"));
        Assert.Equal("space typeahead", trigger.GetAttribute("data-rune-list"));
        Assert.Equal("城市", trigger.GetAttribute("aria-label"));
        Assert.Equal("城市", list.GetAttribute("aria-label"));
        // 外壳是输入框的部件：按 InputPart 定制时一起变
        var shell = cut.Find("[data-rune-control]");
        Assert.Equal("input", shell.GetAttribute("data-scope"));
        Assert.Equal(shell.Id, panel.GetAttribute("data-anchor"));
        Assert.NotNull(panel.GetAttribute("data-match-width"));
        // 没有选择：占位文字取语言包
        Assert.Equal("请选择", trigger.TextContent);
    }

    [Fact]
    public void 单选_选中后显示文字_再选已选项不取消()
    {
        var value = "上海";
        var cut = Render<Select<string>>(p => p
            .Add(x => x.Items, cities)
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v!)));

        Assert.Equal("上海", cut.Find("button[role=combobox]").TextContent);
        cut.Find("[role=option][data-label='上海']").Click();
        Assert.Equal("上海", value);
        cut.Find("[role=option][data-label='广州']").Click();
        Assert.Equal("广州", value);
    }

    [Fact]
    public void 只读与不可用_不能打开_只读仍能聚焦()
    {
        var readOnly = Render<Select<string>>(p => p.Add(x => x.Items, cities).Add(x => x.Value, "北京").Add(x => x.ReadOnly, true).Add(x => x.Clearable, true));
        var trigger = readOnly.Find("button[role=combobox]");
        Assert.Null(trigger.GetAttribute("popovertarget"));
        Assert.Null(trigger.GetAttribute("data-rune-list"));
        Assert.Null(trigger.GetAttribute("disabled"));
        Assert.Equal("true", trigger.GetAttribute("aria-readonly"));
        Assert.Empty(readOnly.FindAll("[data-scope=input][data-part=button]"));

        var disabled = Render<Select<string>>(p => p.Add(x => x.Items, cities).Add(x => x.Disabled, true));
        Assert.NotNull(disabled.Find("button[role=combobox]").GetAttribute("disabled"));
    }

    [Fact]
    public void 多选_标签只给指针用_读屏从触发按钮念出全部_Backspace命令移除最后一项()
    {
        IReadOnlyList<string> values = ["北京", "广州"];
        var cut = Render<Select<string>>(p => p
            .Add(x => x.Items, cities)
            .Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.Values, values)
            .Add(x => x.ValuesChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => values = v))
            .Add(x => x.Name, "city"));

        var tags = cut.Find("[data-scope=selected-tags]");
        Assert.Equal("true", tags.GetAttribute("aria-hidden"));
        Assert.All(tags.QuerySelectorAll("button"), b => Assert.Equal("-1", b.GetAttribute("tabindex")));
        var trigger = cut.Find("button[role=combobox]");
        Assert.Equal("北京、广州", trigger.TextContent.Trim());
        Assert.Equal("remove-last", trigger.GetAttribute("data-rune-backspace"));
        Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-multiselectable"));
        // 随表单提交：每个值一个隐藏的 input
        Assert.Equal(["北京", "广州"], cut.FindAll("input[type=hidden][name=city]").Select(i => i.GetAttribute("value")));

        trigger.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "remove-last" });
        Assert.Equal(["北京"], values);
    }

    [Fact]
    public void 多选_数量上限_其余收进加N()
    {
        var cut = Render<Select<string>>(p => p
            .Add(x => x.Items, cities)
            .Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.Values, ["北京", "上海", "广州"])
            .Add(x => x.MaxTagCount, 2));

        Assert.Equal(["北京", "上海", "+1"], cut.FindAll("[data-scope=selected-tags] > [data-scope=badge]").Select(t => t.TextContent.Trim()));
    }

    [Fact]
    public void 按宽度收起_交给JS的约定与加1到加N的文字()
    {
        var cut = Render<Select<string>>(p => p
            .Add(x => x.Items, cities)
            .Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.Values, ["北京", "上海", "广州"])
            .Add(x => x.MaxTagCount, TagLimit.Responsive));

        var row = cut.Find("[data-rune-overflow]");
        Assert.Equal("""["+1","+2","+3"]""", row.GetAttribute("data-rune-more"));
        Assert.Equal(3, row.QuerySelectorAll("[data-rune-overflow-item]").Length);
        Assert.NotNull(row.QuerySelector("[data-rune-overflow-rest][data-overflow-hidden]"));
    }

    [Fact]
    public void 可搜索_按显示文字过滤_忽略大小写与重音_没有匹配时提示()
    {
        string[] items = ["Ångström", "Berlin", "Bern"];
        var cut = Render<Select<string>>(p => p.Add(x => x.Items, items).Add(x => x.Searchable, true));
        var search = cut.Find("[popover] input[type=search]");
        Assert.Equal(cut.Find("[role=listbox]").Id, search.GetAttribute("aria-controls"));
        Assert.Equal("text", search.GetAttribute("data-rune-list"));

        search.Input("ang");
        Assert.Equal(["Ångström"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label")));
        Assert.Equal("ang", cut.Find("[role=listbox]").GetAttribute("data-rune-query"));
        Assert.Equal("1 个选项", cut.Find("[role=listbox]").GetAttribute("data-rune-results"));

        cut.Find("[popover] input[type=search]").Input("xyz");
        Assert.Empty(cut.FindAll("[role=option]"));
        Assert.Contains("没有匹配的选项", cut.Find("[role=listbox]").TextContent);
    }

    [Fact]
    public void 可搜索_关闭时清空搜索词_已选项被过滤掉时仍显示文字()
    {
        var cut = Render<Select<string>>(p => p.Add(x => x.Items, cities).Add(x => x.Value, "北京").Add(x => x.Searchable, true));
        Toggle(cut, true);
        cut.Find("[popover] input[type=search]").Input("广");
        Assert.Single(cut.FindAll("[role=option]"));
        Assert.Equal("北京", cut.Find("button[role=combobox]").TextContent);

        Toggle(cut, false);
        Assert.Equal(3, cut.FindAll("[role=option]").Count);
        Assert.Equal("", cut.Find("[popover] input[type=search]").GetAttribute("value"));
    }

    [Fact]
    public void 新建_没有同名选项时列表末尾多一项_选中后报告并清空搜索词()
    {
        string? created = null;
        IReadOnlyList<string> values = [];
        var cut = Render<Select<string>>(p => p
            .Add(x => x.Items, cities)
            .Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.Values, values)
            .Add(x => x.ValuesChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => values = v))
            .Add(x => x.Creatable, true)
            .Add(x => x.OnCreate, EventCallback.Factory.Create<string>(this, text => created = text)));

        cut.Find("[popover] input[type=search]").Input("北京");
        Assert.DoesNotContain(cut.FindAll("[role=option]"), o => o.GetAttribute("data-label")!.StartsWith("新建"));

        cut.Find("[popover] input[type=search]").Input(" 深圳 ");
        var creation = cut.FindAll("[role=option]")[^1];
        Assert.Equal("新建“深圳”", creation.GetAttribute("data-label"));
        creation.Click();
        Assert.Equal("深圳", created);
        Assert.Equal(["深圳"], values);
        Assert.Equal("", cut.Find("[popover] input[type=search]").GetAttribute("value"));
    }

    [Fact]
    public async Task 远程分页_滚到末尾取下一页_按值去重_骨架接在末尾()
    {
        var calls = new List<(string Query, int Page, TaskCompletionSource<InfinitePage<string, int>> Result)>();
        var cut = Render<Select<string>>(p => p.Add(x => x.LoadPage, (query, page, _) =>
        {
            var result = new TaskCompletionSource<InfinitePage<string, int>>();
            calls.Add((query, page, result));
            return result.Task;
        }));

        Toggle(cut, true);
        Assert.Equal(1, calls[0].Page);
        calls[0].Result.SetResult(new(["a", "b"], 2, true));
        await cut.InvokeAsync(() => { });
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=option]").Count), TimeSpan.FromSeconds(5));
        Assert.NotNull(cut.Find("[data-rune-list-end]"));

        // JS 看到末尾的哨兵：取第 2 页，已有的选项留着、骨架接在后面；重复的 b 只留一个
        cut.Find("[role=listbox]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "more" });
        Assert.Equal(2, calls[1].Page);
        Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-busy"));
        Assert.Equal(2, cut.FindAll("[role=option]").Count);
        calls[1].Result.SetResult(new(["b", "c"], 3, false));
        await cut.InvokeAsync(() => { });
        cut.WaitForAssertion(() => Assert.Equal(["a", "b", "c"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label"))), TimeSpan.FromSeconds(5));
        Assert.Empty(cut.FindAll("[data-rune-list-end]"));
    }

    [Fact]
    public async Task 远程选项_打开时请求_结果代替Items_已选项文字取自结果()
    {
        var calls = new List<string>();
        var result = new TaskCompletionSource<IEnumerable<string>>();
        var cut = Render<Select<string>>(p => p
            .Add(x => x.Items, ["c1"])
            .Add(x => x.Value, "c1")
            .Add(x => x.ToOption, id => new ListboxOption(id, id == "c1" ? "陈默" : "许悠"))
            .Add(x => x.Load, (query, _) =>
            {
                calls.Add(query);
                return result.Task;
            }));

        Assert.Equal("陈默", cut.Find("button[role=combobox]").TextContent);
        Toggle(cut, true);
        Assert.Equal([""], calls);
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-busy")));

        result.SetResult(["x1", "c1"]);
        await cut.InvokeAsync(() => { });
        cut.WaitForAssertion(() => Assert.Equal(["许悠", "陈默"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label"))), TimeSpan.FromSeconds(5));
        Assert.Null(cut.Find("[role=listbox]").GetAttribute("aria-busy"));
    }

    [Fact]
    public void 在Field里_名称取Field的标签_搜索框不归Field管()
    {
        var cut = Render<Field>(p => p
            .Add(x => x.Label, "城市")
            .AddChildContent<Select<string>>(s => s.Add(x => x.Items, cities).Add(x => x.Searchable, true)));

        var trigger = cut.Find("button[role=combobox]");
        Assert.Equal(trigger.Id, cut.Find("label").GetAttribute("for"));
        var labelId = cut.Find("label").Id;
        Assert.Equal(labelId, cut.Find("[role=listbox]").GetAttribute("aria-labelledby"));
        Assert.NotEqual(trigger.Id, cut.Find("[popover] input[type=search]").Id);
    }

    [Fact]
    public void 文字匹配_包含即命中_忽略大小写重音与全半角_另配的匹配任一命中()
    {
        var match = TextMatching.For("zh-CN");
        Assert.True(match("Ångström", "ANG"));
        Assert.True(match("ＡＢＣ", "abc"));
        Assert.True(match("任何文字", ""));
        Assert.False(match("北京", "上"));

        var config = new UIConfig(Themes.Light, null, ColorScheme.System, ComponentConfigs.Empty)
        {
            TextMatcher = (text, query) => text == "北京" && query == "bj",
        };
        Assert.True(config.Matches("北京", "bj"));
        Assert.False(config.Matches("上海", "bj"));
    }
}
