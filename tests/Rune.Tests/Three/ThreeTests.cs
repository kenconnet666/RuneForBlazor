using System.Text.Json;
using System.Text.Json.Nodes;
using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.Three;

/// <summary>
/// 3D 效果（Rune.Three）的 C# 一侧：画布外层的属性（场景、参数、画质、暂停、分辨率）、无障碍、降级内容，
/// 各效果交给 JS 的参数（data-options），以及 JS 经 rune-command 报回来的状态。渲染本身在浏览器里（端到端测试）。
/// </summary>
public class ThreeTests : BunitContext
{
    public ThreeTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static JsonElement Options<T>(IRenderedComponent<T> cut) where T : IComponent =>
        JsonDocument.Parse(cut.Find("[data-rune-three]").GetAttribute("data-options")!).RootElement;

    [Fact]
    public void 画布_场景地址_参数_默认是装饰()
    {
        var cut = Render<ThreeCanvas>(p => p
            .Add(c => c.Setup, "js/knot.js")
            .Add(c => c.Options, new JsonObject { ["speed"] = 1.5 }));
        var root = cut.Find("[data-rune-three]");
        Assert.Equal("js/knot.js", root.GetAttribute("data-rune-three"));
        Assert.Equal("""{"speed":1.5}""", root.GetAttribute("data-options"));
        Assert.Equal("auto", root.GetAttribute("data-quality"));
        Assert.Null(root.GetAttribute("data-paused"));
        Assert.Null(root.GetAttribute("data-resolution"));
        Assert.Equal("true", root.GetAttribute("aria-hidden"));
        Assert.Null(root.GetAttribute("role"));
        // canvas 由 JS 建：C# 只给出它的属性模板；状态（data-state）也由 JS 写
        Assert.Equal("canvas", cut.Find("template").GetAttribute("data-part"));
        Assert.Null(root.GetAttribute("data-state"));
        Assert.Empty(cut.FindAll("canvas"));
    }

    [Fact]
    public void 画布_写了名称时是一张图_画质暂停分辨率写在属性上()
    {
        var cut = Render<ThreeCanvas>(p => p
            .Add(c => c.Setup, "js/knot.js")
            .Add(c => c.Quality, ThreeQuality.Off)
            .Add(c => c.Paused, true)
            .Add(c => c.Resolution, 0.5)
            .AddUnmatched("aria-label", "旋转的环面结"));
        var root = cut.Find("[data-rune-three]");
        Assert.Equal("img", root.GetAttribute("role"));
        Assert.Null(root.GetAttribute("aria-hidden"));
        Assert.Equal("off", root.GetAttribute("data-quality"));
        Assert.Equal("", root.GetAttribute("data-paused"));
        Assert.Equal("0.5", root.GetAttribute("data-resolution"));
    }

    [Fact]
    public void 降级内容_默认是主题色的渐变_给了就用给的()
    {
        var plain = Render<ThreeCanvas>(p => p.Add(c => c.Setup, "a.js"));
        var custom = Render<ThreeCanvas>(p => p.Add(c => c.Setup, "a.js").Add(c => c.Fallback, (RenderFragment)(b => b.AddContent(0, "特效已关闭"))));
        var fallback = plain.Find("[data-part=fallback]");
        Assert.Equal("true", fallback.GetAttribute("aria-hidden"));
        Assert.NotEqual(fallback.ClassName, custom.Find("[data-part=fallback]").ClassName);
        Assert.Equal("特效已关闭", custom.Find("[data-part=fallback]").TextContent);
    }

