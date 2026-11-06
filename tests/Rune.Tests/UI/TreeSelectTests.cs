using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

// 打开、关闭与键盘在 JS 里（原生 popover、tree.js），这里测 C# 的一半：打开后才渲染树、展开已选的上级、单选写回、勾选按取法写回、标签只列整枝
public class TreeSelectTests : BunitContext
{
    public TreeSelectTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed record Org(string Id, string Name, IReadOnlyList<Org>? Children = null);

    static readonly Org[] Orgs =
    [
        new("rd", "研发中心", [new("rd-frontend", "前端团队"), new("rd-backend", "后端团队")]),
        new("mfg", "生产中心", [new("mfg-assembly", "装配团队"), new("mfg-testing", "测试团队")]),
    ];

    IRenderedComponent<TreeSelect<Org>> RenderOrgs(Action<ComponentParameterCollectionBuilder<TreeSelect<Org>>> configure) =>
        Render<TreeSelect<Org>>(p =>
        {
            p.Add(x => x.Items, Orgs).Add(x => x.GetKey, o => o.Id).Add(x => x.GetLabel, o => o.Name).Add(x => x.GetChildren, o => o.Children);
            configure(p);
        });

    static void Toggle(IRenderedComponent<TreeSelect<Org>> cut, bool open) =>
        cut.Find("[popover]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = open });

    [Fact]
    public void 打开后才渲染树_已选节点的上级展开_点节点写回值()
    {
        string? value = "rd-backend";
        var cut = RenderOrgs(p => p.Add(x => x.Value, value).Add(x => x.ValueChanged, v => value = v));

        var trigger = cut.Find("button[role=combobox]");
        Assert.Equal(("tree", "后端团队"), (trigger.GetAttribute("aria-haspopup"), trigger.TextContent.Trim()));
        Assert.Empty(cut.FindAll("[role=tree]"));

        Toggle(cut, true);
        Assert.Equal(trigger.GetAttribute("aria-controls"), cut.Find("[role=tree]").Id);
        Assert.Equal("研发中心,前端团队,后端团队,生产中心", string.Join(",", cut.FindAll("[role=treeitem]").Select(r => r.TextContent.Trim())));

        cut.FindAll("[role=treeitem]")[1].Click();
        Assert.Equal("rd-frontend", value);
    }

    [Fact]
    public void 多选勾上父节点_按取法写回_标签只列整枝_移除标签取消整枝()
    {
        IReadOnlyList<string> values = ["mfg-testing"];
        var cut = RenderOrgs(p => p
            .Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.CheckStrategy, CheckStrategy.Parent)
            .Add(x => x.Values, values)
            .Add(x => x.ValuesChanged, v => values = v));

        Toggle(cut, true);
        cut.FindAll("[role=treeitem]")[0].Click();
        Assert.Equal(["rd", "mfg-testing"], values);

        cut.Render(p => p.Add(x => x.Values, values));
        var tags = cut.FindAll("[data-scope=selected-tags] > [data-scope=badge]").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal(2, tags.Count);
        Assert.StartsWith("研发中心", tags[0]);

        // 触发按钮上 Backspace 移除最后一个标签
        cut.Find("button[role=combobox]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "remove-last" });
        Assert.Equal(["rd"], values);
    }
}
