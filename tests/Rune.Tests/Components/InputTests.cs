using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.Components;

public class InputTests : BunitContext
{
    public InputTests() => Services.AddRuneUI();

    [Theory]
    [InlineData("１２３．５", 123.5)]
    [InlineData("1,234.5", 1234.5)]
    [InlineData("－7", -7)]
    [InlineData("¥12", 12)]
    public void 数字文本_认全角与千分位(string text, double expected)
    {
        Assert.True(NumberText.TryParse(text, CultureInfo.GetCultureInfo("zh-CN"), out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Fact]
    public void 数字文本_夹到范围并对齐步长_没有浮点误差()
    {
        Assert.Equal(0.3m, NumberText.Constrain(0.30000000000000004m, null, null, 0.1m));
        Assert.Equal(10m, NumberText.Constrain(12m, 0m, 10m, 1m));
        // 对齐到 min + n × step
        Assert.Equal(1.5m, NumberText.Constrain(1.4m, 0.5m, null, 1m));
        Assert.True(NumberText.TryParse("  ", CultureInfo.InvariantCulture, out var empty));
        Assert.Null(empty);
        Assert.False(NumberText.TryParse("abc", CultureInfo.InvariantCulture, out _));
    }

    [Fact]
    public void 数字输入框_提交时夹到范围_清空写回null_步进按钮()
    {
        int? value = 5;
        var cut = Render<NumberField<int?>>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int?>(this, v => value = v))
            .Add(x => x.Min, 1)
            .Add(x => x.Max, 10));

        var input = cut.Find("input");
        Assert.Equal("spinbutton", input.GetAttribute("role"));
        Assert.Equal("1", input.GetAttribute("aria-valuemin"));
        input.Change("99");
        Assert.Equal(10, value);

        cut.Render(p => p.Add(x => x.Value, value));
        // 到了最大值：增加按钮不可用
        Assert.True(cut.Find("button[aria-label=增加]").HasAttribute("disabled"));
        cut.Find("button[aria-label=减少]").Click();
        Assert.Equal(9, value);

        cut.Find("input").Change("");
        Assert.Null(value);
    }

    [Fact]
    public void 数字输入框_解析不了时恢复原来的显示()
    {
        var cut = Render<NumberField<decimal>>(p => p.Add(x => x.Value, 2.5m).Add(x => x.Format, "0.00"));
        var before = cut.Find("input");

        before.Change("abc");

        // 换了 key：input 重建，显示回当前值
        var after = cut.Find("input");
        Assert.NotSame(before, after);
        Assert.Equal(2.5m.ToString("0.00", CultureInfo.CurrentCulture), after.GetAttribute("value"));
    }