    [Fact]
    public void 渲染状态_JS报回来时调用OnStatusChange_没人要时不挂处理()
    {
        ThreeStatus? status = null;
        var cut = Render<ThreeCanvas>(p => p.Add(c => c.Setup, "a.js").Add(c => c.OnStatusChange, (ThreeStatus s) => status = s));
        cut.Find("[data-rune-three]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "three-status", Text = "running" });
        Assert.Equal(ThreeStatus.Running, status);

        var silent = Render<ThreeCanvas>(p => p.Add(c => c.Setup, "a.js"));
        Assert.Throws<MissingEventHandlerException>(() =>
            silent.Find("[data-rune-three]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "three-status", Text = "running" }));
    }

    [Fact]
    public void 效果外层_画布在底层_有内容时才有内容层()
    {
        var empty = Render<EffectSurface>(p => p.Add(c => c.Setup, "a.js"));
        Assert.NotNull(empty.Find("[data-rune-three-host]"));
        Assert.Equal("three-canvas", empty.Find("[data-rune-three]").GetAttribute("data-scope"));
        Assert.Empty(empty.FindAll("[data-scope=effect-surface][data-part=content]"));

        var filled = Render<EffectSurface>(p => p.Add(c => c.Setup, "a.js").AddChildContent("<h1>标题</h1>"));
        Assert.Equal("标题", filled.Find("[data-scope=effect-surface][data-part=content] h1").TextContent);
    }

    [Fact]
    public void 着色器背景_参数交给内置场景_色板默认是主题色板()
    {
        var cut = Render<ShaderBackground>(p => p
            .Add(c => c.FragmentShader, "vec4 shade(vec2 uv) { return vec4(uv, 0.0, 1.0); }")
            .Add(c => c.Uniforms, new Dictionary<string, double> { ["uRings"] = 6 })
            .Add(c => c.Speed, 2));
        Assert.Equal("shader-background", cut.Find("[data-rune-three]").GetAttribute("data-rune-three"));
        var options = Options(cut);
        Assert.StartsWith("vec4 shade", options.GetProperty("fragmentShader").GetString());
        Assert.Equal(6, options.GetProperty("uniforms").GetProperty("uRings").GetDouble());
        Assert.Equal(2, options.GetProperty("speed").GetDouble());
        Assert.Equal(ThreePalettes.Shader.Select(c => c.ToString()), options.GetProperty("palette").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void 渐变与极光_默认降一半分辨率()
    {
        Assert.Equal("0.5", Render<MeshGradient>().Find("[data-rune-three]").GetAttribute("data-resolution"));
        Assert.Equal("0.5", Render<AuroraBackground>().Find("[data-rune-three]").GetAttribute("data-resolution"));
        Assert.Null(Render<ParticleField>().Find("[data-rune-three]").GetAttribute("data-resolution"));
        Assert.Null(Render<MeshGradient>(p => p.Add(c => c.Resolution, 1)).Find("[data-rune-three]").GetAttribute("data-resolution"));
    }

    [Fact]
    public void 参数没变时_交给JS的JSON是同一个_不重新序列化()
    {
        var cut = Render<ParticleField>(p => p.Add(c => c.Links, true));
        var before = cut.Find("[data-rune-three]").GetAttribute("data-options");
        cut.Render(p => p.Add(c => c.Links, true));
        Assert.Same(before, cut.Find("[data-rune-three]").GetAttribute("data-options"));
        Assert.True(Options(cut).GetProperty("links").GetBoolean());
    }

    [Fact]
    public void 线框地形_画法写成JS的名字()
    {
        var cut = Render<WaveGrid>(p => p.Add(c => c.Type, WaveGridType.Dots));
        Assert.Equal("dots", Options(cut).GetProperty("type").GetString());
    }

    [Fact]
    public void 地球_标注与弧线_颜色默认取主题_能拖动时有抓手光标()
    {
        var cut = Render<Globe>(p => p
            .Add(c => c.Markers, [new GlobeMarker(31.23, 121.47) { Size = 1.4 }, new GlobeMarker(1.35, 103.82) { Color = Tones.Danger.Solid }])
            .Add(c => c.Arcs, [new GlobeArc(new(31.23, 121.47), new(1.35, 103.82))]));
        var options = Options(cut);
        var markers = options.GetProperty("markers").EnumerateArray().ToList();
        Assert.Equal(1.4, markers[0].GetProperty("size").GetDouble());
        Assert.False(markers[0].TryGetProperty("color", out _));
        Assert.Equal(Tones.Danger.Solid.ToString(), markers[1].GetProperty("color").GetString());
        Assert.Equal([31.23, 121.47], options.GetProperty("arcs")[0].GetProperty("from").EnumerateArray().Select(e => e.GetDouble()));
        Assert.Equal(20, options.GetProperty("center").GetProperty("lat").GetDouble());
        Assert.Equal(Colors.TextMuted.ToString(), options.GetProperty("landColor").GetString());
        Assert.Equal(Tones.Accent.Solid.ToString(), options.GetProperty("markerColor").GetString());

        var still = Render<Globe>(p => p.Add(c => c.Draggable, false));
        Assert.NotEqual(cut.Find("[data-rune-three-host]").ClassName, still.Find("[data-rune-three-host]").ClassName);
        Assert.False(Options(still).GetProperty("draggable").GetBoolean());
    }

    [Fact]
    public void 模型查看器_写了说明是一张图_能操作时可聚焦_加载状态一开始是加载中()
    {
        var cut = Render<ModelViewer>(p => p.Add(c => c.Src, "models/chair.glb").Add(c => c.Alt, "扶手椅"));
        var host = cut.Find("[data-rune-three-host]");
        Assert.Equal("img", host.GetAttribute("role"));
        Assert.Equal("扶手椅", host.GetAttribute("aria-label"));
        Assert.Equal("0", host.GetAttribute("tabindex"));
        Assert.Equal("loading", host.GetAttribute("data-status"));
        Assert.Equal("true", host.GetAttribute("aria-busy"));
        // 转圈与出错的提示都在，按 data-status（JS 写）显示其中一个
        Assert.Contains("正在加载模型", cut.Markup);
        Assert.Contains("模型加载失败", cut.Markup);

        var fixedView = Render<ModelViewer>(p => p.Add(c => c.ObjectModule, "js/models.js#gem").Add(c => c.Controls, false));
        Assert.Null(fixedView.Find("[data-rune-three-host]").GetAttribute("tabindex"));
        Assert.Equal("js/models.js#gem", Options(fixedView).GetProperty("object").GetString());
    }

    [Fact]
    public void 模型查看器_加载完与失败交给回调()
    {
        var loaded = false;
        string? error = null;
        var cut = Render<ModelViewer>(p => p
            .Add(c => c.Src, "models/missing.glb")
            .Add(c => c.OnLoad, () => loaded = true)
            .Add(c => c.OnError, (string reason) => error = reason));
        var host = cut.Find("[data-rune-three-host]");
        host.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "model-error", Text = "404" });
        Assert.Equal("404", error);
        host.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "model-load" });
        Assert.True(loaded);
    }

    [Fact]
    public void 彩纸_一开始不放_触发一次加一批_落完后收起并调用OnDone()
    {
        var done = 0;
        var cut = Render<Confetti>(p => p.Add(c => c.Trigger, 0).Add(c => c.OnDone, () => done++));
        Assert.Empty(cut.FindAll("[data-rune-three]"));

        cut.Render(p => p.Add(c => c.Trigger, 1).Add(c => c.Angle, 60));
        var layer = cut.Find("[data-rune-three-host]");
        Assert.Equal("manual", layer.GetAttribute("popover"));
        Assert.NotNull(layer.GetAttribute("data-rune-three-layer"));
        var bursts = Options(cut).GetProperty("bursts").EnumerateArray().ToList();
        Assert.Single(bursts);
        Assert.Equal(1, bursts[0].GetProperty("id").GetInt32());
        Assert.Equal(60, bursts[0].GetProperty("angle").GetDouble());

        // 还在飘的时候又触发了一次：JS 报告落完了第 1 批，第 2 批还留着
        cut.Render(p => p.Add(c => c.Trigger, 2));
        cut.Find("[data-rune-three-host]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "confetti-done", Text = "1" });
        Assert.Equal(0, done);
        Assert.Equal(2, Options(cut).GetProperty("bursts")[0].GetProperty("id").GetInt32());

        cut.Find("[data-rune-three-host]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "confetti-done", Text = "2" });
        Assert.Equal(1, done);
        Assert.Empty(cut.FindAll("[data-rune-three]"));
    }

    [Fact]
    public void 彩纸_没有WebGL时直接收尾()
    {
        var done = 0;
        var cut = Render<Confetti>(p => p.Add(c => c.Trigger, 0).Add(c => c.Fullscreen, false).Add(c => c.OnDone, () => done++));
        cut.Render(p => p.Add(c => c.Trigger, 1));
        Assert.Null(cut.Find("[data-rune-three-host]").GetAttribute("popover"));
        cut.Find("[data-rune-three-host]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "three-status", Text = "fallback" });
        Assert.Equal(1, done);
        Assert.Empty(cut.FindAll("[data-rune-three]"));
    }
}
