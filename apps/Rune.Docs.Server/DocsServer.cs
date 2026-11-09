using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Rune.Docs.Site;

namespace Rune.Docs.Server;

/// <summary>文档数据的服务端：注册 <see cref="DocsData"/> 的服务端实现，提供 /_docs/… 的地址（浏览器换页时下载，静态导出时写出）。</summary>
public static class DocsServer
{
    /// <summary>注册服务端的文档数据（预渲染与 Interactive Server 用）。</summary>
    public static IServiceCollection AddDocsServer(this IServiceCollection services) =>
        services.AddSingleton<DocsFiles>().AddScoped<DocsData, ServerDocsData>();

    /// <summary>
    /// /_docs/source/{类型}.txt、/_docs/api/{类型}.json、/_docs/icons.json：浏览器端的 DocsData 按需下载；
    /// /_docs/files：预渲染用到过的这些地址，静态导出爬完页面后按它把文件写出来。
    /// </summary>
    public static IEndpointRouteBuilder MapDocsData(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/_docs/files", (DocsFiles files) => string.Join('\n', files.All));
        endpoints.MapGet("/_docs/{**path}", (string path) =>
        {
            var full = $"_docs/{path}";
            if (ServerDocsData.Render(full) is not { } content) return Results.NotFound();
            var type = full.EndsWith(".json", StringComparison.Ordinal) ? "application/json; charset=utf-8" : "text/plain; charset=utf-8";
            return Results.Text(content, type);
        });
        return endpoints;
    }
}
