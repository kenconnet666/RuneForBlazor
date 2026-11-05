using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

// 切换按钮组、复选框组、组合输入；不受控（bUnit 的 Bind 不会把新值传回组件），从 aria-pressed、checked 读值
public class GroupTests : BunitContext
{
    public GroupTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static RenderFragment Items(params string[] values) => builder =>
    {
        foreach (var value in values)
        {
            builder.OpenComponent<ToggleItem>(0);
            builder.SetKey(value);
            builder.AddAttribute(1, nameof(ToggleItem.Value), value);
            builder.AddAttribute(2, "aria-label", value);
            builder.CloseComponent();
        }
    };

    static string[] Pressed(IRenderedComponent<ToggleGroup> cut) =>
        [.. cut.FindAll("button[aria-pressed=true]").Select(b => b.GetAttribute("aria-label")!)];

    void Click(IRenderedComponent<ToggleGroup> cut, string value) => cut.Find($"button[aria-label={value}]").Click();

    [Fact]
    public void 单选_再点已选项弹起_必填时不弹起_滑块只在有选中时出现()
    {
        var cut = Render<ToggleGroup>(p => p.Add(x => x.Value, "a").Add(x => x.ChildContent, Items("a", "b")));
        Assert.Equal("group", cut.Find("div").GetAttribute("role"));
        Assert.Equal("horizontal", cut.Find("div").GetAttribute("data-rune-roving"));
        Assert.NotNull(cut.Find("[data-rune-indicator]"));

        Click(cut, "b");
        Assert.Equal(["b"], Pressed(cut));
        Click(cut, "b");
        Assert.Empty(Pressed(cut));
        Assert.Empty(cut.FindAll("[data-rune-indicator]"));

        var required = Render<ToggleGroup>(p => p.Add(x => x.Value, "a").Add(x => x.Required, true).Add(x => x.ChildContent, Items("a", "b")));
        Click(required, "a");
        Assert.Equal(["a"], Pressed(required));
    }

    [Fact]
    public void 多选_各项各自开关_没有滑块()
    {
        var cut = Render<ToggleGroup>(p => p
            .Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.Values, ["bold"])
            .Add(x => x.ChildContent, Items("bold", "italic", "underline")));

