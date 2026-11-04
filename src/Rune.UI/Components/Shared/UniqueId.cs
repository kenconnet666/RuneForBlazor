namespace Rune.UI;

/// <summary>组件内部元素的 id（aria-controls、aria-labelledby 互相引用用）。</summary>
internal static class UniqueId
{
    static long counter;

    /// <summary>形如 <c>collapsible-trigger-12</c>；进程内唯一。</summary>
    public static string Next(string prefix) => $"{prefix}-{Interlocked.Increment(ref counter)}";
}
