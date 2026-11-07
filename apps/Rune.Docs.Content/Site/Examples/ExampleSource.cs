using System.Collections.Concurrent;

namespace Rune.Docs.Site;

/// <summary>
/// 示例的源码：示例文件嵌在内容程序集里（见项目文件），按示例组件的类型取出原文，与运行的效果永远一致。
/// 命名空间跟着文件夹：类型 <c>Rune.Docs.Components.Base.Button.Demo.BasicExample</c> 对应资源 <c>examples/Components/Base/Button/Demo/BasicExample.razor</c>，
/// 指南的示例 <c>Rune.Docs.Guide.Demo.…</c> 对应 <c>examples/Guide/Demo/…</c>。
/// </summary>
public static class ExampleSource
{
    const string ContentNamespace = "Rune.Docs.";
    static readonly ConcurrentDictionary<Type, string> cache = new();

    /// <summary>示例组件的源码。</summary>
    public static string Of(Type example) => cache.GetOrAdd(example, static type =>
    {
        var ns = type.Namespace ?? "";
        if (!ns.StartsWith(ContentNamespace, StringComparison.Ordinal))
            throw new ArgumentException($"{type.FullName} 不在 {ContentNamespace} 下，取不到源码", nameof(example));
        var path = $"examples/{ns[ContentNamespace.Length..].Replace('.', '/')}/{type.Name}.razor";
        var assembly = type.Assembly;
        // 资源名里的路径分隔符随构建的系统（Windows 上是反斜杠）
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => string.Equals(n.Replace('\\', '/'), path, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"找不到示例的源码 {path}");
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().ReplaceLineEndings("\n").TrimEnd() + "\n";
    });
}
