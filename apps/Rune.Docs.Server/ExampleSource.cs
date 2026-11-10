using System.Collections.Concurrent;

namespace Rune.Docs.Server;

/// <summary>
/// 示例的源码：内容项目的示例文件嵌在这个程序集里（见项目文件），按示例组件的类型取出原文，与运行的效果永远一致。
/// 命名空间跟着文件夹：类型 <c>Rune.Docs.Components.Base.Button.Demo.BasicExample</c> 对应资源 <c>examples/Components/Base/Button/Demo/BasicExample.razor</c>，
/// 指南的示例 <c>Rune.Docs.Guide.Demo.…</c> 对应 <c>examples/Guide/Demo/…</c>。
/// </summary>
public static class ExampleSource
{
    const string ContentNamespace = "Rune.Docs.";
    static readonly ConcurrentDictionary<Type, string> cache = new();

    /// <summary>
    /// 示例组件的源码。旁边有同名的 JS 模块（<c>XxxExample.razor.js</c>，ThreeCanvas 的场景、ModelViewer 的对象）时接在后面：
    /// 包在 script 里只是为了按 JS 高亮，它是单独的文件，示例按地址引用它（_content/Rune.Docs.Content/…）。
    /// </summary>
    public static string Of(Type example) => cache.GetOrAdd(example, static type =>
    {
        var ns = type.Namespace ?? "";
        if (!ns.StartsWith(ContentNamespace, StringComparison.Ordinal))
            throw new ArgumentException($"{type.FullName} 不在 {ContentNamespace} 下，取不到源码", nameof(example));
        var path = $"examples/{ns[ContentNamespace.Length..].Replace('.', '/')}/{type.Name}.razor";
        var source = Read(path) ?? throw new InvalidOperationException($"找不到示例的源码 {path}");
        if (Read(path + ".js") is { } module)
            source += $"\n@* {type.Name}.razor.js：示例用到的 JS 模块（单独的文件，这里一起显示） *@\n<script type=\"module\">\n{module}</script>\n";
        return source;
    });

    static string? Read(string path)
    {
        var assembly = typeof(ExampleSource).Assembly;
        // 资源名里的路径分隔符随构建的系统（Windows 上是反斜杠）
        var name = assembly.GetManifestResourceNames().FirstOrDefault(n => string.Equals(n.Replace('\\', '/'), path, StringComparison.Ordinal));
        if (name is null) return null;
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().ReplaceLineEndings("\n").TrimEnd() + "\n";
    }
}
