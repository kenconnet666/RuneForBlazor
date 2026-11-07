using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Rune.UI;

/// <summary>组合框：输入框 + 过滤后的选项列表。用法见 Combobox.razor 顶部的说明。</summary>
/// <typeparam name="TItem">选项的数据：ListboxOption、字符串，或任意数据加 ToOption。</typeparam>
public partial class Combobox<TItem>
{
    Memo<(Attrs Control, string ListId, string PopoverId, bool Open, bool Editable, string Text), Attrs> inputManaged;
    Memo<(string Id, bool Open, bool Disabled), Attrs> shellManaged;
    Memo<(string ListId, string PopoverId, bool Open), Attrs> toggleManaged;
    Memo<string, Attrs> refocusInput;
    bool open;

    // 用户输入的过滤词：选中、用箭头或展开按钮打开时清空（显示全部选项）；输入框里的文字另算（Text）
    string query = "";

    // 上次同步到输入框的已选项（值与文字）：父级改了值、或选项后到时，输入框跟着显示它的文字
    (string? Value, string Label) synced;

    /// <summary>数据：ListboxOption、字符串，或任意数据加 ToOption；有 Load 时是第一次加载之前的选项（编辑页放上已选的那一项）。</summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>数据 → 选项（值、文字、不可用、分组……）；Items 是 ListboxOption 或字符串时可以不写。</summary>
    [Parameter] public Func<TItem, ListboxOption>? ToOption { get; set; }

    /// <summary>输入框里的文字（<c>@bind-InputValue</c>）：异步搜索时据此取数据。</summary>
    [Parameter] public string? InputValue { get; set; }

    /// <summary>输入框里的文字变了（用户输入，或选中后写入选项文字）。</summary>
    [Parameter] public EventCallback<string?> InputValueChanged { get; set; }

    /// <summary>受控的打开状态（<c>@bind-Open</c>）；不写时由用户操作决定。</summary>
    [Parameter] public bool? Open { get; set; }

    /// <summary>打开或关闭后。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>怎样算匹配（数据、输入）；默认显示文字包含输入（忽略大小写与重音，见 <see cref="UIConfig.Matches"/>）。</summary>
    [Parameter] public Func<TItem, string, bool>? Filter { get; set; }

    /// <summary>在本地按输入过滤，默认 true；由调用方按 InputValue 换 Items（异步搜索）时设为 false（有 Load 时本来就不在本地过滤）。</summary>
    [Parameter] public bool LocalFilter { get; set; } = true;

    /// <summary>远程选项：打开时与输入停下 300 ms 后按输入调用，结果代替 Items；只采用最后一次，被顶掉的请求取消。</summary>
    [Parameter] public OptionsLoader<TItem>? Load { get; set; }

    /// <summary>
    /// 分页的远程选项（代替 Load）：搜索词与第几页（从 1 起）→ 这一页；滚到列表末尾时取下一页，按选项的值去重，取下一页时骨架接在末尾。
    /// </summary>
    [Parameter] public PagedOptionsLoader<TItem>? LoadPage { get; set; }

    // 有远程选项时由服务端按输入过滤
    bool Remote => Load is not null || LoadPage is not null;
    // 分页且还有下一页时，列表滚到末尾取下一页
    EventCallback ReachEnd => LoadPage is null || !options.HasMore ? default : EventCallback.Factory.Create(this, options.LoadMore);

    /// <summary>允许输入不在选项里的文字：失焦时保留，Value 为 null、文字在 InputValue 里。</summary>
    [Parameter] public bool AllowCustomValue { get; set; }

    /// <summary>显示右侧的展开按钮，默认 true。</summary>
    [Parameter] public bool ShowToggle { get; set; } = true;

    /// <summary>没有可选的项时不弹出列表（不显示“没有匹配的选项”）。</summary>
    [Parameter] public bool HideEmpty { get; set; }

    /// <summary>正在取选项（自己管的异步搜索）：列表里换成骨架行。</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>弹出位置，默认下方、与起始边对齐。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>有内容时显示清空按钮。</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>前置图标。</summary>
    [Parameter] public IconData? Icon { get; set; }

