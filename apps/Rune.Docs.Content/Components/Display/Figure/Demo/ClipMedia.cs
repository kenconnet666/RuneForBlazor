using Rune.UI;

namespace Rune.Docs.Components.Display.Figure.Demo;

/// <summary>视频示例用的片段（wwwroot/media，同 vue-tsx-pro 文档站的 public/media）：十秒的小球动画，两种格式、封面、中英字幕。</summary>
static class ClipMedia
{
    const string Folder = "_content/Rune.Docs.Content/media/";

    public static readonly IReadOnlyList<VideoSource> Sources = [new(Folder + "clip.webm", "video/webm"), new(Folder + "clip.mp4", "video/mp4")];

    public static readonly IReadOnlyList<VideoTrack> Tracks = [new(Folder + "clip.zh.vtt", "zh", "中文"), new(Folder + "clip.en.vtt", "en", "English")];

    public const string Poster = Folder + "clip.jpg";
}
