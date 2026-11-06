using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>图片预览（Lightbox）里的一张图。</summary>
/// <param name="Src">大图的地址。</param>
/// <param name="Alt">图片的说明（读屏念它）。</param>
public sealed record LightboxItem(string Src, string Alt)
{
    /// <summary>图片下方的说明。</summary>
    public RenderFragment? Caption { get; init; }

    /// <summary>显示下载按钮，默认是。</summary>
    public bool Downloadable { get; init; } = true;

    /// <summary>下载时的文件名；不写时用地址里的名字。</summary>
    public string? DownloadName { get; init; }
}
