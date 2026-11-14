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

    /// <summary>自绘的控制条（Controls="VideoControls.Custom"）。</summary>
    Controls,

    /// <summary>控制条里的按钮（播放、静音、字幕、倍速、全屏；IconButton）。</summary>
    Button,

    /// <summary>当前时间与时长。</summary>
    Time,

    /// <summary>进度滑块的外框。</summary>
    Seek,

    /// <summary>音量滑块的外框。</summary>
    Volume,
}

/// <summary>视频的控制条。</summary>
public enum VideoControls
{
    /// <summary>浏览器自己的控制条（默认）。</summary>
    Native,

    /// <summary>
    /// 自绘的控制条：跟主题，带倍速与字幕菜单、全屏，播放中自动收起。要有外框（Ratio 不为 null）、要交互渲染：
    /// 不定比例或静态渲染（含交互接管前的预渲染）时用浏览器自己的。
    /// </summary>
    Custom,

    /// <summary>不要控制条（自动播放的装饰视频；减少动态效果时仍会显示浏览器的）。</summary>
    None,
}

/// <summary>视频的样式（同 vue-tsx-pro 的 videoStyles）。</summary>
public static class VideoStyles
{
    /// <summary>各部件的默认层；<paramref name="natural"/>：不定比例，按视频自己的尺寸。</summary>
    public static PartLayers<VideoPart> Of(bool natural, ComponentStyles styles) =>
        Recipe.Of("video", natural, styles, static (bool natural, VideoPart part) => part switch
        {
            VideoPart.Root => Root,
            VideoPart.Media => natural ? Natural : Media,
            VideoPart.Controls => Controls,
            VideoPart.Button => Button,
            VideoPart.Time => Time,
            VideoPart.Seek => Seek,
            _ => Volume,
        });

    /// <summary>自绘控制条的外框：点画面后焦点在它上面（不画焦点环）；控制条收起时连鼠标指针一起藏起来（JS 写的 data-idle）。</summary>
    internal static Style Custom => Css.Define(static () => new Style
    {
        OutlineStyle = OutlineStyle.None,
        [When.Attribute("[data-idle]")] = new() { Cursor = Cursor.None },
    });

    /// <summary>深色画面上的滑块：轨道是半透明的白，走过的一段是白。</summary>
    internal static Style SliderTrack => Css.Define(static () => new Style { BackgroundColor = $"color-mix(in srgb, {Colors.OnScrim} 30%, transparent)" });

    /// <inheritdoc cref="SliderTrack"/>
    internal static Style SliderRange => Css.Define(static () => new Style { BackgroundColor = Colors.OnScrim });

    // 控制条：贴在底边，下面压一层由透明到深色的渐变，白字在任何画面上都读得清。
    // 收起（JS 写的 data-state=hidden）：看不见、点不到；键盘聚焦到里面、菜单开着时 JS 不收起
    static Style Controls => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetInline = 0,
        Top = CssLength.Auto,
        Bottom = 0,
        Height = Height.Auto,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        Color = Colors.OnScrim,
        BackgroundImage = $"linear-gradient(to top, {Colors.ScrimStrong}, transparent)",
        Transition = $"opacity {Durations.Md} {Easings.Standard}",
        [When.Attribute("[data-state=hidden]")] = new() { Opacity = 0, PointerEvents = PointerEvents.None },
    });

    // 深色画面上的白色图标，悬停是一层浅白（同 Lightbox 的工具栏）
    static Style Button => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        Color = Colors.OnScrim,
        BackgroundColor = CssColors.Transparent,
        [When.Hover] = new() { BackgroundColor = $"color-mix(in srgb, {Colors.OnScrim} 16%, transparent)" },
    });

    static Style Time => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        FontSize = FontSizes.Xs,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        WhiteSpace = WhiteSpace.Nowrap,
    });

    static Style Seek => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = CssLength.Em(4) });

    // 音量滑块只给鼠标：手机上用机身的音量键（iOS 也不让网页改音量）
    static Style Volume => Css.Define(static () => new Style
    {
        Width = CssLength.Em(4.5),
        FlexShrink = 0,
        [Rune.Styles.Media.CoarsePointer] = new() { Display = Display.None },
    });

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
