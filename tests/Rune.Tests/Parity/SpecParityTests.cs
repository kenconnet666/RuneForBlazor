using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.Parity;

/// <summary>
/// 与 vue-tsx-pro 的组件规格比对（vue-spec.json，由 tools/parity/spec.mjs 从 vue 文档站的 /spec/components.json 精简而来）：
/// vue 的每个组件、每个属性（普通属性、部件属性 propsXxx、事件 onXxx、内容 children）在 Rune 里有没有对应的组件与参数。
/// 名字按约定换算：_value → Value，propsTrigger → PropsTrigger，onClose → OnClose，children → ChildContent，renderDetail → DetailContent；
/// aria-* 是元素的属性，不比；改了名的组件（Link → TextLink 等）按 Renamed 找；把没声明的参数原样交给里面组件的（Forwards）连同里面组件的参数一起算。现有的差异记在 spec-baseline.txt（有意的不同、还没跟的都在里面），只对新出现的差异报错，
/// 也提醒把已经补上的从基线里删掉。跟进 vue 时重新取规格，补上的属性从基线里去掉；确实不跟的加进基线。
/// RUNE_SPEC_UPDATE=1 时按当前的差异重写基线。
/// </summary>
public class SpecParityTests
{
    static readonly Assembly[] Libraries = [typeof(Button).Assembly, typeof(DatePicker<>).Assembly, typeof(MapChart).Assembly];

    // 改了名的组件：vue 的名字 → Rune 的名字（Blazor 里 Link、Text 这类名字与常用类型冲突）
    static readonly Dictionary<string, string> Renamed = new(StringComparer.Ordinal)
    {
        ["Link"] = "TextLink",
        ["Text"] = "TextSpan",
        ["DateTime"] = "DateTimeText",
        ["Provider"] = "RuneProvider",
    };

    // 建在另一个组件上、没声明的参数（Rest）原样交给它的：Rune 的组件名 → 里面的组件名（可以一层套一层）
    static readonly Dictionary<string, string> Forwards = new(StringComparer.Ordinal)
    {
        ["TreeDataTable"] = "DataTable",
        ["PageSizeSelect"] = "Select",
        ["Popconfirm"] = "Popover",
        ["StatusBadge"] = "Badge",
        ["DeadlineBadge"] = "Badge",
        ["NavMenu"] = "NavList",
        ["ColumnTable"] = "Table",
        ["PropsTable"] = "Table",
        ["BackTop"] = "FloatButton",
        ["FloatButton"] = "IconButton",
    };

    [Fact]
    public void 与vue的组件规格比对_没有基线之外的差异()
    {
        var root = RepositoryRoot();
        var specPath = Path.Combine(root, "tests", "Rune.Tests", "Parity", "vue-spec.json");
        var baselinePath = Path.Combine(root, "tests", "Rune.Tests", "Parity", "spec-baseline.txt");
        using var spec = JsonDocument.Parse(File.ReadAllText(specPath));
        var differences = Differences(spec.RootElement.GetProperty("components")).Order(StringComparer.Ordinal).ToList();

        if (Environment.GetEnvironmentVariable("RUNE_SPEC_UPDATE") == "1")
        {
            File.WriteAllLines(baselinePath, differences);
            return;
        }
        var baseline = File.Exists(baselinePath) ? File.ReadAllLines(baselinePath).Where(line => line.Length > 0).ToHashSet(StringComparer.Ordinal) : [];
        var added = differences.Where(item => !baseline.Contains(item)).ToList();
        var fixedOnes = baseline.Where(item => !differences.Contains(item)).Order(StringComparer.Ordinal).ToList();
        Assert.True(added.Count == 0, $"Rune 缺少 vue-tsx-pro {spec.RootElement.GetProperty("version")} 的这些组件 / 参数（补上，或确实不跟时加进 spec-baseline.txt）：\n{string.Join("\n", added)}");
        Assert.True(fixedOnes.Count == 0, $"这些已经对上了，从 spec-baseline.txt 里删掉：\n{string.Join("\n", fixedOnes)}");
    }

    static IEnumerable<string> Differences(JsonElement components)
    {
        var types = Libraries.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => typeof(IComponent).IsAssignableFrom(type) && !type.IsAbstract)
            .GroupBy(type => type.Name.Split('`')[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var component in components.EnumerateObject())
        {
            if (!types.TryGetValue(Renamed.GetValueOrDefault(component.Name, component.Name), out var type))
            {
                yield return $"{component.Name}（组件）";
                continue;
            }
            var parameters = ParametersOf(type);
            for (var name = type.Name.Split('`')[0]; Forwards.TryGetValue(name, out var inner); name = inner) parameters.UnionWith(ParametersOf(types[inner]));
            foreach (var prop in component.Value.EnumerateArray())
            {
                var name = prop.GetProperty("name").GetString()!;
                if (Expected(name, prop.GetProperty("kind").GetString()!) is { } expected && !parameters.Contains(expected))
                    yield return $"{component.Name}.{expected}";
            }
        }
    }

    static HashSet<string> ParametersOf(Type type) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.GetCustomAttribute<ParameterAttribute>() is not null)
        .Select(property => property.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // vue 的属性名 → Rune 的参数名；元素属性（aria-*）不比
    static string? Expected(string name, string kind)
    {
        if (name.Contains('-') || name.Contains(':')) return null;
        if (kind == "content")
            return name == "children" ? "ChildContent" : name.StartsWith("render", StringComparison.Ordinal) ? $"{name[6..]}Content" : Pascal(name);
        return Pascal(name.TrimStart('_'));
    }

    static string Pascal(string name) => name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];

    static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "RuneForBlazor.slnx"))) return directory.FullName;
        throw new InvalidOperationException($"从 {AppContext.BaseDirectory} 往上找不到 RuneForBlazor.slnx");
    }
}
