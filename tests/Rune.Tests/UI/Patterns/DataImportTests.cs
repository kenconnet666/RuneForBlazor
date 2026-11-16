using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 数据导入（同 vue-tsx-pro 的 DataImport 与 sheet.ts）：CSV 读写、多行表头与拆行；
/// 按名称与别名自动对上列，必填没对上时不能往下走；逐行校验、就地改后重新校验；导入失败时留在预览。
/// </summary>
public class DataImportTests : BunitContext
{
    public DataImportTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    sealed record Part(string Code, decimal? Quantity);

    static readonly IReadOnlyList<ImportField> Fields =
    [
        new("part", "零件号") { Required = true, Aliases = ["料号"] },
        new("quantity", "数量") { Type = ImportFieldType.Number },
    ];

    static Part ToPart(IReadOnlyDictionary<string, object?> row) => new((string)row["part"]!, (decimal?)row["quantity"]);

    [Fact]
    public void CSV_引号里的逗号换行与两个引号_写出再读回不变()
    {
        IReadOnlyList<IReadOnlyList<object?>> matrix = [["料号", "备注"], ["P-1", "含逗号, 与\"引号\""], ["P-2", "两行\n说明"]];

        var csv = Sheets.ToCsv(matrix);
        var read = Sheets.ParseCsv("﻿" + csv + "\r\n");

        Assert.Equal(matrix.Select(row => row.Cast<string>()), read.Select(row => row.Cast<string>()));
        Assert.StartsWith("data:text/csv;charset=utf-8,%EF%BB%BF", Sheets.CsvDataUrl(matrix));
    }

    [Fact]
    public void 矩阵转表_多行表头连成组与列_重复标题加序号_全空的行去掉_按列拆行()
    {
        IReadOnlyList<IReadOnlyList<object?>> matrix =
        [
            ["零件", "数量", "数量", "备注"],
            ["零件", "计划", "实际", "备注"],
            ["P-1\nP-2", 10, 9, "x"],
            [null, "  ", null, null],
        ];

        var sheet = Sheets.FromMatrix("报价", matrix, new() { HeaderRows = 2, SplitLines = ["零件"] });

        Assert.Equal(["零件", "数量 / 计划", "数量 / 实际", "备注"], sheet.Headers);
        Assert.Equal(2, sheet.Rows.Count);
        Assert.Equal("P-2", sheet.Rows[1]["零件"]);
        // 没拆的列照抄
        Assert.Equal(10, sheet.Rows[1]["数量 / 计划"]);

        var duplicated = Sheets.FromMatrix("x", [["备注", "备注", null], ["a", "b", "c"]]);
        Assert.Equal(["备注", "备注 2", "第 3 列"], duplicated.Headers);
    }

    IRenderedComponent<DataImport<Part>> Upload(string csv, Func<IReadOnlyList<Part>, Task> onImport, Func<IReadOnlyDictionary<string, object?>, IReadOnlyDictionary<string, string>?>? validate = null)
    {
        var cut = Render<DataImport<Part>>(p => p
            .Add(x => x.Fields, Fields)
            .Add(x => x.ToRow, ToPart)
            .Add(x => x.Validate, validate)
            .Add(x => x.OnImport, onImport));
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(csv, "parts.csv"));
        return cut;
    }

    [Fact]
    public async Task 按别名对上列_逐行校验_就地改后重新校验_导入通过的行()
    {
        IReadOnlyList<Part>? imported = null;
        var cut = Upload("料号,数量（件）\nP-1,\"1,200\"\n,3\nP-3,三百\n", rows =>
        {
            imported = rows;
            return Task.CompletedTask;
        });

        // 对应列：零件号认出“料号”、数量认出“数量（件）”（括号里的说明不算）
        var selects = cut.FindComponents<Select<string>>();
        Assert.Equal(["料号", "数量（件）"], selects.Select(s => s.Instance.Value));
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "下一步").Click();

        Assert.Contains("共 3 行，可以导入 1 行，2 行有错", cut.Markup);
        var table = cut.FindComponent<DataTable<ImportRow>>();
        var rows = table.Instance.Items!.ToList();
        Assert.Equal(Lang.ZhCn.FormRequired, rows[1].Errors["part"]);
        Assert.Equal(Lang.ZhCn.Patterns.DataImportNotNumber, rows[2].Errors["quantity"]);

        // 就地改：补上零件号，这一行通过
        await cut.InvokeAsync(() => table.Instance.OnCellChange.InvokeAsync(new CellChange<ImportRow>(rows[1], "part", "P-2")));
        Assert.Contains("共 3 行，可以导入 2 行，1 行有错", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "导入 2 行").Click();
        Assert.Equal([new Part("P-1", 1200m), new Part("P-2", 3m)], imported);
        Assert.Contains("已导入 2 行", cut.Markup);
    }

    [Fact]
    public void 必填的字段没对上_不能往下走()
    {
        var cut = Upload("编号,数量\nP-1,1\n", _ => Task.CompletedTask);

        Assert.Contains("必填的字段还没对应：零件号", cut.Markup);
        Assert.True(cut.FindAll("button").Single(b => b.TextContent.Trim() == "下一步").HasAttribute("disabled"));
    }

    [Fact]
    public void Validate补业务规则_导入失败时留在预览并说出原因()
    {
        var cut = Upload("零件号,数量\nP-1,1\nX-2,2\n", _ => throw new InvalidOperationException("库存服务不可用"),
            row => row["part"] is string code && !code.StartsWith("P-", StringComparison.Ordinal) ? new Dictionary<string, string> { ["part"] = "零件号是 P- 开头" } : null);
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "下一步").Click();

        Assert.Contains("零件号是 P- 开头", cut.Markup);
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "导入 1 行").Click();

        Assert.Contains("库存服务不可用", cut.Markup);
        Assert.NotNull(cut.FindComponent<DataTable<ImportRow>>());
    }

    [Fact]
    public void 读不出数据行_提示文件是空的()
    {
        var cut = Upload("零件号,数量\n", _ => Task.CompletedTask);

        Assert.Contains(Lang.ZhCn.Patterns.DataImportEmpty, cut.Markup);
        Assert.Empty(cut.FindComponents<Select<string>>());
    }
}