    /// <summary>自定义选项内容：拿到原始数据、选项信息与是否已选。</summary>
    [Parameter] public RenderFragment<ListOptionState<TItem>>? ChildContent { get; set; }

    /// <summary>没有匹配的选项时列表里显示的内容（加载失败时换成相应的提示）。</summary>
    [Parameter] public RenderFragment? Empty { get; set; }

    /// <summary>清空按钮的名称（读屏读出）；不写时取语言包的“清空”。</summary>
    [Parameter] public string? ClearLabel { get; set; }

    /// <summary>前置图标的部件属性（交给 Icon）。</summary>
    [Parameter] public Attrs? PropsIcon { get; set; }

    /// <summary>清空按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsClear { get; set; }

    /// <summary>展开按钮的部件属性（交给 IconButton）。</summary>
    [Parameter] public Attrs? PropsChevron { get; set; }

    /// <summary>弹出层的部件属性。</summary>
    [Parameter] public Attrs? PropsPopover { get; set; }

    /// <summary>列表的部件属性（交给 ListboxView，可以写它的参数，如 PropsOption）。</summary>
    [Parameter] public Attrs? PropsListbox { get; set; }

    /// <summary>
    /// 值就是输入框里的文字（Autocomplete）：选一项等于把它的文字填进来，不显示展开按钮，没有建议时不弹出。
    /// 这时 Value 是文字，不再有“选中的选项”。
    /// </summary>
    protected virtual bool TextIsValue => false;

    Bindable<string?> inputValue { get => field ??= new(this, () => InputValue, () => InputValueChanged); }

    OptionSource<TItem> options { get => field ??= new(() => InvokeAsync(StateHasChanged)); }

    bool Editable => !IsDisabled && !IsReadOnly;

    bool ShowsToggle => ShowToggle && !TextIsValue;

    bool AllowsCustom => AllowCustomValue || TextIsValue;

    /// <summary>输入框里的文字。</summary>
    string Text => (TextIsValue ? CurrentValue : inputValue.Value) ?? "";

    // 选中的选项的值；TextIsValue 时没有
    string? SelectedValue => TextIsValue ? null : CurrentValue;

    IReadOnlyList<string> SelectedValues => SelectedValue is { } value ? [value] : [];

    string? SubmittedValue => TextIsValue ? (Text.Length > 0 ? Text : null) : CurrentValue;

    string ShellId => $"{ControlId}-shell";

    string PopoverId => $"{ControlId}-popover";

    string ListId => $"{ControlId}-listbox";

    string? AriaLabel => AdditionalAttributes?.TryGetValue("aria-label", out var label) == true ? label as string : null;

    IReadOnlyList<ListEntry<TItem>> AllEntries => options.All(Items, ToOption);

    // 列表里的选项：本地按输入过滤；有 Load、关掉本地过滤时由服务端过滤
    IReadOnlyList<ListEntry<TItem>> ShownEntries =>
        !Remote && LocalFilter ? options.Filter(AllEntries, query, Filter, Config) : AllEntries;

    string SelectedLabel => SelectedValue is { } value ? options.Chosen(AllEntries, [value])[0].Option.Label : "";

    string? EmptyText => options.EmptyText(Config.Lang, query);

    string? Results(int count) => OptionSource<TItem>.Results(Config.Lang, query, count);

    Attrs RefocusInput => refocusInput.Get(ControlId, static id => new Attrs { ["data-rune-refocus"] = id });

    Attrs ShellManaged => shellManaged.Get((ShellId, open, IsDisabled), static k => Attrs.Merge(ShellAttrs, new Attrs
    {
        Id = k.Id,
        ["data-state"] = k.Open ? "open" : "closed",
        ["data-disabled"] = k.Disabled ? true : null,
    }));

