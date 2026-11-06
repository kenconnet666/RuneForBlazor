using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

// 左边是勾选的树：已移过去的叶子勾上且不可用；勾父节点只记还没移过去的叶子；移过去按先后追加
public class TreeTransferTests : BunitContext
{
    public TreeTransferTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed record Org(string Id, string Name, IReadOnlyList<Org>? Children = null);

    static readonly Org[] Orgs = [new("rd", "研发中心", [new("rd-fe", "前端组"), new("rd-be", "后端组"), new("rd-qa", "测试组")]), new("purchase", "采购部")];

    [Fact]
    public void 已移过去的叶子勾上且不可用_勾父节点后移过去的只有还没移的叶子()
    {
        IReadOnlyList<string>? value = ["rd-fe"];
        var cut = Render<TreeTransfer<Org>>(p => p
            .Add(x => x.Items, Orgs).Add(x => x.GetKey, o => o.Id).Add(x => x.GetLabel, o => o.Name).Add(x => x.GetChildren, o => o.Children)
            .Add(x => x.Value, value).Add(x => x.ValueChanged, v => value = v));

        cut.Find("[role=tree]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "expand", Text = "0" });
        var fe = cut.FindAll("[role=treeitem]")[1];
        Assert.Equal(("true", "true"), (fe.GetAttribute("aria-checked"), fe.GetAttribute("aria-disabled")));

        cut.FindAll("[role=treeitem]")[0].Click();
        cut.Find("button[aria-label=移到已选]").Click();
        Assert.Equal(["rd-fe", "rd-be", "rd-qa"], value);
    }
}
