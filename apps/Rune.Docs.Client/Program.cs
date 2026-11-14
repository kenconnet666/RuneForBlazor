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
// 打开的第一页只把视口附近的小节换成交互的，其余的滚到附近才渲染（留着预渲染的内容）：重的页面不用在后台再算上好几秒
builder.Services.AddSingleton(new RenderQueueOptions(NearViewportOnly: true));
// 图表、省市区的程序集按页面懒加载（Routes.razor）
builder.Services.AddScoped<LazyAssemblyLoader>();

await builder.Build().RunAsync();
