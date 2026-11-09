using Rune.Docs.Components;
using Rune.Docs.Server;
using Rune.Docs.Site;
using Rune.UI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// 预渲染用这里的服务；浏览器里用 Rune.Docs.Client/Program.cs 里的同一套
builder.Services.AddRuneUI(o => o.ThemeChoices = ThemeChoices.Builtin);
builder.Services.AddDocsContent(builder.Configuration);
// 示例的源码、API 表、图标库：服务端生成，预渲染时带给浏览器，浏览器换页时下载 /_docs/…
builder.Services.AddDocsServer();
// 文档内容注册了授权（Provider 页“权限”一节的 AuthorizeView），Web 应用会自动加上授权中间件，它要服务端完整的授权服务
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/not-found", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
// 合并、挪走了的文档页：旧地址永久跳到新页的小节；静态导出取 /_redirects 写成 Cloudflare Pages 的规则
foreach (var (from, to) in DocRedirects.Map)
    app.MapGet(from, () => Results.Redirect(to, permanent: true));
app.MapGet("/_redirects", () => DocRedirects.CloudflareRules);
app.MapDocsData();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Rune.Docs.Client._Imports).Assembly, typeof(DocsCatalog).Assembly);

app.Run();
