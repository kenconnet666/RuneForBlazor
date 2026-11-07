using Rune.Styles;

namespace Rune.Tests.Styles;

/// <summary>
/// 分段输出（同 vue-tsx-pro 的阶段 1）：每段一个类、各进一层级联层，合并语义与合成一个类逐字相同（StyleCompiler 的说明）。
/// </summary>
public class SegmentTests
{
    static IReadOnlyList<CompiledStyle> Compile(params Style[][] segments) => StyleCompiler.CompileSegments(segments);

    [Fact]
    public void 只有一段时一个类_类名与不分段时相同_规则在第0层()
    {
        var a = new Style { Color = "rgb(1, 1, 1)" };
        var b = new Style { MarginTop = 3 };

        var single = Assert.Single(Compile([a, b]));
        Assert.Equal(StyleCompiler.Compile([a, b])!.ClassName, single.ClassName);
        Assert.StartsWith("@layer rune.s0{", single.Css);
    }

    [Fact]
    public void 分段时每段一个类_各在自己的层里_后段只带它改到的属性族()
    {
        var compiled = Compile([new Style { Color = "rgb(1, 1, 1)", Padding = 4 }], [new Style { MarginTop = 3 }]);

        Assert.Equal(2, compiled.Count);
        Assert.StartsWith("@layer rune.s0{", compiled[0].Css);
        Assert.Equal($"@layer rune.s1{{.{compiled[1].ClassName}{{margin-top:3px;}}}}", compiled[1].Css);
    }

    [Fact]
    public void 组件的档位与使用方的覆盖相加而不是相乘()
    {
        Style[] tones = [new() { Color = "rgb(2, 0, 0)", Padding = 1 }, new() { Color = "rgb(3, 0, 0)", Padding = 1 }, new() { Color = "rgb(4, 0, 0)", Padding = 1 }];
        Style[] overrides = [new() { MarginTop = 11 }, new() { MarginTop = 12 }];

        var seen = tones.SelectMany(tone => overrides.SelectMany(user => Compile([tone], [user]))).Select(c => c.ClassName).ToHashSet();

        // 3 + 2 个类，而不是 3 × 2 个各带全部声明的组合类
        Assert.Equal(5, seen.Count);
    }

    [Fact]
    public void 属性族一起带_前段在条件里写简写_后段写长属性时后段带上那条简写()
    {
        var ring = new Style { Outline = Outline.None, [When.FocusVisible] = new() { Outline = "2px solid blue" } };
        var blocks = StyleCompiler.SegmentBlocks([[ring, new Style { Display = Display.Flex }], [new Style { OutlineColor = "red" }]], 1);

        Assert.Equal(new Dictionary<string, string> { ["outline"] = "none", ["outline-color"] = "red" }, blocks[""]);
        Assert.Equal(new Dictionary<string, string> { ["outline"] = "2px solid blue" }, blocks[":focus-visible"]);
    }

    [Theory]
    [InlineData("margin-top", "margin")]
    [InlineData("top", "inset")]
    [InlineData("row-gap", "gap")]
    [InlineData("align-items", "place")]
    [InlineData("justify-content", "place")]
    [InlineData("line-height", "font")]
    [InlineData("white-space", "text")]
    [InlineData("columns", "column")]
    [InlineData("-webkit-line-clamp", "-webkit")]
    [InlineData("--rune-x", "--rune-x")]
    public void 属性族按名字的第一段分_几个例外(string property, string family) => Assert.Equal(family, StyleCompiler.FamilyOf(property));

    [Fact]
    public void 层的顺序_主题变量最低_各段依次往上() =>
        Assert.Equal("@layer rune.base,rune.vars,rune.s0,rune.s1,rune.s2,rune.s3,rune.s4,rune.s5,rune.s6,rune.s7;", StyleCompiler.LayerOrder);

    [Fact]
    public void 超过8段的并进最后一段()
    {
        var segments = Enumerable.Range(1, 10).Select(i => new[] { new Style { MarginTop = i } }).ToArray();

        var compiled = Compile(segments);

        Assert.Equal(StyleCompiler.MaxSegments, compiled.Count);
        Assert.Contains("rune.s7{", compiled[^1].Css);
        Assert.Contains("margin-top:10px", compiled[^1].Css);
    }

    // ───────── 随机比对：任意分段、任意状态下，按层叠选出的值与合成一个类时相同 ─────────

    static readonly string[] Properties = ["color", "margin", "margin-top", "outline", "outline-color"];