        Click(cut, "underline");
        Click(cut, "bold");
        Assert.Equal(["underline"], Pressed(cut));
        Assert.Empty(cut.FindAll("[data-rune-indicator]"));
    }

    [Fact]
    public void 竖排_不可用_只读时点击不变()
    {
        var vertical = Render<ToggleGroup>(p => p.Add(x => x.Orientation, Orientation.Vertical).Add(x => x.ChildContent, Items("a")));
        Assert.Equal("vertical", vertical.Find("div").GetAttribute("data-rune-roving"));
        Assert.Equal("vertical", vertical.Find("div").GetAttribute("data-orientation"));

        var disabled = Render<ToggleGroup>(p => p.Add(x => x.Value, "a").Add(x => x.Disabled, true).Add(x => x.ChildContent, Items("a", "b")));
        Assert.Equal("true", disabled.Find("div").GetAttribute("aria-disabled"));
        Assert.All(disabled.FindAll("button"), b => Assert.True(b.HasAttribute("disabled")));

        var readOnly = Render<ToggleGroup>(p => p.Add(x => x.Value, "a").Add(x => x.ReadOnly, true).Add(x => x.ChildContent, Items("a", "b")));
        Click(readOnly, "b");
        Assert.Equal(["a"], Pressed(readOnly));
    }

    [Fact]
    public void 放在Field里_组以标签为名称_写了aria_label时不再labelledby()
    {
        var cut = Render<Field>(p => p.Add(x => x.Label, "视图").Add(x => x.Description, "说明").AddChildContent<ToggleGroup>(g => g.Add(x => x.ChildContent, Items("a"))));
        var group = cut.Find("[role=group]");
        Assert.Equal("视图", cut.Find($"#{group.GetAttribute("aria-labelledby")}").TextContent.Trim());
        Assert.Equal("说明", cut.Find($"#{group.GetAttribute("aria-describedby")}").TextContent.Trim());

        var labelled = Render<Field>(p => p.Add(x => x.Label, "视图").AddChildContent<ToggleGroup>(g => g
            .AddUnmatched("aria-label", "布局").Add(x => x.ChildContent, Items("a"))));
        Assert.False(labelled.Find("[role=group]").HasAttribute("aria-labelledby"));
    }

    static RenderFragment Boxes(params string[] values) => builder =>
    {
        foreach (var value in values)
        {
            builder.OpenComponent<Checkbox>(0);
            builder.SetKey(value);
            builder.AddAttribute(1, nameof(Checkbox.GroupValue), value);
            builder.AddAttribute(2, nameof(Checkbox.ChildContent), (RenderFragment)(b => b.AddContent(0, value)));
            builder.CloseComponent();
        }
    };

    static string[] Checked(IRenderedComponent<CheckboxGroup<string>> cut) =>
        [.. cut.FindAll("input[type=checkbox]").Where(i => i.HasAttribute("checked")).Select(i => i.GetAttribute("value")!)];

    [Fact]
    public void 复选框组_勾选进列表_Name随表单提交()
    {
        var cut = Render<CheckboxGroup<string>>(p => p.Add(x => x.Value, ["email"]).Add(x => x.Name, "channels").Add(x => x.ChildContent, Boxes("email", "sms", "push")));
        Assert.Equal("group", cut.Find("div").GetAttribute("role"));
        Assert.All(cut.FindAll("input"), i => Assert.Equal("channels", i.GetAttribute("name")));

        cut.FindAll("input")[2].Change(true);
        Assert.Equal(["email", "push"], Checked(cut));
        cut.FindAll("input")[0].Change(false);
        Assert.Equal(["push"], Checked(cut));
    }

    [Fact]
    public void 复选框组_按Items生成_说明与不可用_组的不可用传给各项()
    {
        ListboxOption[] options = [new("email", "邮件", Description: "每天一封"), new("sms", "短信", Disabled: true)];
        var cut = Render<CheckboxGroup<ListboxOption>>(p => p.Add(x => x.Items, options).Add(x => x.Value, ["email"]));
        var inputs = cut.FindAll("input");
        Assert.Equal("每天一封", cut.Find($"#{inputs[0].GetAttribute("aria-describedby")}").TextContent.Trim());
        Assert.True(inputs[1].HasAttribute("disabled"));
        Assert.Contains("短信", cut.Markup);

        var disabled = Render<CheckboxGroup<ListboxOption>>(p => p.Add(x => x.Items, options).Add(x => x.Disabled, true));
        Assert.All(disabled.FindAll("input"), i => Assert.True(i.HasAttribute("disabled")));
    }

    [Fact]
    public void 复选框组在Field里_组拿Field的id_里面的复选框不拿()
    {
        var cut = Render<Field>(p => p.Add(x => x.Label, "通知").Add(x => x.Error, "至少选一种")
            .AddChildContent<CheckboxGroup<string>>(g => g.Add(x => x.ChildContent, Boxes("email", "sms"))));
        var group = cut.Find("[role=group]");
        Assert.Equal(cut.Find("label[for]").GetAttribute("for"), group.Id);
        Assert.All(cut.FindAll("input"), i => Assert.NotEqual(group.Id, i.Id));
        // 组的校验未通过传给各项
        Assert.All(cut.FindAll("input"), i => Assert.Equal("true", i.GetAttribute("aria-invalid")));
    }

    [Fact]
    public void 组合输入在Field里_组拿Field的id与说明_里面的控件各有id()
    {
        var cut = Render<Field>(p => p.Add(x => x.Label, "金额").Add(x => x.Description, "说明")
            .AddChildContent<ControlGroup>(g => g.AddChildContent<TextField>(t => t.AddUnmatched("aria-label", "数额"))));
        var group = cut.Find("[role=group]");
        Assert.Equal(cut.Find("label[for]").GetAttribute("for"), group.Id);
        Assert.Equal("说明", cut.Find($"#{group.GetAttribute("aria-describedby")}").TextContent.Trim());
        var input = cut.Find("input");
        Assert.NotEqual(group.Id, input.Id);
        Assert.False(input.HasAttribute("aria-describedby"));
    }
}
