using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

// 读写存储在 JS 里（js/core/storage.js）：这里测交给 JS 的键与值、JS 交回记下的值、坏掉的不用、不认识的类型要给 TypeInfo
public class LocalSettingTests : BunitContext
{
    public LocalSettingTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 值写成JSON交给JS_读回记下的值写回绑定_坏掉的不动()
    {
        IReadOnlyList<string> hidden = ["dmc"];
        var cut = Render<LocalSetting<IReadOnlyList<string>>>(p => p.Add(x => x.Key, "orders.hidden").Add(x => x.Value, hidden).Add(x => x.ValueChanged, v => hidden = v));
        var template = cut.Find("template");

        Assert.Equal(("orders.hidden", "[\"dmc\"]"), (template.GetAttribute("data-rune-local-setting"), template.GetAttribute("data-value")));

        template.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "restore", Text = "[\"category\",\"buyer\"]" });
        Assert.Equal(["category", "buyer"], hidden);

        template.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "restore", Text = "{坏掉" });
        Assert.Equal(["category", "buyer"], hidden);
    }

    [Fact]
    public void 不认识的类型要给TypeInfo()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<LocalSetting<DateOnly>>(p => p.Add(x => x.Key, "k")));
        Assert.Contains("TypeInfo", error.Message);
    }
}