    // 比对用的属性族，与实现（FamilyOf）无关：简写与它的长属性在同一族
    static readonly Dictionary<string, string> Families = new()
    {
        ["color"] = "color", ["margin"] = "margin", ["margin-top"] = "margin", ["outline"] = "outline", ["outline-color"] = "outline",
    };
    static readonly Condition[] Conditions = [When.Hover, When.FocusVisible, Media.Up(1)];

    [Fact]
    public void 任意分段_任意状态下_每个属性族按层叠选出的取值与合成一个类时相同()
    {
        var random = new Random(20261106);
        for (var round = 0; round < 500; round++)
        {
            var fragments = Enumerable.Range(0, 2 + random.Next(5)).Select(_ => RandomFragment(random)).ToList();
            var segments = RandomSplit(fragments, random);
            var single = StyleCompiler.SegmentBlocks([[.. fragments.SelectMany(f => f)]], 0);
            var layered = segments.Select((_, index) => StyleCompiler.SegmentBlocks(segments, index)).ToList();

            foreach (var state in Subsets(Conditions.Select(c => c.Key).ToArray()))
            {
                foreach (var family in Families.Values.Distinct())
                {
                    var expected = FamilyIn(single, family, state);
                    // 层叠：最高的、对这一族在这个状态下有规则的层说了算（同一层里按特异性与书写顺序，同合成一个类）
                    var actual = new Dictionary<string, string>();
                    for (var index = layered.Count - 1; index >= 0 && actual.Count == 0; index--) actual = FamilyIn(layered[index], family, state);
                    Assert.True(expected.OrderBy(p => p.Key).SequenceEqual(actual.OrderBy(p => p.Key)),
                        $"第 {round} 轮 {family} @ [{string.Join(",", state)}]：期望 {Show(expected)}，实际 {Show(actual)}");
                }
            }
        }
    }

    /// <summary>这一层在这个状态下这一族各属性胜出的取值：条件都成立的规则里特异性最高、写得最后的（同 StyleCompiler 的规则顺序）</summary>
    static Dictionary<string, string> FamilyIn(Dictionary<string, Dictionary<string, string>> blocks, string family, IReadOnlySet<string> state)
    {
        var result = new Dictionary<string, string>();
        var applicable = blocks
            .Select(b => (Chain: b.Key, Parts: b.Key.Length == 0 ? [] : b.Key.Split('\u0001'), Declarations: b.Value))
            .Where(b => b.Parts.All(state.Contains))
            .OrderBy(b => b.Parts.Count(part => !part.StartsWith('@')))
            .ThenBy(b => b.Parts.Sum(StylePriority.OfCondition))
            .ThenBy(b => b.Chain, StringComparer.Ordinal);
        foreach (var block in applicable)
            foreach (var (property, value) in block.Declarations)
                if (Families[property] == family) result[property] = value;
        return result;
    }

    /// <summary>一段随机样式：几条声明，各在随机的条件链里（每条一个只含它的 Style，依次合并同写在一个 Style 里）</summary>
    static List<Style> RandomFragment(Random random)
    {
        var fragment = new List<Style>();
        foreach (var property in Properties)
        {
            if (random.NextDouble() < 0.4) continue;
            var chain = Conditions.Where(_ => random.NextDouble() < 0.35).ToArray();
            fragment.Add(Declare(property, $"v{random.Next(5)}", chain));
            if (random.NextDouble() < 0.3) fragment.Add(Declare(property, $"w{random.Next(5)}", []));
        }
        return fragment;
    }

    static Style Declare(string property, string value, Condition[] chain)
    {
        var style = property switch
        {
            "color" => new Style { Color = value },
            "margin" => new Style { Margin = value },
            "margin-top" => new Style { MarginTop = value },
            "outline" => new Style { Outline = value },
            _ => new Style { OutlineColor = value },
        };
        foreach (var condition in chain.Reverse()) style = new Style { [condition] = style };
        return style;
    }

    static IReadOnlyList<IReadOnlyList<Style>> RandomSplit(List<List<Style>> fragments, Random random)
    {
        var segments = new List<List<Style>> { new() };
        foreach (var fragment in fragments)
        {
            if (segments[^1].Count > 0 && random.NextDouble() < 0.5) segments.Add([]);
            segments[^1].AddRange(fragment);
        }
        return [.. segments.Where(s => s.Count > 0)];
    }

    static IEnumerable<HashSet<string>> Subsets(string[] items) =>
        Enumerable.Range(0, 1 << items.Length).Select(mask => items.Where((_, i) => (mask & (1 << i)) != 0).ToHashSet());

    static string Show(Dictionary<string, string> values) => string.Join(";", values.OrderBy(p => p.Key).Select(p => $"{p.Key}:{p.Value}"));
}
