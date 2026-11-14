using System.Text.RegularExpressions;

namespace Rune.Tests.UI;

/// <summary>
/// Rune.UI 的 JS 主体（runtime.js）在 document 上挂的每种事件，入口（RuneForBlazor.UI.lib.module.js 的 SLOTS）都要先占好位置：
/// 没占位的在浏览器里一挂就报错，整套 JS 装不上（所有交互失效），而单元测试不跑 JS、看不出来。
/// </summary>
public partial class EventSlotTests
{
    [Fact]
    public void 主体挂的事件入口都占了位置()
    {
        var root = RepositoryRoot();
        var scripts = Path.Combine(root, "src", "Rune.UI", "wwwroot");
        var entry = File.ReadAllText(Path.Combine(scripts, "RuneForBlazor.UI.lib.module.js"));
        var runtime = File.ReadAllText(Path.Combine(scripts, "js", "core", "runtime.js"));

        var table = entry[entry.IndexOf("const SLOTS = [", StringComparison.Ordinal)..];
        var slots = Slot().Matches(table[..table.IndexOf("];", StringComparison.Ordinal)])
            .Select(match => (Type: match.Groups[1].Value, Capture: match.Groups[2].Value == "true"))
            .ToHashSet();
        Assert.NotEmpty(slots);

        // on('事件', 处理函数) 是冒泡，on('事件', 处理函数, true) 是捕获。处理函数写成一行的看行尾；写成几行的箭头函数看收尾的那一行（同样的缩进）
        var hooks = OneLine().Matches(runtime).Concat(Block().Matches(runtime))
            .Select(match => (Type: match.Groups["type"].Value, Capture: match.Groups["capture"].Success))
            .Distinct()
            .ToList();
        Assert.True(hooks.Count > 20, $"只认出 {hooks.Count} 处，runtime.js 里 on(…) 的写法变了？");
        var missing = hooks.Where(hook => !slots.Contains(hook)).Select(hook => $"{hook.Type}（{(hook.Capture ? "捕获" : "冒泡")}）").ToList();
        Assert.True(missing.Count == 0, $"runtime.js 挂了这些事件，入口的 SLOTS 里没有：{string.Join("、", missing)}");
    }

    static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "RuneForBlazor.slnx"))) return directory.FullName;
        throw new InvalidOperationException($"从 {AppContext.BaseDirectory} 往上找不到 RuneForBlazor.slnx");
    }

    [GeneratedRegex(@"\['([a-z]+)',\s*(true|false)\]")]
    private static partial Regex Slot();

    [GeneratedRegex(@"^ *on\('(?<type>[a-z]+)', [^\n{]*?(?<capture>, true)?\);\s*?$", RegexOptions.Multiline)]
    private static partial Regex OneLine();

    [GeneratedRegex(@"^(?<indent> *)on\('(?<type>[a-z]+)', [^\n]*\{\s*?\n.*?^\k<indent>\}(?<capture>, true)?\);", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex Block();
}
