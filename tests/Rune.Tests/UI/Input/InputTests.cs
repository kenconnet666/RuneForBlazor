using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class InputTests : BunitContext
{
    public InputTests() => Services.AddRuneUI();

    [Fact]
    public void 分组格式_去掉分隔过滤字符截到最长_按组显示()
    {
        Assert.Equal("13800001111", TextFormat.Phone.Parse("138 0000 1111 99"));
        Assert.Equal("138 0000 1111", TextFormat.Phone.Apply("13800001111"));
        Assert.Equal("11010519491231002X", TextFormat.IdCard.Parse("110105 19491231 002x"));
        Assert.Equal("6222 0212 3456 7890 123", TextFormat.BankCard.Apply(TextFormat.BankCard.Parse("6222abc02123456789012345")));
        Assert.Equal("12-345-678", new TextFormat([2, 3]) { Separator = "-" }.Apply("12345678"));
    }

    [Theory]
    [InlineData("13800001111", true)]
    [InlineData("12800001111", false)]
    [InlineData("1380000111", false)]
    [InlineData("", true)]
    public void 手机号校验(string value, bool valid) => Assert.Equal(valid, new ChinaPhoneAttribute().IsValid(value));

    [Theory]
    [InlineData("11010519491231002X", true)]
    [InlineData("110105194912310021", false)]
    [InlineData("11010519490231002X", false)]
    public void 身份证号校验_出生日期与校验码(string value, bool valid) => Assert.Equal(valid, IdCardAttribute.IsValidIdCard(value));

    [Fact]
    public void 文本框的格式_显示分组_写回原文()
    {
        string? value = "13800001111";
        var cut = Render<TextField>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v))
            .Add(x => x.Format, TextFormat.Phone));

        var input = cut.Find("input");
        Assert.Equal(("138 0000 1111", "3,4,4", "tel"), (input.GetAttribute("value"), input.GetAttribute("data-rune-format"), input.GetAttribute("inputmode")));
        input.Change("139 1234 5678");
        Assert.Equal("13912345678", value);
    }

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
    public void 数字输入框_百分比的格式按百分数编辑()
    {
        var discount = 0.85m;
        var cut = Render<NumberField<decimal>>(p => p
            .Add(x => x.Value, discount)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<decimal>(this, v => discount = v))
            .Add(x => x.Min, 0)
            .Add(x => x.Max, 1)
            .Add(x => x.Step, 0.05m)
            .Add(x => x.Format, "P0"));

        // 显示 85%；给键盘加减（JS）的范围与步长都按百分数
        var input = cut.Find("input");
        // 按语言包的文化（zh-CN）格式化，不跟运行测试的机器的文化走
        Assert.Equal("85%", input.GetAttribute("value"));
        Assert.Equal(("85", "0", "100", "5"), (input.GetAttribute("aria-valuenow"), input.GetAttribute("aria-valuemin"), input.GetAttribute("aria-valuemax"), input.GetAttribute("data-step")));

        // 输入 90 得 0.9（之前被夹成 1）；带着百分号改也认
        input.Change("90");
        Assert.Equal(0.9m, discount);
        cut.Render(p => p.Add(x => x.Value, discount));
        cut.Find("input").Change("45 %");
        Assert.Equal(0.45m, discount);

        Assert.True(NumberText.IsPercent("p2") && NumberText.IsPercent("0.#%"));
        Assert.False(NumberText.IsPercent("C2") || NumberText.IsPercent(null) || NumberText.IsPercent("0.00"));
        // 引号里的、转义的 % 是原样的文字，值不乘 100
        Assert.False(NumberText.IsPercent("0'%'") || NumberText.IsPercent(@"0\%") || NumberText.IsPercent("0.0 \"% 浓度\""));
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
        Assert.Contains(registry.Rules, r => r.Contains(":has(> :is(input, select, textarea, [role=combobox]):disabled, > [data-rune-date-segments][data-disabled])"));
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
    public void 卡片外观_组里各项跟着_选中按input的勾选_横排按最小宽度分列_结束边内容()
    {
        var cut = Render<RadioGroup<string>>(group => group
            .Add(x => x.Card, true)
            .Add(x => x.Orientation, Orientation.Horizontal)
            .Add(x => x.MinItemWidth, Rune.Styles.CssLength.Rem(10))
            .AddChildContent<Radio<string>>(radio => radio.Add(x => x.Value, "team").Add(x => x.End, b => b.AddContent(0, "¥99")).AddChildContent("团队版"))
            .AddChildContent<Radio<string>>(radio => radio.Add(x => x.Value, "free").Add(x => x.Card, false).AddChildContent("个人版")));
        var rules = Services.GetRequiredService<Rune.Styles.StyleRegistry>().Rules;
        var labels = cut.FindAll("label");

        Assert.Equal("--rune-choice-min-item-width:10rem", cut.Find("[role=radiogroup]").GetAttribute("style"));
        Assert.Contains(rules, rule => labels[0].ClassList.Any(c => rule.Contains($".{c}:has(input:checked)")));
        Assert.DoesNotContain(rules, rule => labels[1].ClassList.Any(c => rule.Contains($".{c}:has(input:checked)")));
        Assert.Equal("¥99", cut.Find("[data-part=end]").TextContent);
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
            var link = cut.Find("[data-part=summaryList] a");
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
