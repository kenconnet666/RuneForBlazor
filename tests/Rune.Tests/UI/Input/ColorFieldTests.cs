using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class ColorFieldTests : BunitContext
{
    public ColorFieldTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 色块显示颜色_是打开取色器的按钮()
    {
        var cut = Render<ColorField>(p => p.Add(x => x.Value, "#11182799"));

        Assert.Contains("--rune-color-field-fill:#11182799", cut.Find("[data-part=fill]").GetAttribute("style")!.Replace(" ", ""));
        var swatch = cut.Find("button[data-part=swatch]");
        Assert.Equal("选择颜色", swatch.GetAttribute("aria-label"));
        Assert.NotNull(swatch.GetAttribute("aria-controls") ?? swatch.GetAttribute("popovertarget"));
        Assert.Equal("#11182799", cut.Find("input[type=text]").GetAttribute("value"));
    }

    [Fact]
    public void 回车或失焦时规范成hex_写不成颜色时标无效并恢复()
    {
        string? committed = null;
        var cut = Render<ColorField>(p => p.Add(x => x.Value, "#0ea5e9").Add(x => x.ValueChanged, (string? v) => committed = v));
        var input = cut.Find("input[type=text]");

        input.Input("tomato");
        input.Change("tomato");
        Assert.Equal("#ff6347", committed);

        cut.Find("input[type=text]").Input("nope");
        Assert.Equal("true", cut.Find("input[type=text]").GetAttribute("aria-invalid"));
        cut.Find("input[type=text]").Change("nope");
        Assert.Null(cut.Find("input[type=text]").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void 只读时色块按钮不可用_空值时色块透明()
    {
        var cut = Render<ColorField>(p => p.Add(x => x.Value, "").Add(x => x.ReadOnly, true));
        Assert.True(cut.Find("button[data-part=swatch]").HasAttribute("disabled"));
        Assert.Contains("transparent", cut.Find("[data-part=fill]").GetAttribute("style"));
        Assert.Equal("true", cut.Find("input[type=text]").GetAttribute("aria-invalid"));
    }
    [Fact]
    public void 点开取色器_改动实时写回()
    {
        string? committed = null;
        var cut = Render<ColorField>(p => p.Add(x => x.Value, "#0ea5e9").Add(x => x.ValueChanged, (string? v) => committed = v));
        cut.Find("dialog, [popover]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true });
        var picker = cut.Find("[data-rune-color-picker]");
        picker.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "color-pick", Text = "#ff0000" });
        Assert.Equal("#ff0000", committed);
    }
    string? model = "#0ea5e9";

    [Fact]
    public void 在Field里绑定字段时不来回重新渲染()
    {
        // 取色器里的零件曾经往外层 Field 登记自己的字段，与 ColorField 的轮流登记，无限重新渲染（这里会卡住不返回）。
        // 静态渲染时弹出层的内容一开始就在
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<Field>(p => p.Add(x => x.Label, "品牌色").AddChildContent<ColorField>(c => c
            .Add(x => x.Value, model)
            .Add(x => x.ValueChanged, (string? v) => model = v)
            .Add(x => x.ValueExpression, () => model)));
        Assert.Single(cut.FindAll("[data-rune-color-picker]"));
    }
}
