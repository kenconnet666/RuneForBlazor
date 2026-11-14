using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>搜索框（同 vue-tsx-pro 0.26.0 的 SearchField）：值每敲一个字都更新，OnSearch 只在停手、回车、清空时调用。</summary>
public class SearchFieldTests : BunitContext
{
    readonly FakeTimeProvider time = new();

    public SearchFieldTests()
    {
        Services.AddRuneUI();
        Services.AddSingleton<TimeProvider>(time);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    (IRenderedComponent<SearchField> Cut, List<string> Searched, Func<string?> Value) RenderSearch(int? delay = null, string? initial = null)
    {
        var searched = new List<string>();
        var value = initial;
        var cut = Render<SearchField>(p =>
        {
            p.Add(x => x.Value, value).Add(x => x.ValueChanged, v => value = v).Add(x => x.OnSearch, text => searched.Add(text));
            if (delay is { } ms) p.Add(x => x.Delay, ms);
        });
        return (cut, searched, () => value);
    }

    [Fact]
    public void 是搜索框_默认有图标_名称与占位文字_清空按钮()
    {
        var cut = Render<SearchField>(p => p.Add(x => x.Value, "轴承"));

        var input = cut.Find("input");
        Assert.Equal(("search", "搜索", "搜索", "search"),
            (input.GetAttribute("type"), input.GetAttribute("placeholder"), input.GetAttribute("aria-label"), input.GetAttribute("enterkeyhint")));
        Assert.NotNull(cut.Find("svg"));
        Assert.NotNull(cut.Find("button"));

        // 默认值都可以改
        var custom = Render<SearchField>(p => p.Add(x => x.Clearable, false).Add(x => x.Value, "轴承").AddUnmatched("placeholder", "搜索订单"));
        Assert.Equal("搜索订单", custom.Find("input").GetAttribute("placeholder"));
        Assert.Empty(custom.FindAll("button"));
    }

    [Fact]
    public void 打字每次都更新值_停手300毫秒才搜索_同一个词不重复()
    {
        var (cut, searched, value) = RenderSearch();

        cut.Find("input").Input("轴");
        cut.Find("input").Input("轴承");
        Assert.Equal("轴承", value());
        Assert.Empty(searched);

        time.Advance(TimeSpan.FromMilliseconds(299));
        Assert.Empty(searched);
        time.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => Assert.Equal(["轴承"], searched));

        // 改了又改回来：停手时还是上一次的词，不再搜索
        cut.Find("input").Input("轴承座");
        cut.Find("input").Input("轴承");
        time.Advance(TimeSpan.FromMilliseconds(300));
        cut.WaitForState(() => true);
        Assert.Equal(["轴承"], searched);
    }

    [Fact]
    public void 回车与清空立即搜索()
    {
        var (cut, searched, _) = RenderSearch();

        cut.Find("input").Input("法兰");
        // 回车提交输入框（change）：不等停手
        cut.Find("input").Change("法兰");
        Assert.Equal(["法兰"], searched);

        // 父级把新的值交回来，清空按钮才出现
        cut.Render(p => p.Add(x => x.Value, "法兰"));
        cut.Find("button").Click();
        Assert.Equal(["法兰", ""], searched);

        // 已经搜过了：迟到的计时不再触发
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(["法兰", ""], searched);
    }

    [Fact]
    public void Delay是0时每次输入都搜索()
    {
        var (cut, searched, _) = RenderSearch(delay: 0);

        cut.Find("input").Input("a");
        cut.Find("input").Input("ab");
        Assert.Equal(["a", "ab"], searched);
    }

    [Fact]
    public void 父级从外面改值不触发搜索_之后打回同一个词也不搜()
    {
        var searched = new List<string>();
        var cut = Render<SearchField>(p => p.Add(x => x.Value, "").Add(x => x.OnSearch, text => searched.Add(text)));

        // 重置、恢复保存的条件
        cut.Render(p => p.Add(x => x.Value, "已保存的条件"));
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Empty(searched);
        Assert.Equal("已保存的条件", cut.Find("input").GetAttribute("value"));

        cut.Find("input").Input("已保存的条件");
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Empty(searched);
    }
}
