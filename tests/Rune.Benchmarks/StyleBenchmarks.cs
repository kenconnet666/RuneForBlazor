using BenchmarkDotNet.Attributes;
using Rune.Styles;
using Rune.UI;

namespace Rune.Benchmarks;

/// <summary>
/// 每个元素每次渲染都走的样式路径：样式定义的查找（静态、按键、配方）、属性合并（只有样式、部件层、三层都有、交给子组件）。
/// 合并要登记样式规则：基准在一个登记表的作用域里跑（同组件渲染时）。
/// </summary>
[MemoryDiagnoser]
public class StyleBenchmarks
{
    StyleRegistry registry = default!;
    StyleRegistry.Scope scope;
    Attrs classOnly = default!;
    Attrs part = default!;
    Attrs rest = default!;
    Attrs managed = default!;

    static Style Plain => Css.Define(static () => new Style { Display = Display.Flex, Gap = Space.Sm });

    static Style Extra => Css.Define(static () => new Style { Color = Colors.TextMuted });

    [GlobalSetup]
    public void Setup()
    {
        registry = new StyleRegistry();
        scope = StyleRegistry.Enter(registry);
        classOnly = new Attrs { Sx = Plain };
        part = IconStyles.Of(Size.Md, null, false, ComponentStyles.Empty)[IconPart.Root];
        rest = Attrs.From(new Dictionary<string, object> { ["id"] = "icon-1" }, Extra);
        managed = new Attrs { ["aria-hidden"] = "true", ["focusable"] = "false" };
    }

    [GlobalCleanup]
    public void Cleanup() => scope.Dispose();

    [Benchmark(Description = "Css.Define 静态")]
    public Style DefineStatic() => Plain;

    [Benchmark(Description = "Css.Define 按枚举键")]
    public Style DefineKeyed() => ScaleStyles.Radius(Radius.Md);

    [Benchmark(Description = "Recipe.Of（图标）")]
    public PartLayers<IconPart> RecipeOf() => IconStyles.Of(Size.Md, null, false, ComponentStyles.Empty);

    [Benchmark(Description = "Merge 只有样式")]
    public Attrs MergeClassOnly() => Attrs.Merge(classOnly);

    [Benchmark(Description = "Merge 部件层")]
    public Attrs MergePart() => Attrs.Merge(part);

    [Benchmark(Description = "Merge 部件层 + Rest + 受管")]
    public Attrs MergeThreeLayers() => Attrs.Merge(part, rest, managed);

    [Benchmark(Description = "MergeProps 部件层 + Rest")]
    public Attrs MergeProps() => Attrs.MergeProps(part, rest);
}
