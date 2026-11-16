using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 按模型生成的表单（同 vue-tsx-pro 的 SchemaForm）：标签、分组、控件按模型；显示条件与权限；条件必填随提交报错；
/// 列表加行、删行，明细表的合计。
/// </summary>
public partial class SchemaFormTests : BunitContext
{
    public SchemaFormTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public enum Category
    {
        [Display(Name = "机械件")] Machined,
        [Display(Name = "塑料件")] Plastic,
    }

    [FormSchema]
    public partial class Line
    {
        [Display(Name = "零件号")] public string? Part { get; set; }

        [Display(Name = "数量"), FormField(Sum = true, Unit = "件")] public int Quantity { get; set; }
    }

    [FormSchema]
    public partial class Application
    {
        [Display(Name = "公司全称", GroupName = "企业信息"), Required(ErrorMessage = "请填写公司")]
        public string? Company { get; set; }

        [Display(Name = "供货品类", GroupName = "企业信息")] public Category? Category { get; set; }

        [Display(Name = "境外企业")] public bool Overseas { get; set; }

        [Display(Name = "境外注册号"), FormField(VisibleWhen = nameof(Overseas), RequiredWhen = nameof(Overseas))]
        public string? ForeignId { get; set; }

        [Display(Name = "备注")] public string? Note { get; set; }

        [Display(Name = "报价明细"), FormField(Control = SchemaControl.Table)] public List<Line> Lines { get; set; } = [];
    }

    IRenderedComponent<SchemaForm<Application>> RenderForm(Application model, Action<ComponentParameterCollectionBuilder<SchemaForm<Application>>>? extra = null) =>
        Render<SchemaForm<Application>>(p =>
        {
            p.Add(x => x.Model, model);
            extra?.Invoke(p);
        });

    static IEnumerable<string> Labels(IRenderedComponent<SchemaForm<Application>> cut) =>
        cut.FindAll("label").Select(l => l.TextContent.Trim()).Where(t => t.Length > 0);

    [Fact]
    public void 按模型生成_没分组的在前_分组成段_枚举是下拉_必填有标记()
    {
        var cut = RenderForm(new());

        var sections = cut.FindAll("[data-scope=\"form-section\"][data-part=\"root\"], section").Select(s => s.TextContent).ToList();
        Assert.Contains(Labels(cut), l => l == "境外企业");
        Assert.DoesNotContain(Labels(cut), l => l.StartsWith("境外注册号", StringComparison.Ordinal));
        Assert.Contains("企业信息", cut.Markup);
        // 没分组的一组在“企业信息”前面
        Assert.True(cut.Markup.IndexOf("境外企业", StringComparison.Ordinal) < cut.Markup.IndexOf("企业信息", StringComparison.Ordinal));
        Assert.NotNull(cut.FindAll("label").First(l => l.TextContent.Contains("公司全称")).QuerySelector("svg"));
        Assert.Single(cut.FindAll("button[role=\"combobox\"]"));
        Assert.Equal("报价明细", cut.Find("table").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task 显示条件成立后出现_条件必填随提交报错()
    {
        var model = new Application { Company = "华辰" };
        var submitted = 0;
        var cut = RenderForm(model, p => p.Add(x => x.OnValidSubmit, _ => submitted++));

        cut.Find("input[role=\"switch\"]").Change(true);
        Assert.True(model.Overseas);
        Assert.Contains(Labels(cut), l => l.StartsWith("境外注册号", StringComparison.Ordinal));

        await cut.InvokeAsync(() => cut.Find("form").Submit());
        Assert.Equal(0, submitted);
        Assert.Contains(Lang.ZhCn.FormRequired, cut.Markup);

        cut.FindAll("input").First(i => i.GetAttribute("type") is null or "text" && i.Id == cut.FindAll("label").First(l => l.TextContent.Contains("境外注册号")).GetAttribute("for")).Change("HK-2026");
        await cut.InvokeAsync(() => cut.Find("form").Submit());
        Assert.Equal(1, submitted);
    }

    [Fact]
    public void 明细表加行删行_合计带单位_权限只读时不能增删()
    {
        var model = new Application { Lines = [new() { Part = "P-1", Quantity = 1200 }, new() { Part = "P-2", Quantity = 34 }] };
        var cut = RenderForm(model);

        Assert.Equal("1,234 件", cut.Find("tfoot td:nth-child(2)").TextContent.Trim());
        Assert.Equal("合计", cut.Find("tfoot td:first-child").TextContent.Trim());
        // 格子里的控件以“列名（第几行）”为名
        Assert.Contains(cut.FindAll("label"), l => l.TextContent.Contains("零件号（第 2 个报价明细）"));

        cut.FindAll("button").First(b => b.TextContent.Contains("添加")).Click();
        Assert.Equal(3, model.Lines.Count);
        Assert.Equal(3, cut.FindAll("tbody tr").Count);
        Assert.NotNull(cut.FindAll("tbody tr").Last().GetAttribute("data-rune-focus-first"));

        cut.FindAll("tbody tr")[0].QuerySelector("button[aria-label*=\"报价明细\"]")!.Click();
        Assert.Equal(["P-2", null], model.Lines.Select(l => l.Part));

        cut.Render(p => p.Add(x => x.Access, new Dictionary<string, SchemaFieldAccess> { ["Lines"] = SchemaFieldAccess.ReadOnly, ["Note"] = SchemaFieldAccess.Hidden }));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("添加"));
        Assert.DoesNotContain(Labels(cut), l => l == "备注");
    }
}