    // 输入框上由组件管理的属性：combobox 的角色与状态、值与输入事件、交给 JS 的键盘约定（输入框是列表的宿主，弹层由 data-rune-popover 指出）
    Attrs InputManaged => inputManaged.Get((ControlAttrs, ListId, PopoverId, open, Editable, Text), this, static (k, owner) => Attrs.Merge(k.Control, new Attrs
    {
        Role = "combobox",
        ["value"] = k.Text,
        ["autocomplete"] = "off",
        ["aria-autocomplete"] = "list",
        ["aria-expanded"] = k.Open ? "true" : "false",
        ["aria-controls"] = k.ListId,
        ["data-rune-list"] = k.Editable ? "text" : null,
        ["data-rune-popover"] = k.Editable ? k.PopoverId : null,
        ["data-rune-escape"] = k.Editable ? "clear" : null,
        ["oninput"] = EventCallback.Factory.Create<ChangeEventArgs>(owner, owner.TypedAsync),
        ["onblur"] = EventCallback.Factory.Create<FocusEventArgs>(owner, owner.CommitAsync),
        ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(owner, owner.CommandAsync),
    }));

    // 展开按钮：原生 popovertarget 切换；不进 Tab 顺序、按下不抢输入框的焦点（外壳的 data-rune-control），点完焦点回到输入框。
    // 交给 IconButton（经 MergeProps），箭头的旋转样式作为 Sx 交下去，与按钮自己的样式合成一个类
    Attrs ToggleManaged => toggleManaged.Get((ListId, PopoverId, open), this, static (k, owner) => new Attrs
    {
        Sx = InputStyles.Chevron,
        ["tabindex"] = "-1",
        ["popovertarget"] = k.PopoverId,
        ["aria-expanded"] = k.Open ? "true" : "false",
        ["aria-controls"] = k.ListId,
        ["data-rune-refocus"] = owner.ControlId,
    });

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        // 父级改了值、或选项后到（编辑页先拿到值、再拿到选项）：输入框跟着显示它的文字；用户正在输入时不打断
        if (TextIsValue || query.Length > 0) return;
        var current = (SelectedValue, SelectedLabel);
        if (current == synced) return;
        synced = current;
        if (!AllowsCustom || current.SelectedValue is not null) await SetTextAsync(current.SelectedLabel);
    }

    Task SetTextAsync(string text) =>
        Text == text ? Task.CompletedTask : TextIsValue ? SetValueAsync(text) : inputValue.SetAsync(text);

    // 用户输入（输入法组合结束后才到）：按输入过滤；JS 已经打开了弹层，并在 C# 渲染后高亮第一项
    async Task TypedAsync(ChangeEventArgs e)
    {
        var text = e.Value as string ?? "";
        query = text;
        await SetTextAsync(text);
        if (open) options.Request(Load, LoadPage, ToOption, text, debounce: true);
    }

    async Task PickAsync(string value)
    {
        if (!Editable) return;
        var label = AllEntries.FirstOrDefault(entry => entry.Option.Value == value).Option?.Label ?? value;
        query = "";
        if (TextIsValue)
        {
            await SetValueAsync(label);
            return;
        }
        await SetValueAsync(value);
        synced = (value, label);
        await SetTextAsync(label);
    }

    // 失焦：文字不是选项时恢复（或清空选择）；允许自由输入时文字与已选项不符就取消选择
    async Task CommitAsync()
    {
        query = "";
        if (TextIsValue || !Editable) return;
        if (AllowsCustom)
        {
            if (Text != SelectedLabel) await SetValueAsync(null);
            return;
        }
        if (Text.Length == 0) await SetValueAsync(null);
        else await SetTextAsync(SelectedLabel);
    }

    async Task ClearAsync()
    {
        query = "";
        await SetTextAsync("");
        if (!TextIsValue) await SetValueAsync(null);
    }

    // 展开按钮、↑↓ 打开时显示全部选项；关着时 Esc 清空
    void ShowAll() => query = "";

    Task CommandAsync(CommandEventArgs e)
    {
        switch (e.Name)
        {
            case "show-all":
                ShowAll();
                return Task.CompletedTask;
            case "clear":
                return ClearAsync();
            default:
                return Task.CompletedTask;
        }
    }

    async Task ToggledAsync(OverlayToggleEventArgs e)
    {
        if (open == e.Open) return;
        open = e.Open;
        if (open) options.Request(Load, LoadPage, ToOption, query, debounce: false);
        else options.Cancel();
        await OpenChanged.InvokeAsync(open);
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        base.OnDispose();
        options.Dispose();
    }
}
