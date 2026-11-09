using System.Text.RegularExpressions;

namespace Rune.Tests.UI;

/// <summary>
/// 部件名一律小驼峰（同 vue-tsx-pro 的结构检查，7437db27）：Part.Of / Recipe.Of 按枚举名生成的已经是，
/// 这里查源码里手写的 data-part（图表、JS 里画的节点），免得写成 center-label 这样的连字符。
/// </summary>
public partial class PartNameTests
{
    [Fact]
    public void 手写的部件名是小驼峰()
    {
        var root = RepositoryRoot();
        var wrong = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".razor") || file.EndsWith(".cs") || file.EndsWith(".js"))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(file => LiteralPart().Matches(File.ReadAllText(file)).Select(match => (File: Path.GetRelativePath(root, file), Name: match.Groups[1].Value)))
            .Where(part => !CamelCase().IsMatch(part.Name))
            .Select(part => $"{part.File}: {part.Name}")
            .ToList();
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "RuneForBlazor.slnx"))) return directory.FullName;
        throw new InvalidOperationException($"从 {AppContext.BaseDirectory} 往上找不到 RuneForBlazor.slnx");
    }

    // data-part="x"、['data-part'] = "x"、'data-part', 'x'
    [GeneratedRegex("""data-part["']?\]?\s*[=,:]\s*["']([^"'{}]+)["']""")]
    private static partial Regex LiteralPart();

    [GeneratedRegex("^[a-z][a-zA-Z0-9]*$")]
    private static partial Regex CamelCase();
}
