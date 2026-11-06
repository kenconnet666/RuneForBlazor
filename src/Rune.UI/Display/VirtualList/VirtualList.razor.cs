using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>虚拟列表跟着谁滚动。</summary>
public enum VirtualScroll
{
    /// <summary>组件自己是滚动容器（默认）：要给它高度（横向时给宽度）。</summary>
    Container,

    /// <summary>随页面滚动：列表按总大小撑开，放在页面的普通流里。</summary>
    Window,
}

/// <summary>滚动到某一项时，它停在视口的什么位置。</summary>
public enum ScrollAlign
{
    /// <summary>已经完整可见就不动，否则就近对齐开头或末尾（默认）。</summary>
    Auto,

    /// <summary>对齐视口开头。</summary>
    Start,

    /// <summary>居中。</summary>
    Center,

    /// <summary>对齐视口末尾。</summary>
    End,
}

/// <summary>可见范围（不含 overscan），两端都包含。</summary>
/// <param name="StartIndex">第一个可见项。</param>
/// <param name="EndIndex">最后一个可见项。</param>
public readonly record struct VisibleRange(int StartIndex, int EndIndex);

/// <summary>虚拟列表：只渲染看得见的几十项，行高渲染后自动测量（用法见 VirtualList.razor 顶部）。</summary>
/// <typeparam name="TItem">一项的数据类型。</typeparam>
public partial class VirtualList<TItem>
{
    // 加载中显示几行骨架：没有数据时铺几行，加载更多时接在末尾几行
    const int SkeletonRowCount = 5;
    const int MoreSkeletonRows = 2;

    static readonly Attrs RootLayer = new() { Sx = Css.Define(static () => new Style { OverflowAnchor = OverflowAnchor.None }) };
    // 容器模式：列表元素是 ScrollArea 的视口；自己调整滚动位置，不要浏览器再按锚点调整一次
    static readonly Attrs ContainerLayer = new() { Sx = Rune.Styles.Sx.Of(ContainerStyle, SharedStyles.FocusRing) };
    // 横向：视口的固有宽度不算内容（列表的总宽有几万像素），否则按内容排版的祖先（表格、auto 的网格列）会被一路撑开
    static readonly Attrs HorizontalContainerLayer = new()
    {
        Sx = Rune.Styles.Sx.Of(ContainerStyle, SharedStyles.FocusRing, Css.Define(static () => new Style { Contain = Contain.InlineSize })),
    };
    static readonly Attrs ListLayer = new() { Sx = Css.Define(static () => new Style { Position = Position.Relative, OverflowAnchor = OverflowAnchor.None }) };
    static readonly Attrs VerticalItemLayer = new() { Sx = Rune.Styles.Sx.Of(ItemBase, Css.Define(static () => new Style { Width = CssLength.Percent(100) })) };
    // 横向的项按内容的高度（视口里还隔着 ScrollArea 的内容层，百分比高度算不出来）
    static readonly Attrs HorizontalItemLayer = new() { Sx = ItemBase };
    static readonly Attrs SkeletonRowLayer = new() { Sx = Css.Define(static () => new Style { PaddingBlock = Space.Xs }) };
    static readonly Attrs SkeletonLayer = new() { Sx = Css.Define(static () => new Style { Display = Display.Block, Width = CssLength.Percent(100) }) };

    static Style ContainerStyle => Css.Define(static () => new Style { OverscrollBehavior = OverscrollBehavior.Contain, OverflowAnchor = OverflowAnchor.None });

    static Style ItemBase => Css.Define(static () => new Style { Position = Position.Absolute, Top = 0, Left = 0, OverflowAnchor = OverflowAnchor.None });

    // JS 报告的要渲染的那一段；还没报告时是开头的 InitialCount 项
    (int From, int To)? reported;
    VisibleRange? visible;
    (int Index, double Offset, ScrollAlign Align, bool ByOffset, int Sequence) scrollRequest;

    DelayedLoading? delayedLoading;
    DelayedLoading indicator => delayedLoading ??= new(() => InvokeAsync(StateHasChanged));

    /// <summary>数据。</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>渲染一项（Context 是这一项）。</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>每项的 key，默认用序号；数据会重排、增删时一定要给，测量结果与组件状态才对得上。</summary>
    [Parameter] public Func<TItem, object>? GetKey { get; set; }

    /// <summary>每项的估计大小（px，横向时是宽度），渲染后以实际为准；默认 40。</summary>
    [Parameter] public float EstimateSize { get; set; } = 40;

    /// <summary>可见范围前后各多渲染几项，默认 4。</summary>
    [Parameter] public int Overscan { get; set; } = 4;

    /// <summary>跟着谁滚动，默认组件自己是滚动容器。</summary>
    [Parameter] public VirtualScroll Scroll { get; set; }

    /// <summary>横向排列与滚动。</summary>
    [Parameter] public bool Horizontal { get; set; }

