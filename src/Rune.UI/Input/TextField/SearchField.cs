using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 搜索框（input type="search"，同 vue-tsx-pro 的 SearchField）：TextField 加上搜索图标、清空按钮、默认的名称与占位文字（“搜索”），
/// 以及“要搜索了”的时机——停手一会儿、按回车、清空。
/// <code>
/// &lt;SearchField @bind-Value="keyword" /&gt;                                   本地过滤：每敲一个字都用 keyword 过滤
/// &lt;SearchField @bind-Value="keyword" OnSearch="text =&gt; ReloadAsync(text)" /&gt;   请求：停手 300ms、回车、清空才发
/// </code>
/// <list type="bullet">
/// <item><description>值每敲一个字都更新（输入框里的草稿）；<see cref="OnSearch"/> 只在该查的时候调用，同一个词不重复调用。</description></item>
/// <item><description>回车、清空立即触发；打字等 <see cref="Delay"/>（默认 300ms，0 是每次输入都触发）。父级从外面改值（重置、恢复保存的条件）不触发。</description></item>
/// <item><description>其余参数同 TextField（未声明的落到 input 上）。下拉、穿梭框、树、表头筛选里的搜索框都是它。</description></item>
/// </list>
/// </summary>
public class SearchField : TextField
{
    static readonly Attrs SearchKeyboard = new() { ["enterkeyhint"] = "search" };

    Memo<string, Attrs> defaults;
    Attrs? flushOnCommit;
    CancellationTokenSource? pending;
    // 上一次交出去的词：一样的不再交
    string searched = "";
    // 自己输入的最新的词：父级从外面改了值时不是它，那次不触发搜索
    string typed = "";
    // 上一次设置参数时看到的值：值没变的重新渲染不算“外面改了值”
    string? seen;

    /// <summary>创建：图标、清空按钮、边输边更新是默认值，都可以用参数改掉。</summary>
    public SearchField()
    {
        Type = "search";
        Icon = BuiltInIcons.Search;
        Clearable = true;
        Immediate = true;
    }

    /// <summary>要搜索了：停手 <see cref="Delay"/> 毫秒、按回车、清空时调用（回车、清空立即）；和上一次相同的词不再调用。</summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>停手多久调用 <see cref="OnSearch"/>（毫秒），默认 300；0 是每次输入都调用。</summary>
    [Parameter] public int Delay { get; set; } = 300;

    /// <inheritdoc/>
    protected override Attrs? InputDefaultAttrs => defaults.Get(Config.Lang.SelectSearch, static label =>
        Attrs.Layer(SearchKeyboard, new Attrs { ["placeholder"] = label, ["aria-label"] = label }));

    // 回车提交输入框（change）：还在等停手的那次立即触发。不用 keydown：那样每次按键都要交给 C#
    /// <inheritdoc/>
    protected override Attrs? InputExtraManaged => flushOnCommit ??= new Attrs { ["onchange"] = (Func<ChangeEventArgs, Task>)(_ => SearchAsync()) };

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var value = CurrentValue ?? "";
        if (value == seen) return;
        seen = value;
        if (value == typed) return;
        // 外面改了值：草稿跟上，不触发搜索
        Cancel();
        typed = searched = value;
    }

    /// <inheritdoc/>
    protected override async Task InputAsync(string? text)
    {
        typed = text ?? "";
        await base.InputAsync(text);
        // 没人听 OnSearch（只按值过滤的搜索框）：不用计时
        if (!OnSearch.HasDelegate) return;
        Cancel();
        // 清空立即生效；打字等停手
        if (typed.Length == 0 || Delay <= 0) await SearchAsync();
        else _ = SearchLaterAsync((pending = new CancellationTokenSource()).Token);
    }

    async Task SearchLaterAsync(CancellationToken cancelled)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(Delay), TimeSource, cancelled);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        await InvokeAsync(SearchAsync);
    }

    async Task SearchAsync()
    {
        Cancel();
        if (typed == searched) return;
        searched = typed;
        await OnSearch.InvokeAsync(typed);
    }

    void Cancel()
    {
        pending?.Cancel();
        pending?.Dispose();
        pending = null;
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        Cancel();
        base.OnDispose();
    }
}
