using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 条件（同 vue-tsx-pro 的 condition.ts）的求值与摘要，条件构造器的读、写：
/// 没填完整的比较不加条件，否定类的比较把空值算作满足，一组值按“有一项 / 没有一项”比；构造器只写回填完整的比较。
/// </summary>
public class ConditionBuilderTests : BunitContext
{
    public ConditionBuilderTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly IReadOnlyList<ConditionField> Fields =
    [
        new("amount", "金额", ConditionFieldType.Number),
        new("category", "品类", ConditionFieldType.Select) { Options = [new("steel", "钢材"), new("aluminum", "铝材")] },
        new("urgent", "加急", ConditionFieldType.Boolean),
        new("tags", "标签", ConditionFieldType.Text),
    ];

    static Func<string, object?> Row(decimal amount, string category, bool urgent, params string[] tags) => name => name switch
    {
        "amount" => amount,
        "category" => category,
        "urgent" => urgent,
        "tags" => tags,
        _ => null,
    };

    [Fact]
    public void 求值_组合与比较_没填完整的不加条件()
    {
        ConditionExpression condition = new ConditionAll(
        [
            new ConditionRule("amount", ConditionOp.Between, new List<object?> { 50000m, null }),
            new ConditionAny([new ConditionRule("category", ConditionOp.In, new List<object?> { "steel" }), new ConditionRule("urgent", ConditionOp.Eq, true)]),
            // 没填值：不加条件
            new ConditionRule("tags", ConditionOp.Contains, ""),
        ]);

        Assert.True(Conditions.Matches(condition, Row(80000, "steel", false)));
        Assert.True(Conditions.Matches(condition, Row(80000, "aluminum", true)));
        Assert.False(Conditions.Matches(condition, Row(80000, "aluminum", false)));
        Assert.False(Conditions.Matches(condition, Row(40000, "steel", true)));
        // 数字与数字文字相等
        Assert.True(Conditions.Matches(new ConditionRule("amount", ConditionOp.Eq, "80000"), Row(80000, "steel", false)));
    }

    [Fact]
    public void 求值_一组值按有一项满足_否定类按没有一项满足_空值算作不等于()
    {
        Assert.True(Conditions.Matches(new ConditionRule("tags", ConditionOp.Contains, "急"), Row(1, "steel", false, "加急", "复检")));
        Assert.False(Conditions.Matches(new ConditionRule("tags", ConditionOp.NotContains, "急"), Row(1, "steel", false, "加急", "复检")));
        Assert.True(Conditions.Matches(new ConditionRule("tags", ConditionOp.Empty), Row(1, "steel", false)));
        Assert.True(Conditions.Matches(new ConditionRule("missing", ConditionOp.Ne, "x"), _ => null));
        Assert.False(Conditions.Matches(new ConditionNot(new ConditionRule("urgent", ConditionOp.Eq, true)), Row(1, "steel", true)));
    }

    [Fact]
    public void 写成一句话_选项写文字_嵌套的组加括号()
    {
        ConditionExpression condition = new ConditionAll(
        [
            new ConditionRule("amount", ConditionOp.Gte, 100000m),
            new ConditionAny([new ConditionRule("category", ConditionOp.In, new List<object?> { "steel", "aluminum" }), new ConditionRule("urgent", ConditionOp.Eq, true)]),
        ]);

        Assert.Equal("金额 大于等于 100,000，且 （品类 是其中之一 钢材、铝材，或 加急 为 是）", Conditions.Describe(condition, Fields, Lang.ZhCn));
        Assert.Equal(3, Conditions.CountRules(condition));
    }

    [Fact]
    public void 构造器_读进条件_否定读成都不满足_只写回填完整的比较()
    {
        ConditionExpression? written = null;
        var cut = Render<ConditionBuilder>(p => p
            .Add(x => x.Fields, Fields)
            .Add(x => x.Value, new ConditionNot(new ConditionAny([new ConditionRule("urgent", ConditionOp.Eq, true)])))
            .Add(x => x.ValueChanged, v => written = v));

        Assert.Single(cut.FindAll("[data-condition-rule]"));
        Assert.Contains("都不满足", cut.Find("""[data-scope="condition-builder"] button[role="combobox"]""").TextContent);

        // 加一条空的：界面上多一条，写回的条件不变（没有调用）
        cut.FindAll("button").First(b => b.TextContent.Contains("添加条件")).Click();
        Assert.Equal(2, cut.FindAll("[data-condition-rule]").Count);
        Assert.Null(written);

        // 删掉填完整的那条：只剩空的一条，写回 null
        cut.FindAll("[data-condition-rule] button[aria-label=\"删除条件\"]")[0].Click();
        Assert.Single(cut.FindAll("[data-condition-rule]"));
        Assert.Null(written);
    }
}