    /// <summary>静态渲染（没有交互时）与交互接管前先输出前多少项，默认 30。</summary>
    [Parameter] public int InitialCount { get; set; } = 30;

    /// <summary>没有数据时显示的内容。</summary>
    [Parameter] public RenderFragment? Empty { get; set; }

    /// <summary>加载中：没有数据时铺几行骨架，已有数据（加载更多）时骨架接在末尾；带 aria-busy。</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>可见范围变化（不含 overscan）：接近末尾时加载更多。</summary>
    [Parameter] public EventCallback<VisibleRange> OnRangeChange { get; set; }

    /// <summary>列表层（撑开总大小、各项都在它里面）的部件属性。</summary>
    [Parameter] public Attrs? PropsList { get; set; }

    /// <summary>每一项外层（role="listitem"）的部件属性。</summary>
    [Parameter] public Attrs? PropsItem { get; set; }

    /// <summary>按项计算的每一项外层的部件属性（数据、序号；写了时代替 <see cref="PropsItem"/>）。</summary>
    [Parameter] public Func<TItem, int, Attrs?>? PropsItemFor { get; set; }

    /// <summary>加载中占位行的部件属性（交给 Skeleton）。</summary>
    [Parameter] public Attrs? PropsSkeleton { get; set; }

    /// <summary>当前的可见范围（JS 报告过之后才有）。</summary>
    public VisibleRange? Visible => visible;

    // 交互模式下才虚拟滚动（JS 排位置）；静态 SSR、预渲染时在普通流里输出开头几项
    bool Virtual => RendererInfo.IsInteractive;

    (int From, int To) RenderedRange
    {
        get
        {
            var last = Items.Count - 1;
            if (Virtual && reported is { } range) return (Math.Min(range.From, last), Math.Min(range.To, last));
            return (0, Math.Min(InitialCount, Items.Count) - 1);
        }
    }

    // 骨架行比一项略矮：上下留出间隔
    Attrs SkeletonHeight => new() { InlineStyle = $"height:{Math.Max(8, EstimateSize - 8).ToString(CultureInfo.InvariantCulture)}px" };

    // 外层：交给 JS 的选项与滚动请求、角色与名称
    Attrs RootManaged => new()
    {
        // 没有数据（空状态、首次加载的骨架）时不是列表；仍给 group 角色，写在外层的 aria-label 才有效
        Role = Items.Count > 0 ? "list" : "group",
        ["aria-busy"] = Loading ? "true" : null,
        // 容器模式下可以用键盘滚动
        ["tabindex"] = Scroll == VirtualScroll.Container ? "0" : null,
        ["data-rune-virtual"] = Virtual
            ? FormattableString.Invariant($$"""{"count":{{Items.Count}},"estimate":{{EstimateSize}},"overscan":{{Overscan}},"horizontal":{{(Horizontal ? "true" : "false")}},"window":{{(Scroll == VirtualScroll.Window ? "true" : "false")}}}""")
            : null,
        ["data-rune-virtual-scroll"] = scrollRequest.Sequence == 0 ? null : ScrollRequestText,
        ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(this, OnCommandAsync),
    };

    string ScrollRequestText => scrollRequest.ByOffset
        ? FormattableString.Invariant($"offset:{scrollRequest.Offset}:auto:{scrollRequest.Sequence}")
        : FormattableString.Invariant($"index:{scrollRequest.Index}:{scrollRequest.Align.ToString().ToLowerInvariant()}:{scrollRequest.Sequence}");

    /// <summary>滚动到第 index 项（从 0 开始）；align 默认已经完整可见就不动。</summary>
    public void ScrollToIndex(int index, ScrollAlign align = ScrollAlign.Auto)
    {
        scrollRequest = (index, 0, align, false, scrollRequest.Sequence + 1);
        StateHasChanged();
    }

    /// <summary>滚动到相对列表开头的位置（px）。</summary>
    public void ScrollToOffset(double offset)
    {
        scrollRequest = (0, offset, ScrollAlign.Auto, true, scrollRequest.Sequence + 1);
        StateHasChanged();
    }

    /// <inheritdoc/>
    protected override void OnParametersSet() => indicator.Update(Loading);

    /// <inheritdoc/>
    protected override void OnDispose() => delayedLoading?.Dispose();

    // JS 报告要渲染的那一段与可见范围：{第一项}:{最后一项}:{第一个可见}:{最后一个可见}
    async Task OnCommandAsync(CommandEventArgs e)
    {
        if (e.Name != "range" || e.Text?.Split(':') is not [var from, var to, var start, var end]) return;
        reported = (Parse(from), Parse(to));
        var range = new VisibleRange(Parse(start), Parse(end));
        if (visible == range) return;
        visible = range;
        await OnRangeChange.InvokeAsync(range);

        static int Parse(string text) => int.Parse(text, CultureInfo.InvariantCulture);
    }
}
