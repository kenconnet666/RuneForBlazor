using Rune.UI;
using Rune.Docs.Site;
using Rune.Demo.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// 服务端渲染与 Interactive Server 用这里的服务；WebAssembly 用 Rune.Demo.Client/Program.cs 里的同一套
builder.Services.AddRuneUI(o => o.ThemeChoices = ThemeChoices.Builtin);
builder.Services.AddDocsContent();
// 文档内容注册了授权（Can 页的 AuthorizeView），Web 应用会自动加上授权中间件，它要服务端完整的授权服务
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Rune.Demo.Client._Imports).Assembly, typeof(DocsCatalog).Assembly);

app.Run();
