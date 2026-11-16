using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 分步表单（同 vue-tsx-pro 的 StepsForm）：下一步只校验这一步的字段；最后一步整份提交，没通过时跳回第一个有错的步骤；
/// 只读时不校验、没有提交按钮。
/// </summary>
public class StepsFormTests : BunitContext
{
    public StepsFormTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed class Account
    {
        [Required(ErrorMessage = "请填写公司")] public string? Company { get; set; }

        [Required(ErrorMessage = "请填写联系人")] public string? Contact { get; set; }
    }

    static IReadOnlyList<FormStep> StepsOf(Account model) =>
    [
        new("企业信息", [nameof(Account.Company)], b => b.AddMarkupContent(0, "<p>第一步</p>")),
        new("联系人", [nameof(Account.Contact)], b => b.AddMarkupContent(0, "<p>第二步</p>")),
    ];

    [Fact]
    public async Task 下一步只校验这一步的字段_通过才往后走()
    {
        var model = new Account();
        var current = 0;
        var cut = Render<StepsForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Steps, StepsOf(model))
            .Add(x => x.CurrentChanged, v => current = v));

        Assert.Equal("第 1 步，共 2 步", cut.Find("""[data-part="panel"]""").GetAttribute("aria-label"));
        await cut.InvokeAsync(() => cut.Find("form").Submit());
        Assert.Equal(0, current);
        Assert.Contains("第一步", cut.Find("""[data-part="panel"]""").TextContent);
        // 第一步出错，第二步的字段还没校验
        Assert.Equal("1", cut.Find("""[data-part="panel"]""").GetAttribute("data-rune-invalid-submit"));

        model.Company = "华辰";
        await cut.InvokeAsync(() => cut.Find("form").Submit());
        Assert.Equal(1, current);
        Assert.Contains("第二步", cut.Find("""[data-part="panel"]""").TextContent);
        Assert.Equal("第 2 步，共 2 步", cut.Find("""[data-part="panel"]""").GetAttribute("aria-label"));
        Assert.Contains("提交", cut.FindAll("button").Last().TextContent);
    }

    [Fact]
    public async Task 最后一步整份提交没通过时_跳回第一个有错的步骤并标成出错()
    {
        var model = new Account { Contact = "周岚" };
        var submitted = 0;
        var cut = Render<StepsForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Steps, StepsOf(model))
            .Add(x => x.Current, 1)
            .Add(x => x.OnValidSubmit, (EditContext _) => submitted++));

        await cut.InvokeAsync(() => cut.Find("form").Submit());
        Assert.Equal(0, submitted);
        Assert.Contains("第一步", cut.Find("""[data-part="panel"]""").TextContent);
        Assert.Contains("出错", cut.Find("""[data-scope="steps"]""").TextContent);
    }

    [Fact]
    public async Task 只读时下一步不校验_最后一步没有提交按钮()
    {
        var model = new Account();
        var cut = Render<StepsForm>(p => p.Add(x => x.Model, model).Add(x => x.Steps, StepsOf(model)).Add(x => x.ReadOnly, true));

        await cut.InvokeAsync(() => cut.Find("form").Submit());
        Assert.Contains("第二步", cut.Find("""[data-part="panel"]""").TextContent);
        Assert.Empty(cut.FindAll("""[data-part="panel"] [aria-invalid="true"]"""));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("提交") || b.TextContent.Contains("下一步"));
    }
}
