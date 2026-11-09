using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rune.UI;
using Rune.Docs.Site;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 与服务端（Rune.Demo/Program.cs）注册同一套服务：同一个页面可能在任一边运行
builder.Services.AddRuneUI(o => o.ThemeChoices = ThemeChoices.Builtin);
builder.Services.AddDocsContent().AddBrowserDocsData();

await builder.Build().RunAsync();
