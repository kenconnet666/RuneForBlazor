using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>滚动加载：一页页接上、按 key 去重、取完了；失败停下可重试；刷新时丢掉过期的结果；参数变化防抖；列表末尾的状态。</summary>
public class InfiniteListTests : BunitContext
{
    public InfiniteListTests() => Services.AddRuneUI();

    // 每次请求一个可以手动完成的结果
    sealed class FakePages
    {
        public readonly List<(int Cursor, TaskCompletionSource<InfinitePage<int, int>> Result)> Calls = [];

        public Task<InfinitePage<int, int>> Load(int cursor, CancellationToken cancellation)
        {
            var result = new TaskCompletionSource<InfinitePage<int, int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellation.Register(() => result.TrySetCanceled(cancellation));
            Calls.Add((cursor, result));
            return result.Task;
        }
    }

    [Fact]
    public async Task 一页页接上_按key去重_取完了不再请求()
    {
        var pages = new FakePages();
        var list = new InfiniteList<int, int>(pages.Load, new() { Key = item => item });
        Assert.True(list.Loading);
        Assert.Equal(0, pages.Calls[0].Cursor);

        pages.Calls[0].Result.SetResult(new([1, 2, 3], 1, true));
        await WaitUntil(() => list.Loaded);
        Assert.Equal([1, 2, 3], list.Items);

        // 离末尾不到 5 项：取下一页；新数据插进来时开头和上一页重复
        await TriggerAsync(list.OnRangeChange(new VisibleRange(0, 2)));
        Assert.Equal(1, pages.Calls[1].Cursor);
        pages.Calls[1].Result.SetResult(new([3, 4], default, false));
        await WaitUntil(() => !list.Loading);
        Assert.Equal([1, 2, 3, 4], list.Items);
        Assert.False(list.HasMore);

        await list.LoadMoreAsync();
        Assert.Equal(2, pages.Calls.Count);
    }

    [Fact]
    public async Task 失败停下_重试接着取_刷新时丢掉过期的结果()
    {
        var pages = new FakePages();
        var list = new InfiniteList<int, int>(pages.Load);
        pages.Calls[0].Result.SetException(new HttpRequestException("断网"));
        await WaitUntil(() => !list.Loading);
        Assert.IsType<HttpRequestException>(list.Error);
        Assert.False(list.Loaded);

        _ = list.LoadMoreAsync();
        Assert.Null(list.Error);
        Assert.Equal(0, pages.Calls[1].Cursor);

        // 刷新顶掉进行中的：旧的结果回来也不采用
        _ = list.RefreshAsync();
        pages.Calls[1].Result.TrySetResult(new([9], 1, true));
        pages.Calls[2].Result.SetResult(new([1], 1, true));
        await WaitUntil(() => list.Loaded);
        Assert.Equal([1], list.Items);
    }

    [Fact]
    public async Task 参数变化防抖_只按最后一次从第一页重来()
    {
        var pages = new FakePages();
        var list = new InfiniteList<int, int>(pages.Load, new() { Immediate = false, Debounce = TimeSpan.FromMilliseconds(50) });
        Assert.Empty(pages.Calls);

        var first = list.ParamsChangedAsync();
        var second = list.ParamsChangedAsync();
        await Task.WhenAll(first, Task.Delay(120, Xunit.TestContext.Current.CancellationToken));
        Assert.Single(pages.Calls);
        pages.Calls[0].Result.SetResult(new([1], default, false));
        await second;
        Assert.Equal([1], list.Items);
    }

    [Fact]
    public async Task 列表末尾_失败时重试_取完了没有更多()
    {
        var pages = new FakePages();
        var list = new InfiniteList<int, int>(pages.Load);
        var cut = Render<InfiniteListStatus>(p => p.Add(x => x.List, list));
        Assert.Equal("status", cut.Find("div").GetAttribute("role"));
        Assert.Equal("", cut.Find("div").TextContent.Trim());

        pages.Calls[0].Result.SetException(new InvalidOperationException("x"));
        await WaitUntil(() => list.Error is not null);
        cut.Render();
        Assert.Contains("加载失败，请重试", cut.Markup);
        cut.Find("button").Click();
        pages.Calls[1].Result.SetResult(new([1, 2], default, false));
        await WaitUntil(() => list.Loaded);
        cut.Render();
        Assert.Equal("没有更多了", cut.Find("div").TextContent.Trim());
    }

    static Task TriggerAsync(Task task) => Task.WhenAny(task, Task.Delay(10));

    static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++) await Task.Delay(5);
        Assert.True(condition());
    }
}
