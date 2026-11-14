using Rune.Styles;

namespace Rune.UI;

/// <summary>一种格式的视频源：浏览器从上到下取第一个能播的。</summary>
/// <param name="Src">地址。</param>
/// <param name="Type">格式：浏览器不认的跳过（<c>video/webm</c>、<c>video/mp4; codecs=…</c>）。</param>
/// <param name="Media">媒体条件：窄屏换一个小的文件（<c>(max-width: 40rem)</c>）。</param>
public sealed record VideoSource(string Src, string? Type = null, string? Media = null);

/// <summary>文本轨道的种类。</summary>
public enum VideoTrackKind
{
    /// <summary>对白的翻译（默认）。</summary>
    Subtitles,

    /// <summary>对白加音效说明（给听不到声音的人）。</summary>
    Captions,

    /// <summary>画面的文字说明。</summary>
    Descriptions,

    /// <summary>章节。</summary>
    Chapters,

    /// <summary>给脚本用的数据。</summary>
    Metadata,
}

/// <summary>一条文本轨道：字幕、说明、章节。</summary>
/// <param name="Src">WebVTT 文件的地址。</param>
/// <param name="SrcLang">语言（BCP 47：<c>zh</c>、<c>en</c>）。</param>
/// <param name="Label">菜单里显示的名字。</param>
/// <param name="Kind">种类，默认字幕。</param>
/// <param name="Default">默认打开这一条。</param>
public sealed record VideoTrack(string Src, string SrcLang, string Label, VideoTrackKind Kind = VideoTrackKind.Subtitles, bool Default = false);

/// <summary>视频的部件。</summary>
public enum VideoPart
{
    /// <summary>外层（AspectRatio 的 div；不定比例时没有）。</summary>
    Root,

    /// <summary>video 元素。</summary>
    Media,
}

/// <summary>视频的样式（同 vue-tsx-pro 的 videoStyles）。</summary>
public static class VideoStyles
{
    /// <summary>各部件的默认层；<paramref name="natural"/>：不定比例，按视频自己的尺寸。</summary>
    public static PartLayers<VideoPart> Of(bool natural, ComponentStyles styles) =>
        Recipe.Of("video", natural, styles, static (bool natural, VideoPart part) => part == VideoPart.Root ? Root : natural ? Natural : Media);

    static Style Root => Css.Define(static () => new Style { BorderRadius = Radii.Md, BackgroundColor = "black" });

    // 画面完整放进框里，不裁切（AspectRatio 默认给 video 的是 cover）
    static Style Media => Css.Define(static () => new Style
    {
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        ObjectFit = ObjectFit.Contain,
    });

    // 不定比例：按视频自己的尺寸，宽度不超过容器
    static Style Natural => Css.Define(static () => new Style
    {
        Display = Display.Block,
        MaxWidth = CssLength.Percent(100),
        Height = Height.Auto,
        BorderRadius = Radii.Md,
    });
}
