using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Rune.Demo.Client.Infrastructure;

/// <summary>演示可以选的渲染模式：地址里的名字 ↔ Blazor 的渲染模式。</summary>
public static class DemoRenderModes
{
    /// <summary>地址里的名字与说明，按显示顺序。</summary>
    public static readonly IReadOnlyList<(string Slug, string Title)> All =
    [
        ("static", "静态 SSR"),
        ("server", "Server"),
        ("wasm", "WebAssembly"),
        ("auto", "Auto"),
    ];

    /// <summary>默认模式：开发时最常用、改代码热重载最快。</summary>
    public const string Default = "server";

    /// <summary>名字对应的渲染模式；静态渲染是 null。</summary>
    public static IComponentRenderMode? Of(string slug) => slug switch
    {
        "server" => RenderMode.InteractiveServer,
        "wasm" => RenderMode.InteractiveWebAssembly,
        "auto" => RenderMode.InteractiveAuto,
        _ => null,
    };

    /// <summary>是不是认识的模式名。</summary>
    public static bool IsKnown(string? slug) => All.Any(m => m.Slug == slug);
}
