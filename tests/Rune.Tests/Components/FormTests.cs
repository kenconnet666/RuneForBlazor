using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.Components;

public class FormTests : BunitContext
{
    public FormTests() => Services.AddRuneUI();

    sealed class Model
    {
        [Required(ErrorMessage = "请填写邮箱")]
        public string? Email { get; set; }

        public bool Agreed { get; set; }
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
        var cut = Render<Rune.Tests.Components.TestComponents.FormHost>();

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
}
