using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

// 拖动与键盘在 JS 里算，这里测 C# 的一半：渲染出的取值规则与 aria、接住 rune-slide 后的夹取、对齐、提交
public class SliderTests : BunitContext
{
    public SliderTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static void Slide(IRenderedComponent<IComponent> cut, int index, string value, bool commit = false, long seq = 1) =>
        cut.Find("[data-rune-slider]").TriggerEvent("onruneslide", new SlideEventArgs { Index = index, Value = value, Commit = commit, Seq = seq });

    [Fact]
    public void 单值_取值规则交给JS_滑块是role_slider()
    {
        var cut = Render<Slider<int>>(p => p.Add(x => x.Value, 30).Add(x => x.Max, 50).Add(x => x.Step, 5)
            .Add(x => x.ValueText, v => $"{v} 元").AddUnmatched("aria-label", "预算"));

        var root = cut.Find("[data-rune-slider]");
        Assert.Equal(("0", "50", "5"), (root.GetAttribute("data-min"), root.GetAttribute("data-max"), root.GetAttribute("data-step")));
        Assert.Equal("30", root.GetAttribute("data-values"));
        Assert.Contains("--rune-slider-start:0%", root.GetAttribute("style"));
        Assert.Contains("--rune-slider-end:60%", root.GetAttribute("style"));

        var thumb = cut.Find("[role=slider]");
        Assert.Equal("30", thumb.GetAttribute("aria-valuenow"));
        Assert.Equal("30 元", thumb.GetAttribute("aria-valuetext"));
        Assert.Equal("预算", thumb.GetAttribute("aria-label"));
        Assert.Equal("0", thumb.GetAttribute("tabindex"));
        Assert.Equal("horizontal", thumb.GetAttribute("aria-orientation"));
    }

    [Fact]
    public void 接住JS的值_再夹一次范围并对齐步长_松开时提交()
    {
        int? committed = null;
        var cut = Render<Slider<int>>(p => p.Add(x => x.Value, 30).Add(x => x.Max, 50).Add(x => x.Step, 5)
            .Add(x => x.OnValueCommit, v => committed = v));

        Slide(cut, 0, "42");
        Assert.Equal("40", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
        Assert.Null(committed);

        Slide(cut, 0, "999", commit: true, seq: 2);
        Assert.Equal("50", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
        Assert.Equal(50, committed);
        // 处理到的序号写回，JS 据此知道 C# 追上了
        Assert.Equal("2", cut.Find("[data-rune-slider]").GetAttribute("data-ack"));
    }

    [Fact]
    public void 范围_两个滑块互相限制_名称后面加最小值最大值_刻度在范围里时active()
    {
        NumberRange<int>? committed = null;
        var cut = Render<Slider<int>>(p => p
            .Add(x => x.Range, new NumberRange<int>(20, 60))
            .Add(x => x.Marks, [0, new(50, "50"), 100])
            .Add(x => x.Name, "price")
            .Add(x => x.OnRangeCommit, v => committed = v)
            .AddUnmatched("aria-label", "价格"));

        var thumbs = cut.FindAll("[role=slider]");
        Assert.Equal(["价格 最小值", "价格 最大值"], thumbs.Select(t => t.GetAttribute("aria-label")));
        Assert.Equal(("0", "60"), (thumbs[0].GetAttribute("aria-valuemin"), thumbs[0].GetAttribute("aria-valuemax")));
        Assert.Equal(("20", "100"), (thumbs[1].GetAttribute("aria-valuemin"), thumbs[1].GetAttribute("aria-valuemax")));
        Assert.Equal(["false", "true", "false"], cut.FindAll("[data-value]").Select(m => m.HasAttribute("data-active") ? "true" : "false"));
        Assert.Equal(["20", "60"], cut.FindAll("input[type=hidden][name=price]").Select(i => i.GetAttribute("value")));
        Assert.Equal("50", cut.Find("[aria-hidden=true]").TextContent);

        // 低端拖过高端：停在高端
        Slide(cut, 0, "80", commit: true);
        Assert.Equal(new NumberRange<int>(60, 60), committed);
        Assert.Equal("60 60", cut.Find("[data-rune-slider]").GetAttribute("data-values"));
    }

    [Fact]
    public void 在Field里_第一个滑块拿Field的id_范围时连上隐藏的最小值最大值()
    {
        var cut = Render<Field>(p => p.Add(x => x.Label, "价格").Add(x => x.Description, "说明")
            .AddChildContent<Slider<int>>(s => s.Add(x => x.Range, new NumberRange<int>(10, 20))));

        var thumbs = cut.FindAll("[role=slider]");
        Assert.Equal(cut.Find("label").GetAttribute("for"), thumbs[0].Id);
        var labelledBy = thumbs[1].GetAttribute("aria-labelledby")!.Split(' ');
        Assert.Equal("价格", cut.Find($"#{labelledBy[0]}").TextContent.Trim());
        Assert.Equal("最大值", cut.Find($"#{labelledBy[1]}").TextContent.Trim());
        Assert.Equal("说明", cut.Find($"#{thumbs[0].GetAttribute("aria-describedby")}").TextContent.Trim());
    }

    [Fact]
    public void 不可用时不可聚焦也不接受值_只读时能聚焦不接受值_竖排()
    {
        var disabled = Render<Slider<int>>(p => p.Add(x => x.Value, 10).Add(x => x.Disabled, true));
        Assert.Null(disabled.Find("[role=slider]").GetAttribute("tabindex"));
        Assert.True(disabled.Find("[data-rune-slider]").HasAttribute("data-disabled"));
        Slide(disabled, 0, "50");
        Assert.Equal("10", disabled.Find("[role=slider]").GetAttribute("aria-valuenow"));

        var readOnly = Render<Slider<double>>(p => p.Add(x => x.Value, 0.5).Add(x => x.Max, 1).Add(x => x.Step, 0.05)
            .Add(x => x.ReadOnly, true).Add(x => x.Orientation, Orientation.Vertical));
        var thumb = readOnly.Find("[role=slider]");
        Assert.Equal(("0", "true", "vertical"), (thumb.GetAttribute("tabindex"), thumb.GetAttribute("aria-readonly"), thumb.GetAttribute("aria-orientation")));
        Slide(readOnly, 0, "0.8");
        Assert.Equal("0.5", readOnly.Find("[role=slider]").GetAttribute("aria-valuenow"));
    }
}
