using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>嵌入的页面、视频、图片的备选源、量表（同 vue-tsx-pro 0.26.0 的 Embed、Video、Image 的 sources、Progress 的 bands）。</summary>
public class MediaTests : BunitContext
{
    public MediaTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 嵌入的页面_地址交给JS_加载完之前是骨架_默认延迟加载()
    {
        var cut = Render<Embed>(p => p.Add(x => x.Src, "https://example.com/embed/42").Add(x => x.Title, "产品演示").AddUnmatched("allow", "fullscreen"));

        var root = cut.Find("""[data-scope="embed"][data-part="root"]""");
        var frame = cut.Find("iframe");
        Assert.Equal("loading", root.GetAttribute("data-state"));
        Assert.Equal("产品演示", frame.GetAttribute("title"));
        // 地址由 JS 在接上 load 之后才给（预渲染出来的 iframe 可能在脚本接管之前就加载完了）
        Assert.False(frame.HasAttribute("src"));
        Assert.Equal("https://example.com/embed/42", frame.GetAttribute("data-rune-embed"));
        Assert.Equal(("lazy", "strict-origin-when-cross-origin", "fullscreen"),
            (frame.GetAttribute("loading"), frame.GetAttribute("referrerpolicy"), frame.GetAttribute("allow")));
        Assert.NotNull(cut.Find("""[data-scope="embed"][data-part="placeholder"]"""));
    }

    [Fact]
    public void 嵌入的页面_默认值可以改_换了地址重建iframe()
    {
        var cut = Render<Embed>(p => p.Add(x => x.Src, "https://example.com/a").Add(x => x.Title, "报表").AddUnmatched("loading", "eager"));
        var before = cut.Find("iframe");
        Assert.Equal("eager", before.GetAttribute("loading"));

        cut.Render(p => p.Add(x => x.Src, "https://example.com/b"));
        Assert.Equal("https://example.com/b", cut.Find("iframe").GetAttribute("data-rune-embed"));
    }

    [Fact]
    public void 视频_默认有控制条_只预加载元数据_几种格式与字幕()
    {
        var cut = Render<Video>(p => p
            .Add(x => x.Src, "/ignored.mp4")
            .Add(x => x.Sources, [new VideoSource("/clip.webm", "video/webm"), new VideoSource("/clip.mp4", "video/mp4", "(max-width: 40rem)")])
            .Add(x => x.Tracks, [new VideoTrack("/zh.vtt", "zh", "中文", Default: true)])
            .Add(x => x.Poster, "/cover.jpg")
            .AddUnmatched("aria-label", "安装步骤"));

        var video = cut.Find("video");
        Assert.True(video.HasAttribute("controls"));
        Assert.Equal(("metadata", "/cover.jpg", "安装步骤"), (video.GetAttribute("preload"), video.GetAttribute("poster"), video.GetAttribute("aria-label")));
        // 写了 Sources 就不用 Src
        Assert.False(video.HasAttribute("src"));
        Assert.Equal(["/clip.webm", "/clip.mp4"], cut.FindAll("video > source").Select(source => source.GetAttribute("src")));
        Assert.Equal("(max-width: 40rem)", cut.FindAll("video > source")[1].GetAttribute("media"));
        var track = cut.Find("video > track");
        Assert.Equal(("subtitles", "zh", "中文"), (track.GetAttribute("kind"), track.GetAttribute("srclang"), track.GetAttribute("label")));
        Assert.True(track.HasAttribute("default"));
        // 定比例：外层是 AspectRatio
        Assert.NotNull(cut.Find("""[data-scope="video"][data-part="root"] > video"""));
    }

