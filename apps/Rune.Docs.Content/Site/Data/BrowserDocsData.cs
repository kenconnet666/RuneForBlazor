using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Rune.Docs.Components.Base.Icon;
using Rune.UI;

namespace Rune.Docs.Site;

/// <summary>
/// 浏览器里（WebAssembly）的文档数据：先用预渲染带过来的（第一屏不用再请求、不闪），没有的再下载服务端生成的静态文件。
/// 下载过的留在内存里，同一份不重复请求；同时要同一份时共用一次请求。
/// </summary>
public sealed class BrowserDocsData : DocsData
{
    readonly HttpClient http;
    readonly ConcurrentDictionary<string, Task<string>> files = new();
    IReadOnlyList<IconEntry>? icons;

    /// <summary>站点根作基地址；预渲染带过来的数据此时就取走（只能在第一次渲染完之前取）。</summary>
    public BrowserDocsData(NavigationManager navigation, PersistentComponentState state)
    {
        http = new HttpClient { BaseAddress = new Uri(navigation.BaseUri) };
        if (state.TryTakeFromJson<Dictionary<string, string>>(StateKey, out var carried) && carried is not null)
            foreach (var (path, content) in carried) files[path] = Task.FromResult(content);
    }

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
