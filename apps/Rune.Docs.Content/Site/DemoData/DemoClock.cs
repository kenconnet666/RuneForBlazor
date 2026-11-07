namespace Rune.Docs.Site;

/// <summary>
/// 示例里的“现在”与“今天”：与组件读的是同一个时间来源（<see cref="DocsServices.AddDocsContent"/> 设好），
/// 对照与截图时固定在 FixedNow，平时是系统时钟（浏览器里是浏览器的时间）。示例的字段初始化器拿不到注入的服务，所以是静态的。
/// </summary>
public static class DemoClock
{
    internal static TimeProvider Source { get; set; } = TimeProvider.System;

    /// <summary>此刻（本地时区）。</summary>
    public static DateTimeOffset Now => Source.GetLocalNow();

    /// <summary>今天（本地时区）。</summary>
    public static DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
}
