namespace Rune.Docs.Site;

/// <summary>演示页共用的文字格式（_Imports 里 using static 引入）。</summary>
public static class DemoText
{
    /// <summary>枚举值的小写名：说明文字里对照 vue-tsx-pro 的写法（<c>variant="soft"</c>）。</summary>
    public static string Lower<T>(T value) where T : struct, Enum => value.ToString().ToLowerInvariant();
}
