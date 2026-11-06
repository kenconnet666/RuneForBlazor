using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

// 数字框的缩放与数值区间；不受控，从输入框读值
public class NumberRangeFieldTests : BunitContext
{
    public NumberRangeFieldTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 缩放_按万元显示与输入_写回元_步长与范围按元换算给JS()
    {
        decimal? saved = null;
        var cut = Render<NumberField<decimal?>>(p => p.Add(x => x.Value, 155353.2m).Add(x => x.Scale, 10000).Add(x => x.Format, "0.##")
            .Add(x => x.Step, 100).Add(x => x.Min, 0).Add(x => x.ValueChanged, v => saved = v));
        var input = cut.Find("input[role=spinbutton]");
        Assert.Equal("15.54", input.GetAttribute("value"));
        Assert.Equal(("0.01", "0"), (input.GetAttribute("data-step"), input.GetAttribute("aria-valuemin")));

        input.Change("20.5");
        Assert.Equal(205000m, saved);
    }

    [Fact]
    public void 区间_一端可以空着_低比高大时对调_两个框各念最小值最大值()
    {
        var cut = Render<NumberRangeField<int?>>(p => p.Add(x => x.Value, new NumberRange<int?>(20, null)).Add(x => x.Names, ["from", "to"]));

        var inputs = cut.FindAll("input[role=spinbutton]");
        Assert.Equal(["最小值", "最大值"], inputs.Select(i => i.GetAttribute("aria-label")));
        Assert.Equal(["20", ""], inputs.Select(i => i.GetAttribute("value")));
        Assert.Equal("group", cut.Find("[role=group]").GetAttribute("role"));
        Assert.Empty(cut.FindAll("button"));

        cut.FindAll("input[role=spinbutton]")[1].Change("5");
        Assert.Equal(["5", "20"], cut.FindAll("input[role=spinbutton]").Select(i => i.GetAttribute("value")));
        Assert.Equal(["from", "to"], cut.FindAll("input[type=hidden]").Select(i => i.GetAttribute("name")));
        Assert.Equal(["5", "20"], cut.FindAll("input[type=hidden]").Select(i => i.GetAttribute("value")));
    }

    [Fact]
    public void 在Field里_标签指向第一个框_组拿标签与说明()
    {
        var cut = Render<Field>(p => p.Add(x => x.Label, "金额").Add(x => x.Description, "说明").Add(x => x.Required, true)
            .AddChildContent<NumberRangeField<decimal?>>());

        var group = cut.Find("[role=group]");
        var first = cut.FindAll("input[role=spinbutton]")[0];
        Assert.Equal(cut.Find("label").GetAttribute("for"), first.Id);
        Assert.Equal("金额", cut.Find($"#{group.GetAttribute("aria-labelledby")}").TextContent.Trim());
        Assert.Equal("说明", cut.Find($"#{group.GetAttribute("aria-describedby")}").TextContent.Trim());
        Assert.Equal("true", first.GetAttribute("aria-required"));
        Assert.Null(first.GetAttribute("aria-describedby"));
    }
}
