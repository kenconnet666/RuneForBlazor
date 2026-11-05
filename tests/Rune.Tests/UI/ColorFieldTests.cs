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
    public void 色块显示颜色_取色器只认不透明的hex_有名称()
    {
        var cut = Render<ColorField>(p => p.Add(x => x.Value, "#11182799"));

        Assert.Contains("--rune-color-field-fill:#11182799", cut.Find("[data-part=fill]").GetAttribute("style")!.Replace(" ", ""));
        var picker = cut.Find("input[type=color]");
        Assert.Equal("#111827", picker.GetAttribute("value"));
        Assert.Equal("选择颜色", picker.GetAttribute("aria-label"));
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
    public void 只读时取色器不可用_空值时色块透明()
    {
        var cut = Render<ColorField>(p => p.Add(x => x.Value, "").Add(x => x.ReadOnly, true));
        Assert.True(cut.Find("input[type=color]").HasAttribute("disabled"));
        Assert.Contains("transparent", cut.Find("[data-part=fill]").GetAttribute("style"));
        Assert.Equal("true", cut.Find("input[type=text]").GetAttribute("aria-invalid"));
    }
}
