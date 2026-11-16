using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 人员与组织选择器（同 vue-tsx-pro 的 PeoplePicker、PeoplePickerPanel）：按组织架构下钻、面包屑回到上层、搜索；
/// Types 决定能勾哪几种，Max 限制个数（1 时换掉原来的）；选择器在对话框里改草稿，确定才写回。
/// </summary>
public class PeoplePickerTests : BunitContext
{
    public PeoplePickerTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    static PeopleNode Person(string id, string name) => new(id, PeopleType.User, name) { Description = "工程师" };

    static readonly PeopleSource Source = PeopleSource.Local(
    [
        new("d-buy", PeopleType.Department, "采购部")
        {
            Children = [new("d-source", PeopleType.Department, "寻源组") { Children = [Person("u-zhao", "赵一凡")] }, Person("u-lin", "林晓"), Person("u-zhou", "周婷")],
        },
        new("d-empty", PeopleType.Department, "空部门"),
    ], [new("r-buyer", PeopleType.Role, "采购专员")]);

    static IEnumerable<string> Rows(IRenderedComponent<PeoplePickerPanel> cut) =>
        cut.FindAll("[data-part=\"browse\"] li").Select(li => li.QuerySelector("[data-part=\"title\"]")?.TextContent.Trim()).OfType<string>();

    [Fact]
    public void 下钻到部门_面包屑回到上层_没有下级的部门没有下级按钮()
    {
        var cut = Render<PeoplePickerPanel>(p => p.Add(x => x.Source, Source));

        Assert.Equal(["采购部", "空部门"], Rows(cut));
        // 只能选人员：部门行没有勾选框；空部门没有“下级”
        Assert.Empty(cut.FindAll("[data-part=\"browse\"] li input[type=checkbox]"));
        Assert.Single(cut.FindAll("button[aria-label=\"下级：采购部\"]"));
        Assert.Empty(cut.FindAll("button[aria-label=\"下级：空部门\"]"));

        cut.Find("button[aria-label=\"下级：采购部\"]").Click();
        Assert.Equal(["寻源组", "林晓", "周婷"], Rows(cut));
        Assert.Equal("采购部", cut.Find("[aria-current=\"location\"]").TextContent.Trim());

        // 点部门整行（不能选部门）也是进去
        cut.FindAll("[data-part=\"browse\"] li").First(li => li.TextContent.Contains("寻源组")).Click();
        Assert.Equal(["赵一凡"], Rows(cut));

        cut.FindAll("nav button").First(b => b.TextContent.Trim() == "全部").Click();
        Assert.Equal(["采购部", "空部门"], Rows(cut));
    }

    [Fact]
    public void 勾选写回_Max到了其余不可用_全选只加到满为止()
    {
        // 只给初始值：面板自己管选择，右边列出已选的
        var cut = Render<PeoplePickerPanel>(p => p
            .Add(x => x.Source, Source)
            .Add(x => x.Max, 2));
        IEnumerable<string> chosen() => cut.FindAll("[data-part=\"selected\"] li [data-part=\"title\"]").Select(t => t.TextContent.Trim());
        cut.Find("button[aria-label=\"下级：采购部\"]").Click();

        cut.Find("input[aria-label=\"林晓\"]").Change(true);
        Assert.Equal(["林晓"], chosen());
        Assert.Equal("已选 1 / 2", cut.Find("[data-part=\"header\"] strong").TextContent.Trim());

        // 全选：只加到满为止；满了以后其余的勾选框不可用
        cut.FindAll("label").First(l => l.TextContent.Contains("全选")).QuerySelector("input")!.Change(true);
        Assert.Equal(["林晓", "周婷"], chosen());
        Assert.True(cut.Find("input[aria-label=\"林晓\"]").HasAttribute("checked"));
    }

    [Fact]
    public void 写进值的是去掉下级的一项()
    {
        IReadOnlyList<PeopleEntry> value = [];
        var cut = Render<PeoplePickerPanel>(p => p
            .Add(x => x.Source, Source)
            .Add(x => x.Types, [PeopleType.Department])
            .Add(x => x.ValueChanged, v => value = v));
        cut.Find("input[aria-label=\"采购部\"]").Change(true);
        Assert.IsNotType<PeopleNode>(Assert.Single(value));
    }

    [Fact]
    public void Max为1时勾一个就换掉原来的_没有全选()
    {
        IReadOnlyList<PeopleEntry> value = [];
        var cut = Render<PeoplePickerPanel>(p => p
            .Add(x => x.Source, Source)
            .Add(x => x.Max, 1)
            .Bind(x => x.Value, value, v => value = v));
        cut.Find("button[aria-label=\"下级：采购部\"]").Click();

        Assert.DoesNotContain("全选", cut.Markup);
        cut.Find("input[aria-label=\"林晓\"]").Change(true);
        cut.Find("input[aria-label=\"周婷\"]").Change(true);
        Assert.Equal(["周婷"], value.Select(e => e.Name));
    }

    [Fact]
    public void 搜索停下后列出结果_角色页列出全部角色()
    {
        var cut = Render<PeoplePickerPanel>(p => p
            .Add(x => x.Source, Source)
            .Add(x => x.Types, [PeopleType.User, PeopleType.Role]));

        Assert.Contains("角色", cut.Find("[role=\"radiogroup\"], [role=\"group\"]").TextContent);
        cut.Find("input[type=search]").Input("采购");
        cut.WaitForAssertion(() => Assert.Equal(["采购部", "采购专员"], Rows(cut)), TimeSpan.FromSeconds(2));

        cut.Find("input[type=search]").Input("");
        cut.WaitForAssertion(() => Assert.Equal(["采购部", "空部门"], Rows(cut)));
    }

    [Fact]
    public async Task 选择器_对话框里改草稿_确定才写回_Backspace移除最后一个()
    {
        IReadOnlyList<PeopleEntry> value = [new("u-zhou", PeopleType.User, "周婷")];
        var cut = Render<PeoplePicker>(p => p
            .Add(x => x.Source, Source)
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v));

        var trigger = cut.Find("button[role=\"combobox\"]");
        Assert.Equal("dialog", trigger.GetAttribute("aria-haspopup"));
        trigger.Click();
        Assert.Equal("true", cut.Find("button[role=\"combobox\"]").GetAttribute("aria-expanded"));

        cut.Find("button[aria-label=\"下级：采购部\"]").Click();
        cut.Find("input[aria-label=\"林晓\"]").Change(true);
        // 还没确定：值没变
        Assert.Equal(["周婷"], value.Select(e => e.Name));

        await cut.InvokeAsync(() => cut.FindAll("dialog button").Last(b => b.TextContent.Trim() == "确定").Click());
        Assert.Equal(["周婷", "林晓"], value.Select(e => e.Name));

        cut.Render(p => p.Add(x => x.Value, value));
        await cut.InvokeAsync(() => cut.Find("button[role=\"combobox\"]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "remove-last" }));
        Assert.Equal(["周婷"], value.Select(e => e.Name));
    }
}
