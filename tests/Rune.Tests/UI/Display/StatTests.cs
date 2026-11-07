using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>数字格式、会滚动的数字、统计数值、时间线、主栏与侧栏。</summary>
public class StatTests : BunitContext
{
    static readonly Lang Zh = Lang.ZhCn;

    public StatTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Theory]
    [InlineData(1234.5, "1,234.5")]
    [InlineData(0.1234567, "0.123")]
    [InlineData(-42, "-42")]
    public void 默认格式_千分位_最多三位小数(decimal value, string expected) =>
        Assert.Equal(expected, LangFormat.Number(value, NumberFormat.Default, Zh));

    [Fact]
    public void 简写_中文按万亿_英文按KMB_日文按万億_同Intl的compact()
    {
        var compact = new NumberFormat { Notation = NumberNotation.Compact };
        decimal[] values = [182000, 96000, 9600, 1450000, 8200000, 999, 12345678901];
        Assert.Equal("18万|9.6万|9600|145万|820万|999|123亿", string.Join('|', values.Select(v => LangFormat.Number(v, compact, Zh))));
        Assert.Equal("182K|96K|9.6K|1.5M|8.2M|999|12B", string.Join('|', values.Select(v => LangFormat.Number(v, compact, Lang.EnUs))));
        Assert.Equal("¥820万", LangFormat.Number(8200000, compact with { Style = NumberStyle.Currency }, Zh));
        Assert.Equal("1.2億", LangFormat.Number(123456789, compact, Lang.EnUs with { Code = "ja-JP" }));
    }

    [Fact]
    public void 百分比_缩放与单位_带符号_金额()
    {
        Assert.Equal("12%", LangFormat.Number(0.123m, NumberFormat.Percent, Zh));
        Assert.Equal("+12.3%", LangFormat.Number(0.123m, Stat.DefaultChangeFormat, Zh));
        Assert.Equal("-4.2%", LangFormat.Number(-0.042m, Stat.DefaultChangeFormat, Zh));
        Assert.Equal("0%", LangFormat.Number(0m, Stat.DefaultChangeFormat, Zh));
        Assert.Equal("15.54 万元", LangFormat.Number(155353.2m, new NumberFormat { Scale = 10_000, MaximumFractionDigits = 2, Suffix = "万元" }, Zh));
        Assert.Equal("1,087.50", LangFormat.Number(10_875_000m, new NumberFormat { Scale = 10_000, MinimumFractionDigits = 2, MaximumFractionDigits = 2 }, Zh));
        Assert.Equal("¥12,880.35", LangFormat.Number(12_880.35m, new NumberFormat { Style = NumberStyle.Currency }, Zh));
        // 只写了最少位数：最多位数跟着它（同 Intl）
        Assert.Equal("4.60", LangFormat.Number(4.6m, new NumberFormat { MinimumFractionDigits = 2 }, Zh));
        // 0.5 远离零舍入（同 Intl）
        Assert.Equal("3", LangFormat.Number(2.5m, new NumberFormat { MaximumFractionDigits = 0 }, Zh));
    }

    [Fact]
    public void 浏览器里逐帧格式化用的选项()
    {
        var cut = Render<AnimatedNumber>(p => p.Add(x => x.Value, 1m).Add(x => x.Format, new NumberFormat { Style = NumberStyle.Currency, Scale = 10_000, Suffix = "万" }));
        Assert.Equal("""{"style":"currency","currency":"CNY","minimumFractionDigits":2,"maximumFractionDigits":2,"signDisplay":"auto","useGrouping":true,"scale":10000,"suffix":"万"}""",
            cut.Find("[data-rune-tween]").GetAttribute("data-format"));
        Assert.Equal("zh-CN", cut.Find("[data-rune-tween]").GetAttribute("data-locale"));
    }

    [Fact]
    public void 会滚动的数字_终值给读屏_滚动的部分对读屏隐藏_按正负着色()
    {
        var cut = Render<AnimatedNumber>(p => p.Add(x => x.Value, -0.05m).Add(x => x.Format, NumberFormat.Percent).Add(x => x.Trend, true).Add(x => x.From, 0m));
        var value = cut.Find("[data-rune-tween]");
        Assert.Equal("true", value.GetAttribute("aria-hidden"));
        Assert.Equal("-0.05", value.GetAttribute("data-value"));
        Assert.Equal("0", value.GetAttribute("data-from"));
        Assert.Equal("lg", value.GetAttribute("data-rune-tween"));
        Assert.Equal("-5%", value.TextContent);
        Assert.Contains("-5%", cut.Find("span > span:last-child").TextContent);
        Assert.Equal("down", cut.Find("span").GetAttribute("data-trend"));
    }

    [Fact]
    public void 统计数值_名称与数值成对_变化量按正负与InvertChange着色()
    {
        var cut = Render<Stat>(p => p
            .Add(x => x.Label, "平均响应时间")
            .Add(x => x.Value, 212m)
            .Add(x => x.Unit, "ms")
            .Add(x => x.Change, -0.18m)
            .Add(x => x.InvertChange, true)
            .Add(x => x.ChangeLabel, "较上周"));

        Assert.Equal("平均响应时间", cut.Find("dl > dt").TextContent.Trim());
        Assert.Contains("212", cut.Find("dl > dd").TextContent);
        var change = cut.Find("dd[data-trend]");
        Assert.Equal("down", change.GetAttribute("data-trend"));
        Assert.Contains("-18%", change.TextContent);
        Assert.Contains("较上周", change.TextContent);
        // 越低越好：下降是成功色
        Assert.Equal(TextTone.Success, cut.FindComponents<AnimatedNumber>()[1].Instance.Tone);
    }

    [Fact]
    public void 加载中_数值换成骨架_带aria_busy_文字值原样显示()
    {
        var loading = Render<Stat>(p => p.Add(x => x.Label, "项目总数").Add(x => x.Value, 6m).Add(x => x.Loading, true));
        Assert.Equal("true", loading.Find("dl").GetAttribute("aria-busy"));
        Assert.Empty(loading.FindComponents<AnimatedNumber>());

        var text = Render<Stat>(p => p.Add(x => x.Label, "测算").Add(x => x.Text, "—"));
        Assert.Equal("—", text.Find("dl > dd > span").TextContent);
    }

    [Fact]
    public void 时间线_倒序_进行中_收起旧的()
    {
        TimelineItem[] items = [new("一"), new("二") { Time = "10-18", DateTime = "2026-10-18" }, new("三"), new("四")];
        var cut = Render<Timeline>(p => p.Add(x => x.Items, items).Add(x => x.Pending, "处理中").Add(x => x.MaxVisible, 2));

        var titles = cut.FindAll("li").Select(li => li.TextContent.Trim()).ToList();
        Assert.StartsWith("展开其余 2 条", titles[0]);
        Assert.Equal(["三", "四", "处理中"], titles.Skip(1).Select(t => t[..Math.Min(t.Length, 3)].Trim()));
        Assert.Equal("pending", cut.FindAll("li")[^1].GetAttribute("data-state"));

        cut.Find("button[aria-expanded=false]").Click();
        Assert.Equal(6, cut.FindAll("li").Count);
        Assert.Equal("2026-10-18", cut.Find("time").GetAttribute("datetime"));

        var reversed = Render<Timeline>(p => p.Add(x => x.Items, items).Add(x => x.Reverse, true));
        Assert.True(reversed.Find("ol").HasAttribute("reversed"));
        Assert.Equal("四", reversed.Find("li").TextContent.Trim());
    }

    [Fact]
    public void 主栏与侧栏_侧栏是带名称的aside_宽度经变量()
    {
        var cut = Render<AsideLayout>(p => p
            .Add(x => x.AsideLabel, "收益")
            .Add(x => x.AsideWidth, CssLength.Rem(18))
            .Add(x => x.Aside, (RenderFragment)(b => b.AddContent(0, "侧栏")))
            .AddChildContent("主栏"));
        Assert.Equal("收益", cut.Find("aside").GetAttribute("aria-label"));
        Assert.Contains("--rune-aside-width:18rem", cut.Markup.Replace(" ", ""));
    }

    [Fact]
    public void 比例条_读屏名称带各段数量_零的段不画但留在图例里()
    {
        ProportionSegment[] segments = [new("a", "已完成", 25) { Tone = Tone.Success }, new("b", "受阻", 0), new("c", "进行中", 35) { Color = "#3e63dd" }];
        var cut = Render<ProportionBar>(p => p.Add(x => x.Label, "工单构成").Add(x => x.Segments, segments).Add(x => x.ValueText, s => $"{s.Value} 项"));
        Assert.Equal("工单构成：已完成 25 项，受阻 0 项，进行中 35 项", cut.Find("[role=img]").GetAttribute("aria-label"));
        Assert.Equal(2, cut.FindAll("[role=img] > span").Count);
        Assert.Equal(3, cut.FindAll("li").Count);
        Assert.Contains("--rune-status-dot-color:#3e63dd", cut.FindAll("[role=img] > span")[1].GetAttribute("style")!.Replace(" ", ""));
    }

    [Fact]
    public void 截断文字_提示不再当说明念_展开按钮带aria_expanded()
    {
        var cut = Render<Ellipsis>(p => p.AddChildContent("很长的供应商名称"));
        var text = cut.Find("[data-rune-ellipsis-text]");
        Assert.True(text.HasAttribute("data-rune-tooltip"));
        Assert.False(text.HasAttribute("aria-describedby"));
        Assert.Equal("tooltip", cut.Find("[data-rune-ellipsis]").GetAttribute("data-rune-ellipsis"));

        var expandable = Render<Ellipsis>(p => p.Add(x => x.Lines, 2).Add(x => x.Expandable, true).AddChildContent("很长的描述"));
        Assert.False(expandable.Find("[data-rune-ellipsis-text]").HasAttribute("data-rune-tooltip"));
        var toggle = expandable.Find("button");
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        toggle.Click();
        Assert.Equal("true", expandable.Find("button").GetAttribute("aria-expanded"));
        Assert.True(expandable.Find("[data-rune-ellipsis]").HasAttribute("data-expanded"));
        Assert.Equal("收起", expandable.Find("button").TextContent);
    }

    [Fact]
    public void 跑马灯_复制的那份对读屏隐藏且不可聚焦_速度与方向交给JS()
    {
        var cut = Render<Marquee>(p => p.Add(x => x.Direction, MarqueeDirection.Up).Add(x => x.Speed, 20).Add(x => x.Paused, true).AddChildContent("<span>公告</span>"));
        var root = cut.Find("[role=marquee]");
        Assert.Equal("20", root.GetAttribute("data-rune-marquee"));
        Assert.Equal("up", root.GetAttribute("data-direction"));
        Assert.True(root.HasAttribute("data-paused"));
        var groups = root.FirstElementChild!.Children;
        Assert.True(groups[0].HasAttribute("data-rune-marquee-group"));
        Assert.Equal("true", groups[1].GetAttribute("aria-hidden"));
        Assert.True(groups[1].HasAttribute("inert"));
    }

    [Fact]
    public void 完成清单_不适用的不算进度_受阻的不能勾_勾选报告新状态()
    {
        ChecklistItem[] items =
        [
            new("备份") { Status = ChecklistStatus.Done },
            new("迁移"),
            new("冒烟") { Status = ChecklistStatus.Blocked },
            new("CDN") { Status = ChecklistStatus.Skipped },
        ];
        ChecklistCheck? checkedItem = null;
        var cut = Render<Checklist>(p => p.Add(x => x.Items, items).Add(x => x.Summary, true).Add(x => x.OnCheckedChange, (ChecklistCheck c) => checkedItem = c));
        Assert.Contains("已完成 1 / 3", cut.Markup);
        var boxes = cut.FindAll("button[role=checkbox]");
        Assert.Equal("true", boxes[0].GetAttribute("aria-checked"));
        Assert.True(boxes[2].HasAttribute("disabled"));
        Assert.True(boxes[3].HasAttribute("disabled"));
        boxes[1].Click();
        Assert.Equal(("迁移", true), (checkedItem!.Item.Label, checkedItem.Done));

        var icons = Render<Checklist>(p => p.Add(x => x.Items, items).Add(x => x.Layout, ChecklistLayout.Icons));
        Assert.Equal("备份已完成，迁移未完成，冒烟受阻，CDN不适用", icons.Find("[role=img]").GetAttribute("aria-label"));
    }

    [Fact]
    public void 水印_选项写成JSON交给JS_整页且不包内容时只渲染那一层()
    {
        var cut = Render<Watermark>(p => p.Add(x => x.Lines, ["林小满", "2026-10-24"]).Add(x => x.Rotate, -30).AddChildContent("<p>报表</p>"));
        var layer = cut.Find("[data-rune-watermark]");
        Assert.Equal("true", layer.GetAttribute("aria-hidden"));
        var options = layer.GetAttribute("data-rune-watermark")!;
        Assert.Contains("\"lines\":[\"林小满\",\"2026-10-24\"]", options);
        Assert.Contains("\"rotate\":-30", options);
        Assert.DoesNotContain("\"image\"", options);

        var page = Render<Watermark>(p => p.Add(x => x.Text, "内部资料").Add(x => x.Fullscreen, true));
        Assert.Single(page.Nodes.OfType<AngleSharp.Dom.IElement>());
    }

    [Fact]
    public void 迷你图_空值处断开_读屏念概括()
    {
        var cut = Render<Sparkline>(p => p.Add(x => x.Data, [3m, 5m, null, 4m, 8m]).Add(x => x.Label, "近 5 月"));
        Assert.Equal("近 5 月：4 个值，从 3 到 8，最低 3，最高 8", cut.Find("[role=img]").GetAttribute("aria-label"));
        Assert.Equal(2, cut.FindAll("path").Count);
        Assert.Equal("M0 100 L1 60", cut.FindAll("path")[0].GetAttribute("d"));

        var bars = Render<Sparkline>(p => p.Add(x => x.Data, [3m, null, 4m]).Add(x => x.Type, SparklineType.Bar));
        Assert.Equal(2, bars.FindAll("rect").Count);
        Assert.Empty(bars.FindAll("span[aria-hidden]"));
        // 最新的一根与前面的柱不同类（实色、淡下去）；不标最后一个值时都一样
        Assert.NotEqual(bars.FindAll("rect")[0].ClassName, bars.FindAll("rect")[1].ClassName);
        var plain = Render<Sparkline>(p => p.Add(x => x.Data, [3m, null, 4m]).Add(x => x.Type, SparklineType.Bar).Add(x => x.ShowLast, false));
        Assert.Equal(plain.FindAll("rect")[0].ClassName, plain.FindAll("rect")[1].ClassName);
    }

    [Fact]
    public void 仪表盘_meter的取值与目标_数值弧取所在区间的色调()
    {
        GaugeBand[] bands = [new(0.6m, Tone.Danger), new(0.9m, Tone.Warning), new(1, Tone.Success)];
        var cut = Render<Gauge>(p => p.Add(x => x.Value, 0.82m).Add(x => x.Max, 1).Add(x => x.ValueFormat, NumberFormat.Percent)
            .Add(x => x.Target, 0.9m).Add(x => x.Bands, bands).Add(x => x.Label, "完成率"));
        var meter = cut.Find("[role=meter]");
        Assert.Equal("0.82", meter.GetAttribute("aria-valuenow"));
        Assert.Equal("82%，目标 90%", meter.GetAttribute("aria-valuetext"));
        Assert.Equal("完成率", meter.GetAttribute("aria-label"));
        Assert.Single(cut.FindAll("line"));
        // 轨道、数值弧、三段区间
        Assert.Equal(5, cut.FindAll("path").Count);
    }

    [Fact]
    public void 分栏_分隔条是可聚焦的separator_比例经变量_收起的一栏inert()
    {
        var cut = Render<Splitter>(p => p.Add(x => x.Size, 40).Add(x => x.Collapsible, SplitterSide.End).Add(x => x.Collapsed, true)
            .Add(x => x.StartContent, (RenderFragment)(b => b.AddContent(0, "列表"))).Add(x => x.EndContent, (RenderFragment)(b => b.AddContent(0, "详情"))));
        var root = cut.Find("[data-rune-splitter]");
        Assert.Equal("end", root.GetAttribute("data-collapsed"));
        Assert.Contains("--rune-split-start:40fr", root.GetAttribute("style")!.Replace(" ", ""));
        var handle = cut.Find("[role=separator]");
        Assert.Equal("0", handle.GetAttribute("tabindex"));
        Assert.Equal("100", handle.GetAttribute("aria-valuenow"));
        Assert.True(cut.Find("[id$=-end]").HasAttribute("inert"));

        handle.TriggerEvent("onruneresize", new ResizeEventArgs { Size = 55 });
        Assert.False(cut.Find("[data-rune-splitter]").HasAttribute("data-collapsed"));
        Assert.Equal("55", cut.Find("[role=separator]").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void 拖动排序_JS交来的新顺序写回_对不上时不动_把手的名称与说明()
    {
        IReadOnlyList<string> items = ["a", "b", "c"];
        var cut = Render<Sortable<string>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ItemsChanged, (IReadOnlyList<string> v) => items = v)
            .Add(x => x.KeyOf, s => s)
            .Add(x => x.LabelOf, s => s.ToUpperInvariant())
            .Add(x => x.ChildContent, item => b => b.AddContent(0, item)));

        var list = cut.Find("[data-rune-sortable]");
        Assert.Equal("vertical", list.GetAttribute("data-rune-sortable"));
        Assert.Contains("\"moved\":\"{label} 移到第 900001 位，共 900002 项\"", list.GetAttribute("data-texts"));
        var handle = cut.Find("[data-rune-sortable-handle]");
        Assert.Equal("移动 A", handle.GetAttribute("aria-label"));
        Assert.Equal("false", handle.GetAttribute("aria-pressed"));
        Assert.Equal(cut.Find("[id^=sortable]").Id, handle.GetAttribute("aria-describedby"));

        list.TriggerEvent("onrunereorder", new ReorderEventArgs { Values = ["c", "a", "b"] });
        Assert.Equal(["c", "a", "b"], items);
        list.TriggerEvent("onrunereorder", new ReorderEventArgs { Values = ["c", "x", "b"] });
        Assert.Equal(["c", "a", "b"], items);
    }
}