    [Fact]
    public void 视频_自动播放交给JS_默认静音_不定比例时没有外层()
    {
        var cut = Render<Video>(p => p.Add(x => x.Src, "/loop.mp4").Add(x => x.Autoplay, true).Add(x => x.Controls, VideoControls.None).Add(x => x.Ratio, null));

        var video = cut.Find("video");
        // 标记里不写 autoplay：减少动态效果时不该自动播放，由 JS 判断后再播
        Assert.False(video.HasAttribute("autoplay"));
        Assert.True(video.HasAttribute("data-rune-autoplay"));
        Assert.True(video.HasAttribute("muted"));
        Assert.True(video.HasAttribute("playsinline"));
        Assert.False(video.HasAttribute("controls"));
        Assert.Equal("/loop.mp4", video.GetAttribute("src"));
        Assert.Empty(cut.FindAll("""[data-scope="video"][data-part="root"]"""));
    }

    [Fact]
    public void 视频的自绘控制条_JS发来的状态决定按钮_时间与滑块()
    {
        var cut = Render<Video>(p => p.Add(x => x.Src, "/demo.mp4").Add(x => x.Poster, "/cover.jpg").Add(x => x.Controls, VideoControls.Custom)
            .Add(x => x.Tracks, [new VideoTrack("/zh.vtt", "zh", "中文"), new VideoTrack("/en.vtt", "en", "English")])
            .Add(x => x.Speeds, [1, 2]));

        var root = cut.Find("[data-rune-video]");
        var video = cut.Find("video");
        // 自绘的控制条在：video 上不要浏览器的；地址、封面照常
        Assert.False(video.HasAttribute("controls"));
        Assert.Equal(("/demo.mp4", "/cover.jpg"), (video.GetAttribute("src"), video.GetAttribute("poster")));
        Assert.Equal(("-1", "paused"), (root.GetAttribute("tabindex"), root.GetAttribute("data-state")));
        Assert.Equal("播放", cut.Find("[data-rune-video-action=play]").GetAttribute("aria-label"));
        // 还不知道时长：只显示已经播了多久
        Assert.Equal("0:00", cut.Find("""[data-part="time"]""").TextContent);
        // 没有时长之前进度不能拖
        Assert.Equal("true", cut.Find("[data-rune-video-seek] [role=slider]").GetAttribute("aria-disabled"));
        Assert.Equal(["关闭", "中文", "English"], cut.FindAll("[data-rune-video-caption]").Select(item => item.TextContent.Trim()));
        Assert.Equal(["1", "2"], cut.FindAll("[data-rune-video-speed]").Select(item => item.GetAttribute("data-rune-video-speed")));

        // playing current duration volume muted rate caption fullscreen supported
        root.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "video-state", Text = "1 65.4 200.0 0.50 1 2 1 0 1" });

        root = cut.Find("[data-rune-video]");
        Assert.Equal("playing", root.GetAttribute("data-state"));
        Assert.Equal("暂停", cut.Find("[data-rune-video-action=play]").GetAttribute("aria-label"));
        Assert.Equal("1:05 / 3:20", cut.Find("""[data-part="time"]""").TextContent);
        var seek = cut.Find("[data-rune-video-seek] [role=slider]");
        Assert.Equal(("65.4", "200", "1:05，共 3:20"), (seek.GetAttribute("aria-valuenow"), seek.GetAttribute("aria-valuemax"), seek.GetAttribute("aria-valuetext")));
        var mute = cut.Find("[data-rune-video-action=mute]");
        Assert.Equal(("取消静音", "true"), (mute.GetAttribute("aria-label"), mute.GetAttribute("aria-pressed")));
        // 静音时音量滑块在 0
        Assert.Equal("0", cut.Find("[data-rune-video-volume] [role=slider]").GetAttribute("aria-valuenow"));
        Assert.Equal("true", cut.Find("[data-rune-video-caption='1']").GetAttribute("aria-checked"));
        Assert.Equal("true", cut.Find("[data-rune-video-speed='2']").GetAttribute("aria-checked"));
        Assert.Equal("全屏", cut.Find("[data-rune-video-action=fullscreen]").GetAttribute("aria-label"));

        // 浏览器不支持全屏：没有全屏按钮
        root.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "video-state", Text = "0 0.0 200.0 1.00 0 1 -1 0 0" });
        Assert.Empty(cut.FindAll("[data-rune-video-action=fullscreen]"));

        // 直播（没有时长、也没有能跳的范围，JS 报 0）：时间只显示播了多久，进度条不可用
        cut.Find("[data-rune-video]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "video-state", Text = "1 65.0 0.0 1.00 0 1 -1 0 1" });
        Assert.Equal("1:05", cut.Find("""[data-part="time"]""").TextContent);
        Assert.Equal("true", cut.Find("[data-rune-video-seek] [role=slider]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void 视频的自绘控制条_静态渲染或不定比例时用浏览器的()
    {
        var unbounded = Render<Video>(p => p.Add(x => x.Src, "/demo.mp4").Add(x => x.Controls, VideoControls.Custom).Add(x => x.Ratio, null));
        Assert.True(unbounded.Find("video").HasAttribute("controls"));
        Assert.Empty(unbounded.FindAll("[data-rune-video]"));

        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var prerendered = Render<Video>(p => p.Add(x => x.Src, "/demo.mp4").Add(x => x.Controls, VideoControls.Custom));
        Assert.True(prerendered.Find("video").HasAttribute("controls"));
        Assert.Empty(prerendered.FindAll("[data-rune-video-controls]"));
    }

    [Fact]
    public void 图片的备选源_渲染成picture_后备仍是img()
    {
        var plain = Render<Image>(p => p.Add(x => x.Src, "/cover.jpg").Add(x => x.Alt, "封面"));
        Assert.Empty(plain.FindAll("picture"));

        var cut = Render<Image>(p => p.Add(x => x.Src, "/cover.jpg").Add(x => x.Alt, "封面")
            .Add(x => x.Sources, [new ImageSource("/cover.avif", "image/avif"), new ImageSource("/cover-dark.webp", "image/webp", "(prefers-color-scheme: dark)")]));

        var sources = cut.FindAll("picture > source");
        Assert.Equal(["/cover.avif", "/cover-dark.webp"], sources.Select(source => source.GetAttribute("srcset")));
        Assert.Equal(("image/webp", "(prefers-color-scheme: dark)"), (sources[1].GetAttribute("type"), sources[1].GetAttribute("media")));
        var image = cut.Find("picture > img");
        Assert.Equal(("/cover.jpg", "封面"), (image.GetAttribute("src"), image.GetAttribute("alt")));
        Assert.True(image.HasAttribute("data-rune-image"));
    }

    [Theory]
    [InlineData(35, "success")]
    [InlineData(70, "success")]
    [InlineData(82, "warning")]
    [InlineData(100, "danger")]
    [InlineData(140, "danger")]
    public void 量表_值落在哪一段用哪一段的色调_角色是meter(int value, string tone)
    {
        IReadOnlyList<ProgressBand> bands = [new(70, Tone.Success), new(90, Tone.Warning), new(100, Tone.Danger)];
        var cut = Render<Progress>(p => p.Add(x => x.Value, value).Add(x => x.Tone, Tone.Info).Add(x => x.Bands, bands).AddUnmatched("aria-label", "磁盘占用"));

        var meter = cut.Find("[role=meter]");
        Assert.Empty(cut.FindAll("[role=progressbar]"));
        // 色调在指示条上：区间的色调盖过 Tone
        var registry = Services.GetRequiredService<Rune.Styles.StyleRegistry>();
        var classes = meter.QuerySelector("[data-part=indicator]")!.ClassName!.Split(' ');
        var rules = string.Join("", registry.Rules.Where(rule => classes.Any(name => rule.Contains($".{name}{{"))));
        Assert.Contains($"{tone}-text)", rules);
        Assert.DoesNotContain("info-text)", rules);
    }

    [Fact]
    public void 没写区间时仍是进度条()
    {
        var cut = Render<Progress>(p => p.Add(x => x.Value, 40).AddUnmatched("aria-label", "上传"));
        Assert.NotNull(cut.Find("[role=progressbar]"));
    }
}
