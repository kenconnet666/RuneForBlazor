using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rune;
using Rune.Demo.Client.Demos;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 与服务端（Rune.Demo/Program.cs）注册同一套服务：同一个页面可能在任一边运行
builder.Services.AddRune();
builder.Services.AddScoped<CounterStore>();

await builder.Build().RunAsync();