    [Fact]
    public void 数字输入框_不可空的类型不能清空_double也没有浮点误差()
    {
        var count = 3;
        var cut = Render<NumberField<int>>(p => p
            .Add(x => x.Value, count)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, v => count = v)));
        var before = cut.Find("input");
        before.Change("");
        Assert.Equal(3, count);
        Assert.NotSame(before, cut.Find("input"));

        double ratio = 0.1;
        var doubles = Render<NumberField<double>>(p => p
            .Add(x => x.Value, ratio)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<double>(this, v => ratio = v))
            .Add(x => x.Step, 0.1m));
        doubles.Find("button[aria-label=增加]").Click();
        doubles.Render(p => p.Add(x => x.Value, ratio));
        doubles.Find("button[aria-label=增加]").Click();
        Assert.Equal(0.3, ratio);
    }

    [Fact]
    public void 输入外框的不可用只看输入元素本身()
    {
        Render<NativeSelect<string?>>(p => p.Add(x => x.Items, ["a"]).Add(x => x.Placeholder, "选择").Add(x => x.Required, true));

        var registry = Services.GetRequiredService<Rune.Styles.StyleRegistry>();
        // 占位选项不可用（必填）不能让整个外框看起来不可用
        Assert.Contains(registry.Rules, r => r.Contains(":has(> :is(input, select, textarea):disabled)"));
        Assert.DoesNotContain(registry.Rules, r => r.Contains(":has(:disabled)"));
    }

    [Fact]
    public void 单选组_共用name_选中项随值变_以Field标签为名称()
    {
        string? shipping = "pickup";
        var cut = Render<Field>(p => p
            .Add(x => x.Label, "配送方式")
            .AddChildContent<RadioGroup<string?>>(group => group
                .Add(x => x.Value, shipping)
                .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => shipping = v))
                .AddChildContent<Radio<string?>>(radio => radio.Add(x => x.Value, "express").AddChildContent("快递"))
                .AddChildContent<Radio<string?>>(radio => radio.Add(x => x.Value, "pickup").AddChildContent("自提"))));

        var radios = cut.FindAll("input[type=radio]");
        Assert.Equal(radios[0].GetAttribute("name"), radios[1].GetAttribute("name"));
        Assert.False(radios[0].HasAttribute("checked"));
        Assert.True(radios[1].HasAttribute("checked"));
        Assert.Equal(cut.Find("label[for]").Id, cut.Find("[role=radiogroup]").GetAttribute("aria-labelledby"));

        radios[0].Change(true);
        Assert.Equal("express", shipping);
    }

    [Fact]
    public void 原生下拉_按位置对应值_占位选项()
    {
        var cities = new[] { "北京", "上海" };
        string? city = null;
        var cut = Render<NativeSelect<string?>>(p => p
            .Add(x => x.Items, cities)
            .Add(x => x.Placeholder, "选择城市")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => city = v)));

        var select = cut.Find("select");
        Assert.Equal("", select.GetAttribute("value"));
        Assert.Equal(["选择城市", "北京", "上海"], cut.FindAll("option").Select(o => o.TextContent));
        select.Change("1");
        Assert.Equal("上海", city);
    }

    [Fact]
    public void 滑块_松开时写回值_原生的范围属性()
    {
        double value = 20;
        var cut = Render<Slider<double>>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<double>(this, v => value = v))
            .Add(x => x.Max, 50)
            .Add(x => x.Step, 5)
            .Add(x => x.ShowValue, true));

        var input = cut.Find("input[type=range]");
        Assert.Equal("50", input.GetAttribute("max"));
        Assert.Equal("5", input.GetAttribute("step"));
        Assert.Equal("20", cut.Find("output").TextContent);
        input.Change("35");
        Assert.Equal(35, value);
    }

    sealed class Signup
    {
        [Required(ErrorMessage = "请填写姓名")]
        public string? Name { get; set; }

        public string? Note { get; set; }
    }

    [Fact]
    public void 表单_提交未通过时列出错误_带标签并能跳到控件()
    {
        var model = new Signup();
        var cut = Render<Form>(p => p
            .Add(x => x.Model, model)
            .AddChildContent<Field>(field => field
                .Add(x => x.Label, "姓名")
                .AddChildContent<TextField>(text => text
                    .Add(x => x.Value, model.Name)
                    .Add(x => x.ValueExpression, () => model.Name))));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            var link = cut.Find("[data-part=summary-list] a");
            Assert.Equal("姓名：请填写姓名", link.TextContent.Trim());
            Assert.Equal(cut.Find("input").Id, link.GetAttribute("data-rune-focus"));
        });
        Assert.Equal("alert", cut.Find("[data-part=summary]").GetAttribute("role"));
    }

    [Fact]
    public async Task 表单_提交期间提交按钮显示加载中()
    {
        var model = new Signup { Name = "张三" };
        var release = new TaskCompletionSource();
        var cut = Render<Form>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.OnValidSubmit, EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Forms.EditContext>(this, () => release.Task))
            .AddChildContent<Button>(button => button.Add(x => x.Type, "submit").AddChildContent("保存")));

        var submitting = cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("button").GetAttribute("aria-busy")));
        release.SetResult();
        await submitting;
        cut.WaitForAssertion(() => Assert.Null(cut.Find("button").GetAttribute("aria-busy")));
    }
}
