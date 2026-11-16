using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 表单对话框（同 vue-tsx-pro 的 FormDialog 与 useFormDialog）：命令式的 PromptAsync 校验、提交被拒时留着、得到值；
/// 组件的确认按钮就是表单的提交按钮，校验没过、OnSubmit 设了 KeepOpen 时留着。
/// </summary>
public class FormDialogTests : BunitContext
{
    public FormDialogTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    [Fact]
    public async Task 命令式_必填与校验的错误在字段下_不提交_通过后得到去掉空白的值()
    {
        var host = Render<DialogHost>();
        var dialogs = Services.GetRequiredService<DialogService>();
        var submits = 0;

        Task<FormDialogValues?> asking = null!;
        await host.InvokeAsync(() =>
        {
            asking = dialogs.PromptAsync(new()
            {
                Title = "退回收件箱",
                Fields =
                [
                    new("reason", "原因") { Type = FormDialogFieldType.Textarea, Required = true, Validate = (v, _) => v.Length < 6 ? "至少写 6 个字" : null },
                    new("to", "抄送") { Type = FormDialogFieldType.Select, Multiple = true, Options = [new("a", "甲"), new("b", "乙")], DefaultValues = ["b"] },
                ],
                // 第一次提交被后端拒绝：对话框留着
                OnSubmit = _ => Task.FromResult(++submits > 1),
            });
        });
        host.WaitForAssertion(() => Assert.Single(host.FindAll("textarea")));
        var confirm = () => host.FindAll("dialog button").Last();

        await host.InvokeAsync(() => confirm().Click());
        host.WaitForAssertion(() => Assert.Contains("必填", host.Markup));
        await host.InvokeAsync(() => host.Find("textarea").Change("太短"));
        await host.InvokeAsync(() => confirm().Click());
        host.WaitForAssertion(() => Assert.Contains("至少写 6 个字", host.Markup));
        Assert.Equal(0, submits);

        await host.InvokeAsync(() => host.Find("textarea").Change("  料号不对，要重新下单  "));
        await host.InvokeAsync(() => confirm().Click());
        Assert.Equal(1, submits);
        Assert.False(asking.IsCompleted);

        await host.InvokeAsync(() => confirm().Click());
        var values = await asking.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.NotNull(values);
        Assert.Equal("料号不对，要重新下单", values["reason"]);
        Assert.Equal(["b"], values.All("to"));
    }

    sealed class Department
    {
        [Required(ErrorMessage = "请填部门名称")]
        public string? Name { get; set; }
    }

    [Fact]
    public void 组件_确认按钮提交里面的表单_拒绝的说明在顶上()
    {
        var cut = Render<FormDialog>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Title, "新建部门")
            .Add(x => x.Model, new Department())
            .Add(x => x.Size, DialogSize.Sm)
            .Add(x => x.Error, "部门名称已存在：采购部")
            .AddChildContent("<input name=\"name\" />"));

        var form = cut.Find("form");
        var confirm = cut.FindAll("dialog button").Last();
        Assert.Equal(("submit", form.Id), (confirm.GetAttribute("type"), confirm.GetAttribute("form")));
        Assert.Contains("部门名称已存在：采购部", cut.Find("""[data-scope="alert"]""").TextContent);
        Assert.Equal("新建部门", cut.Find("dialog").GetAttribute("aria-label") ?? cut.Find("dialog h2").TextContent);
    }

    [Fact]
    public async Task 组件_在字段里按Enter提交_成功后自己关上_KeepOpen时留着()
    {
        var model = new Department { Name = "质量部" };
        var open = true;
        var keep = true;
        var cut = Render<FormDialog>(p => p
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, v => open = v)
            .Add(x => x.Title, "新建部门")
            .Add(x => x.Model, model)
            .Add(x => x.OnSubmit, (FormDialogSubmitEventArgs e) => e.KeepOpen = keep));

        // 没有点确认，直接提交表单（Enter）
        await cut.InvokeAsync(() => cut.Find("form").Submit());
        Assert.True(open);

        keep = false;
        await cut.InvokeAsync(() => cut.Find("form").Submit());
        Assert.False(open);
    }
}
