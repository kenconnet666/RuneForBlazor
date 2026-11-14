using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class FormTests : BunitContext
{
    public FormTests() => Services.AddRuneUI();

    sealed class Model
    {
        [Required(ErrorMessage = "请填写邮箱")]
        public string? Email { get; set; }

        public bool Agreed { get; set; }

        [Display(Name = "手机号")]
        public string? Phone { get; set; }

        [System.ComponentModel.DisplayName("备注")]
        public string? Note { get; set; }
    }

    sealed class Order
    {
        public string? Email { get; set; }
        public bool Agreed { get; set; }
        public int Quantity { get; set; }
        public List<Line> Lines { get; } = [new(), new()];
    }

    sealed class Line
    {
        public string? Sku { get; set; }
    }

    [Fact]
    public void 服务端渲染的表单里按字段生成name_写了Name以它为准()
    {
        var order = new Order();
        var index = 1;
        var context = new EditContext(order) { ShouldUseFieldIdentifiers = true };
        var cut = Render<CascadingValue<EditContext>>(p => p
            .Add(x => x.Value, context)
            .AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => order.Email))
            .AddChildContent<Checkbox>(box => box.Add(x => x.ValueExpression, () => order.Agreed))
            .AddChildContent<NumberField<int>>(number => number.Add(x => x.ValueExpression, () => order.Quantity).Add(x => x.Value, 3))
            .AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => order.Lines[index].Sku))
            .AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => order.Email).Add(x => x.Name, "contact")));

        var inputs = cut.FindAll("input:not([type=hidden])");
        Assert.Equal("order.Email", inputs[0].GetAttribute("name"));
        // 勾选框勾上时提交 true（同 InputCheckbox），绑得回 bool
        Assert.Equal("order.Agreed", inputs[1].GetAttribute("name"));
        Assert.Equal("true", inputs[1].GetAttribute("value"));
        // 数字框：看得见的输入框不带 name，提交的是隐藏框里的十进制文本
        Assert.Null(inputs[2].GetAttribute("name"));
        Assert.Equal("3", cut.Find("input[type=hidden][name='order.Quantity']").GetAttribute("value"));
        Assert.Equal("order.Lines[1].Sku", inputs[3].GetAttribute("name"));
        Assert.Equal("contact", inputs[4].GetAttribute("name"));
    }

    [Fact]
    public void 交互的WebAssembly表单不生成name()
    {
        var order = new Order();
        var context = new EditContext(order) { ShouldUseFieldIdentifiers = false };
        var cut = Render<CascadingValue<EditContext>>(p => p
            .Add(x => x.Value, context)
            .AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => order.Email)));
        Assert.Null(cut.Find("input").GetAttribute("name"));
    }

    [Fact]
    public void 开关_轨道里的文字对读屏隐藏_切换中拦下点击并转圈()
    {
        var cut = Render<Switch>(p => p
            .Add(x => x.CheckedContent, b => b.AddContent(0, "开"))
            .Add(x => x.UncheckedContent, b => b.AddContent(0, "关"))
            .AddUnmatched("aria-label", "暗色模式"));
        var contents = cut.FindAll("label > span > span[aria-hidden=true]");
        Assert.Equal(["开", "关"], contents.Select(c => c.TextContent));
        Assert.Null(cut.Find("input").GetAttribute("aria-busy"));

        cut.Render(p => p.Add(x => x.Loading, true));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-busy"));
        Assert.Equal("true", cut.Find("label").GetAttribute("data-loading"));
        // 转圈推迟 200ms 才出现（同 DelayedLoading）
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[role=switch] ~ span [data-scope=spinner]")), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void 控件自己写了id时_标签的for跟着指向它()
    {
        var cut = Render<Field>(p => p.Add(x => x.Label, "邮箱").AddChildContent<TextField>(text => text.AddUnmatched("id", "email")));
        Assert.Equal(("email", "email"), (cut.Find("input").Id, cut.Find("label").GetAttribute("for")));

        // 没写 id 的照旧用 Field 生成的
        var plain = Render<Field>(p => p.Add(x => x.Label, "邮箱").AddChildContent<TextField>());
        Assert.Equal(plain.Find("input").Id, plain.Find("label").GetAttribute("for"));
    }

    [Fact]
    public void 字段不写Required时看绑定字段的Required标注_写了以它为准()
    {
        var model = new Model();
        var marked = Render<Field>(p => p.Add(x => x.Label, "邮箱").AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => model.Email)));
        Assert.NotNull(marked.Find("label [data-part=requiredMark]"));
        Assert.Equal("true", marked.Find("input").GetAttribute("aria-required"));

        var plain = Render<Field>(p => p.Add(x => x.Label, "手机号").AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => model.Phone)));
        Assert.Empty(plain.FindAll("[data-part=requiredMark]"));
        Assert.Null(plain.Find("input").GetAttribute("aria-required"));

        var overridden = Render<Field>(p => p.Add(x => x.Label, "邮箱").Add(x => x.Required, false)
            .AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => model.Email)));
        Assert.Empty(overridden.FindAll("[data-part=requiredMark]"));
    }

    [Fact]
    public void 字段不写标签时取Display或DisplayName_写了标签以它为准()
    {
        var model = new Model();
        var cut = Render<Field>(p => p.AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => model.Phone)));
        var label = cut.Find("label");
        Assert.Equal("手机号", label.TextContent.Trim());
        Assert.Equal(label.Id, cut.Find("input").GetAttribute("aria-labelledby") ?? label.Id);
        Assert.Equal(cut.Find("input").Id, label.GetAttribute("for"));

        var note = Render<Field>(p => p.AddChildContent<Textarea>(text => text.Add(x => x.ValueExpression, () => model.Note)));
        Assert.Equal("备注", note.Find("label").TextContent.Trim());

        var given = Render<Field>(p => p.Add(x => x.Label, "联系电话").AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => model.Phone)));
        Assert.Equal("联系电话", given.Find("label").TextContent.Trim());

        // 没有标注、也没写标签：没有标签元素
        var bare = Render<Field>(p => p.AddChildContent<TextField>(text => text.Add(x => x.ValueExpression, () => model.Email)));
        Assert.Empty(bare.FindAll("label"));
    }

    [Fact]
    public void 文本框_失去焦点时更新值_使用方的属性落在input上()
    {
        string? value = null;
        var cut = Render<TextField>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v))
            .AddUnmatched("placeholder", "姓名"));

        var input = cut.Find("input");
        Assert.Equal("姓名", input.GetAttribute("placeholder"));
        input.Change("张三");
        Assert.Equal("张三", value);
        // 默认不在每次按键时更新
        Assert.Throws<MissingEventHandlerException>(() => input.Input("李"));
    }

    [Fact]
    public void 文本框_Immediate时每次输入都更新()
    {
        string? value = null;
        var cut = Render<TextField>(p => p
            .Add(x => x.Immediate, true)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v)));

        cut.Find("input").Input("a");

        Assert.Equal("a", value);
    }

    [Fact]
    public void 字段_标签指向控件_说明进aria_describedby_必填传给控件()
    {
        var cut = Render<Field>(p => p
            .Add(x => x.Label, "邮箱")
            .Add(x => x.Description, "用于找回密码")
            .Add(x => x.Required, true)
            .AddChildContent<TextField>());

        var input = cut.Find("input");
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal(cut.Find("[data-part=description]").Id, input.GetAttribute("aria-describedby"));
        // 必填写成 aria-required：原生 required 会拦下提交、绕过 EditForm 的校验
        Assert.Equal("true", input.GetAttribute("aria-required"));
        Assert.False(input.HasAttribute("required"));
        Assert.Null(input.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void 字段_带格式的说明代替文字说明_同样关联到控件()
    {
        var cut = Render<Field>(p => p
            .Add(x => x.Description, "不显示")
            .Add(x => x.DescriptionContent, b => b.AddMarkupContent(0, "已选：<code>email</code>"))
            .AddChildContent<TextField>());

        var description = cut.Find("[data-part=description]");
        Assert.Equal("email", description.QuerySelector("code")!.TextContent);
        Assert.DoesNotContain("不显示", description.TextContent);
        Assert.Equal(description.Id, cut.Find("input").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void 表单校验未通过_字段显示错误_控件标为aria_invalid()
    {
        var model = new Model();
        var context = new EditContext(model);
        var cut = Render<CascadingValue<EditContext>>(p => p
            .Add(x => x.Value, context)
            .AddChildContent<DataAnnotationsValidator>()
            .AddChildContent<Field>(field => field
                .Add(x => x.Label, "邮箱")
                .AddChildContent<TextField>(text => text
                    .Add(x => x.Value, model.Email)
                    .Add(x => x.ValueExpression, () => model.Email))));

        cut.InvokeAsync(() => context.Validate());

        cut.WaitForAssertion(() => Assert.Equal("请填写邮箱", cut.Find("[data-part=error]").TextContent.Trim()));
        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[data-part=error]").Id, input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void EditForm提交时校验_每个字段显示自己的错误()
    {
        var cut = Render<Rune.Tests.TestComponents.FormHost>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal(["请填写姓名", "请先同意"], cut.FindAll("[data-part=error]").Select(e => e.TextContent.Trim())));
        Assert.False(cut.Instance.Submitted);
        Assert.Equal(2, cut.FindAll("[aria-invalid=true]").Count);
    }

    [Fact]
    public void 改值后通知表单_字段重新校验()
    {
        var model = new Model();
        var context = new EditContext(model);
        var changed = new List<string>();
        context.OnFieldChanged += (_, e) => changed.Add(e.FieldIdentifier.FieldName);
        var cut = Render<CascadingValue<EditContext>>(p => p
            .Add(x => x.Value, context)
            .AddChildContent<Checkbox>(box => box
                .Add(x => x.Value, model.Agreed)
                .Add(x => x.ValueChanged, EventCallback.Factory.Create<bool>(this, v => model.Agreed = v))
                .Add(x => x.ValueExpression, () => model.Agreed)));

        cut.Find("input").Change(true);

        Assert.True(model.Agreed);
        Assert.Equal(["Agreed"], changed);
    }

    [Fact]
    public void 勾选框半选交给JS同步_开关是role_switch()
    {
        var checkbox = Render<Checkbox>(p => p.Add(x => x.Indeterminate, true).AddChildContent("全选"));
        var toggle = Render<Switch>(p => p.Add(x => x.Value, true));

        Assert.Equal("true", checkbox.Find("input").GetAttribute("data-indeterminate"));
        Assert.Equal("minus", checkbox.Find("svg").GetAttribute("data-icon"));
        Assert.Equal("switch", toggle.Find("input").GetAttribute("role"));
        Assert.True(toggle.Find("input").HasAttribute("checked"));
    }

    [Fact]
    public void 按钮默认有波纹_参数可以关掉_像链接的按钮没有()
    {
        var on = Render<Button>(p => p.AddChildContent("保存"));
        var off = Render<Button>(p => p.Add(x => x.Ripple, false).AddChildContent("保存"));
        var link = Render<Button>(p => p.Add(x => x.Variant, ButtonVariant.Link).AddChildContent("像链接"));

        Assert.True(on.Find("button").HasAttribute("data-rune-ripple"));
        Assert.False(off.Find("button").HasAttribute("data-rune-ripple"));
        Assert.False(link.Find("button").HasAttribute("data-rune-ripple"));
    }

    [Fact]
    public void 配置关掉波纹()
    {
        Services.AddRuneUI(o => o.Ripple = false);

        var globallyOff = Render<IconButton>(p => p.Add(x => x.Label, "编辑").Add(x => x.Icon, BuiltInIcons.Plus));
        Assert.False(globallyOff.Find("button").HasAttribute("data-rune-ripple"));
    }

    [Fact]
    public void 图标_色调与从右到左翻转_粗细取主题token()
    {
        var cut = Render<Icon>(p => p.Add(x => x.Data, BuiltInIcons.ChevronRight).Add(x => x.Tone, TextTone.Danger).Add(x => x.FlipInRtl, true));
        var thick = Render<Icon>(p => p.Add(x => x.Data, BuiltInIcons.Check).Add(x => x.StrokeWidth, 3));

        var registry = Services.GetRequiredService<Rune.Styles.StyleRegistry>();
        var rule = Assert.Single(registry.Rules, r => r.Contains($".{cut.Find("svg").ClassName}{{"));
        Assert.Contains("color:var(--rune-tones-danger-text)", rule);
        Assert.Contains("stroke-width:var(--rune-stroke-widths-md)", rule);
        Assert.Contains(":dir(rtl){transform:scaleX(-1);}", rule);
        Assert.Null(cut.Find("svg").GetAttribute("stroke-width"));
        Assert.Equal("stroke-width:3", thick.Find("svg").GetAttribute("style"));
    }

    [Fact]
    public void 勾选框的说明进aria_describedby()
    {
        var cut = Render<Checkbox>(p => p.Add(x => x.Description, "每周一封").Add(x => x.Tone, Tone.Success).AddChildContent("订阅周报"));

        var description = cut.Find("[data-part=description]");
        Assert.Equal("每周一封", description.TextContent);
        Assert.Equal(description.Id, cut.Find("input").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void 字段_标签在起始边_标签宽度经变量写在元素上()
    {
        var cut = Render<Field>(p => p
            .Add(x => x.Label, "姓名")
            .Add(x => x.LabelPlacement, FieldLabelPlacement.Start)
            .Add(x => x.LabelWidth, "6em")
            .AddChildContent<TextField>());

        var root = cut.Find("[data-scope=field][data-part=root]");
        Assert.Equal("--rune-field-label-width:6em", root.GetAttribute("style"));
        var registry = Services.GetRequiredService<Rune.Styles.StyleRegistry>();
        Assert.Contains(registry.Rules, r => r.Contains($".{root.ClassName}{{") && r.Contains("grid-template-columns:var(--rune-field-label-width, 8em) minmax(0, 1fr)"));
    }

    [Fact]
    public void 多行文本框在Rows与MaxRows之间自动增高_Fixed时可以拖动()
    {
        var growing = Render<Textarea>(p => p.Add(x => x.Rows, 2).Add(x => x.MaxRows, 6));
        var fixedHeight = Render<Textarea>(p => p.Add(x => x.Rows, 3).Add(x => x.MaxRows, 6).Add(x => x.Fixed, true));

        var registry = Services.GetRequiredService<Rune.Styles.StyleRegistry>();
        var rule = Assert.Single(registry.Rules, r => r.Contains($".{growing.Find("textarea").ClassName}{{"));
        Assert.Contains("min-height:2lh", rule);
        Assert.Contains("max-height:6lh", rule);
        Assert.True(growing.Find("textarea").HasAttribute("data-rune-autosize"));
        var fixedRule = Assert.Single(registry.Rules, r => r.Contains($".{fixedHeight.Find("textarea").ClassName}{{"));
        Assert.Contains("resize:vertical", fixedRule);
        Assert.DoesNotContain("max-height", fixedRule);
        Assert.False(fixedHeight.Find("textarea").HasAttribute("data-rune-autosize"));
    }

    [Fact]
    public async Task 多行文本框清空与计数都有时叠在同一列_清空后计数还是原来的节点()
    {
        var value = "abc";
        var cut = Render<Textarea>(p => p.Add(x => x.Value, value).Add(x => x.Clearable, true).Add(x => x.ShowCount, true)
            .Add(x => x.ValueChanged, v => value = v ?? ""));

        // 外壳里是文本框与叠起来的那一列；那一列里清空在上、计数在下
        var column = cut.Find("textarea").NextElementSibling!;
        Assert.Equal(["BUTTON", "SPAN"], column.Children.Select(c => c.TagName));
        Assert.Equal("character-count", column.Children[1].GetAttribute("data-scope"));

        await cut.InvokeAsync(() => cut.Find("button").Click());
        cut.Render(p => p.Add(x => x.Value, value));
        Assert.Equal("", value);
        Assert.Empty(cut.FindAll("button"));
        // 计数仍在那一列里（不随清空按钮的有无换位置）
        Assert.Equal("character-count", cut.Find("textarea").NextElementSibling!.Children.Single().GetAttribute("data-scope"));

        // 只有计数时不多包一层
        var plain = Render<Textarea>(p => p.Add(x => x.Value, "abc").Add(x => x.ShowCount, true));
        Assert.Equal("character-count", plain.Find("textarea").NextElementSibling!.GetAttribute("data-scope"));
    }

    [Fact]
    public void 文本框的字数计数_清空与显示密码()
    {
        var cut = Render<TextField>(p => p.Add(x => x.Value, "abc").Add(x => x.ShowCount, true).Add(x => x.Clearable, true)
            .Add(x => x.Type, "password").AddUnmatched("maxlength", "20"));

        var input = cut.Find("input");
        var count = cut.Find("[data-scope=character-count]");
        Assert.Equal(count.Id, input.GetAttribute("data-rune-count"));
        Assert.Contains(count.Id!, input.GetAttribute("aria-describedby"));
        Assert.Equal("3 / 20", count.QuerySelector("[aria-hidden=true]")!.TextContent);
        Assert.Equal(input.Id, cut.Find("button[aria-label=清空]").GetAttribute("data-rune-refocus"));
        Assert.Equal(input.Id, cut.Find("button[aria-label=显示密码]").GetAttribute("data-rune-reveal"));
        cut.Find("button[aria-label=清空]").Click();
        Assert.Equal("", cut.Find("input").GetAttribute("value"));
    }
}
