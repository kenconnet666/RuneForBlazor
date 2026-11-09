using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Rune.Docs.Components.Base.Icon;
using Rune.UI;

namespace Rune.Docs.Site;

/// <summary>
/// 浏览器里（WebAssembly）的文档数据：下载服务端生成的静态文件（按类型一个，几 KB，各页共用、CDN 与浏览器都缓存）。
/// 不随页面带过去：预渲染的 HTML 里已经有源码与 API 表，再带一份（PersistentComponentState 转义加两层 base64）每页要多三十几 KB；
/// 接管后源码区是收着的、API 表在页面底部，下载期间看不出空白。下载过的留在内存里，同一份不重复请求，同时要同一份时共用一次请求。
/// </summary>
public sealed class BrowserDocsData : DocsData
{
    readonly HttpClient http;
    readonly ConcurrentDictionary<string, Task<string>> files = new();
    IReadOnlyList<IconEntry>? icons;

    /// <summary>以站点根作基地址。</summary>
    public BrowserDocsData(NavigationManager navigation) => http = new HttpClient { BaseAddress = new Uri(navigation.BaseUri) };

    /// <inheritdoc/>
    public override async ValueTask<string> SourceAsync(Type example) => await FileAsync(SourcePath(example));

    /// <inheritdoc/>
    public override async ValueTask<ComponentApi> ApiAsync(Type type) => ApiFromJson(await FileAsync(ApiPath(type)));

    /// <inheritdoc/>
    public override async ValueTask<IReadOnlyList<IconEntry>> IconsAsync() => icons ??= IconsFromJson(await FileAsync(IconsPath));

    Task<string> FileAsync(string path) => files.GetOrAdd(path, Download);

    async Task<string> Download(string path)
    {
        try
        {
            return await http.GetStringAsync(path);
        }
        catch
        {
            // 失败的不留着：下次用到时重新请求
            files.TryRemove(path, out _);
            throw;
        }
    }
}
