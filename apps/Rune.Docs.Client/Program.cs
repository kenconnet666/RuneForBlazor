using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rune.Docs.Site;
using Rune.UI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 与服务端（Rune.Docs/Program.cs）注册同一套服务：页面先在服务端预渲染，再在浏览器里运行
builder.Services.AddRuneUI(o => o.ThemeChoices = ThemeChoices.Builtin);
builder.Services.AddDocsContent();

await builder.Build().RunAsync();
