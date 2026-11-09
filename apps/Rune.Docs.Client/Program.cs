using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Rune.Docs.Site;
using Rune.UI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 与服务端（Rune.Docs/Program.cs）注册同一套服务：页面先在服务端预渲染，再在浏览器里运行
builder.Services.AddRuneUI(o =>
{
    o.ThemeChoices = ThemeChoices.Builtin;
    o.ThemeStylesheet = true;
});
builder.Services.AddDocsContent().AddBrowserDocsData();
// 图表、省市区的程序集按页面懒加载（Routes.razor）
builder.Services.AddScoped<LazyAssemblyLoader>();

await builder.Build().RunAsync();
