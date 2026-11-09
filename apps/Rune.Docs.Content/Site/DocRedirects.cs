namespace Rune.Docs.Site;

/// <summary>
/// 合并、挪走了的文档页（同 vue-tsx-pro 文档站的 redirects）：旧地址 → 新页的对应小节，站内外的旧链接、收藏不失效。
/// 服务端按它回 301（Program.cs）；静态导出把它写成 Cloudflare Pages 的 _redirects（tools/docs-export 取 /_redirects）。
/// </summary>
public static class DocRedirects
{
    /// <summary>旧地址 → 新地址（带小节锚点）。</summary>
    public static IReadOnlyDictionary<string, string> Map { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["/components/icon-button"] = "/components/button#icon-buttons",
        ["/components/banner"] = "/components/alert#banner",
        ["/components/loading-bar"] = "/components/progress#loading-bar",
        ["/components/value-change"] = "/components/field-compare#value-change",
        ["/components/resize-handle"] = "/components/splitter#resize-handle",
        ["/components/form-section"] = "/components/form#sections",
        ["/components/list"] = "/components/text#lists",
        ["/components/can"] = "/components/provider#permissions",
        ["/components/nav-menu"] = "/components/nav-list#menu",
        ["/components/nav-bar"] = "/components/nav-list#nav-bar",
        ["/components/viewport-fill"] = "/components/aside-layout#viewport-fill",
        ["/components/gauge"] = "/components/sparkline#gauge",
        ["/components/proportion-bar"] = "/components/sparkline#proportion-bar",
        ["/components/doc-article"] = "/components/docs-kit#article",
        ["/components/props-table"] = "/components/docs-kit#props-table",
    };

    /// <summary>Cloudflare Pages 的 _redirects 文件：一行一条，永久跳转。</summary>
    public static string CloudflareRules => string.Concat(Map.Select(static pair => $"{pair.Key} {pair.Value} 301\n"));
}
