using Rune.Docs.Components;
using Rune.Docs.Site;
using Rune.UI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// 预渲染用这里的服务；浏览器里用 Rune.Docs.Client/Program.cs 里的同一套
builder.Services.AddRuneUI(o => o.ThemeChoices = ThemeChoices.Builtin);
builder.Services.AddDocsContent();

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
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Rune.Docs.Client._Imports).Assembly, typeof(DocsCatalog).Assembly);

app.Run();
