using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>下拉选择：触发框 + 弹出的选项列表。用法见 Select.razor 顶部的说明。</summary>
/// <typeparam name="TItem">选项的数据：ListboxOption、字符串，或任意数据加 ToOption。</typeparam>
public partial class Select<TItem>
{
    Memo<(IReadOnlyList<ListEntry<TItem>> Filtered, IReadOnlyList<ListEntry<TItem>> All, string Query, bool Creatable), ShownOptions> shown;
    Memo<(Attrs Control, bool Open, bool Locked, bool ReadOnly, bool Multiple, bool Tagged), Attrs> triggerManaged;
    Memo<(string Id, bool Open, bool Disabled), Attrs> shellManaged;
    Memo<(string ListId, bool Multiple), Attrs> searchManaged;
    Memo<string, Attrs> searchDefaults, refocusTrigger;
    EventCallback<string>? removeCallback;
    bool open;

    /// <summary>数据：ListboxOption、字符串，或任意数据加 ToOption；有 Load 时是第一次加载之前的选项（编辑页放上已选的那一项）。</summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>数据 → 选项（值、文字、不可用、分组……）；Items 是 ListboxOption 或字符串时可以不写。</summary>
    [Parameter] public Func<TItem, ListboxOption>? ToOption { get; set; }

    /// <summary>单选（默认，值是 Value）或多选（值是 Values）。</summary>
    [Parameter] public SelectionMode SelectionMode { get; set; }

    /// <summary>多选时选中的值（<c>@bind-Values</c>）。</summary>
    [Parameter] public IReadOnlyList<string>? Values { get; set; }

    /// <summary>多选时选择变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> ValuesChanged { get; set; }

    /// <summary>没有选择时显示的文字；不写时取语言包（请选择）。</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>弹出位置，默认下方、与起始边对齐；空间不够时翻到另一侧。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>受控的打开状态（<c>@bind-Open</c>）；不写时由用户操作决定。</summary>
    [Parameter] public bool? Open { get; set; }

    /// <summary>打开或关闭后。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>有选择时显示清空按钮。</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>弹出层顶部带搜索框，输入过滤选项：选项多（供应商、人员、件号）时用。</summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>搜索时怎样算匹配（数据、搜索词）；默认显示文字包含输入（忽略大小写与重音，见 <see cref="UIConfig.Matches"/>）。</summary>
    [Parameter] public Func<TItem, string, bool>? Filter { get; set; }

    /// <summary>在本地按搜索词过滤，默认 true；自己按 <see cref="Search"/> 请求、由服务端过滤时设为 false（有 Load 时本来就不在本地过滤）。</summary>
    [Parameter] public bool LocalFilter { get; set; } = true;

    /// <summary>搜索框里的文字（<c>@bind-Search</c>）：自己发请求时拿到它；关闭时清空。</summary>
    [Parameter] public string? Search { get; set; }

    /// <summary>搜索框里的文字变了。</summary>
    [Parameter] public EventCallback<string?> SearchChanged { get; set; }

    /// <summary>远程选项：打开时与搜索词停下 300 ms 后调用，结果代替 Items；只采用最后一次，被顶掉的请求取消。</summary>
    [Parameter] public OptionsLoader<TItem>? Load { get; set; }

    /// <summary>
    /// 可以新建选项（自带搜索框）：输入的文字没有对应的选项时，列表末尾多一项“新建“…””，选中后值就是这段文字。
    /// 在 <see cref="OnCreate"/> 里把它加进 Items（或存到服务端）；不加时触发框里照样显示这段文字。
    /// </summary>
    [Parameter] public bool Creatable { get; set; }

    /// <summary>新建了一项（Creatable 时）：参数是输入的文字，也就是选中的值。</summary>
    [Parameter] public EventCallback<string> OnCreate { get; set; }

    /// <summary>多选时触发框里最多显示几个标签，其余收进“+N”；<see cref="TagLimit.Responsive"/> 时排成一行、按宽度放。</summary>
    [Parameter] public TagLimit? MaxTagCount { get; set; }

    /// <summary>前置图标。</summary>
    [Parameter] public IconData? Icon { get; set; }

    /// <summary>自定义选项内容：拿到原始数据、选项信息与是否已选。</summary>
    [Parameter] public RenderFragment<ListOptionState<TItem>>? ChildContent { get; set; }

    /// <summary>自定义触发框里显示的内容（已选的数据）；没有选择时显示占位文字。默认单选显示文字、多选显示标签。</summary>
    [Parameter] public RenderFragment<IReadOnlyList<TItem>>? RenderValue { get; set; }

    /// <summary>正在取选项（自己管的异步加载）：列表里换成骨架行。</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>没有选项时列表里显示的内容（搜索没有匹配、加载失败时换成相应的提示）。</summary>
    [Parameter] public RenderFragment? Empty { get; set; }

    /// <summary>清空按钮的名称（读屏读出）；不写时取语言包的“清空”。</summary>
    [Parameter] public string? ClearLabel { get; set; }

    /// <summary>前置图标的部件属性（交给 Icon）。</summary>
    [Parameter] public Attrs? PropsIcon { get; set; }

    /// <summary>已选内容那一层的部件属性。</summary>
    [Parameter] public Attrs? PropsValue { get; set; }

    /// <summary>多选时每个标签的部件属性（交给 Tag）。</summary>
    [Parameter] public Attrs? PropsTag { get; set; }

    /// <summary>多选时那排标签的部件属性（交给 SelectedTags）。</summary>
    [Parameter] public Attrs? PropsTags { get; set; }

    /// <summary>清空按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsClear { get; set; }

    /// <summary>下拉箭头的部件属性（交给 Icon）。</summary>
    [Parameter] public Attrs? PropsChevron { get; set; }

    /// <summary>弹出层的部件属性。</summary>
    [Parameter] public Attrs? PropsPopover { get; set; }

    /// <summary>列表的部件属性（交给 ListboxView，可以写它的参数，如 PropsOption）。</summary>
    [Parameter] public Attrs? PropsListbox { get; set; }

    /// <summary>搜索框的部件属性（交给 TextField）。</summary>
    [Parameter] public Attrs? PropsSearch { get; set; }

    // 没给 Values 时用同一个空数组：每次新建的空列表会被当成父级换了值
    Bindable<IReadOnlyList<string>> values { get => field ??= new(this, () => Values ?? Array.Empty<string>(), () => ValuesChanged); }

    Bindable<string?> search { get => field ??= new(this, () => Search, () => SearchChanged); }

    bool Multiple => SelectionMode == SelectionMode.Multiple;

    // 不能打开、不能改：不可用，或只读（只读时仍能聚焦、看到已选的内容）
    bool Locked => IsDisabled || IsReadOnly;

    bool WithSearch => Searchable || Creatable;

    string SearchText => search.Value ?? "";

    string ShellId => $"{ControlId}-shell";

    string PopoverId => $"{ControlId}-popover";

    string ListId => $"{ControlId}-listbox";

    string? AriaLabel => AdditionalAttributes?.TryGetValue("aria-label", out var label) == true ? label as string : null;

    IReadOnlyList<string> Selected => Multiple ? values.Value : CurrentValue is { } value ? [value] : [];

    // 选项数据：本地或远程、按搜索词过滤、已选项的文字
    OptionSource<TItem> options { get => field ??= new(() => InvokeAsync(StateHasChanged)); }

    IReadOnlyList<ListEntry<TItem>> AllEntries => options.All(Items, ToOption);

    // 列表里显示的选项：搜索词过滤后的（有 Load、关掉本地过滤时由服务端过滤），Creatable 时末尾可能多一项“新建”
    ShownOptions Shown
    {
        get
        {
            var all = AllEntries;
            var filtered = WithSearch && Load is null && LocalFilter ? options.Filter(all, SearchText, Filter, Config) : all;
            return shown.Get((filtered, all, SearchText, Creatable), this, static (k, owner) => owner.Compute(k.Filtered, k.All, k.Query, k.Creatable));
        }
    }

    ShownOptions Compute(IReadOnlyList<ListEntry<TItem>> entries, IReadOnlyList<ListEntry<TItem>> all, string query, bool creatable)
    {
        var text = query.Trim();
        if (!creatable || text.Length == 0) return new(entries, null);
        // 已经有同值或同名（忽略大小写）的选项时不新建
        if (all.Any(e => e.Option.Value == text || string.Equals(e.Option.Label, text, StringComparison.CurrentCultureIgnoreCase))) return new(entries, null);
        var creation = new ListboxOption(text, Config.Lang.SelectCreate(text), Icon: BuiltInIcons.Plus);
        return new([.. entries, new ListEntry<TItem>(creation, default!)], creation);
    }

    List<ChosenOption<TItem>> Chosen(IReadOnlyList<string> selected) => options.Chosen(AllEntries, selected);

    string? Results(int count) => WithSearch ? OptionSource<TItem>.Results(Config.Lang, SearchText, count) : null;

    string? EmptyText => options.EmptyText(Config.Lang, SearchText);

    EventCallback<string> RemoveCallback => removeCallback ??= EventCallback.Factory.Create<string>(this, RemoveAsync);

    Attrs RefocusTrigger => refocusTrigger.Get(ControlId, static id => new Attrs { ["data-rune-refocus"] = id });

    // 外壳：点在空白处时 JS 把焦点交给触发按钮并切换打开（data-rune-control）；箭头按 data-state 转向
    Attrs ShellManaged => shellManaged.Get((ShellId, open, IsDisabled), static k => Attrs.Merge(ShellAttrs, new Attrs
    {
        Id = k.Id,
        ["data-state"] = k.Open ? "open" : "closed",
        ["data-disabled"] = k.Disabled ? true : null,
    }));

    // 触发按钮上由组件管理的属性：combobox 的角色与状态、打开弹层（popovertarget）、交给 JS 的键盘约定
    Attrs TriggerManaged(bool tagged) => triggerManaged.Get((ControlAttrs, open, Locked, IsReadOnly, Multiple, tagged), this, static (k, owner) => new Attrs
    {
        Sx = k.Tagged ? SelectStyles.TriggerBehindTags : (Sx?)null,
        Id = k.Control.Id,
        Role = "combobox",
        ["type"] = "button",
        ["disabled"] = k.Control["disabled"],
        ["popovertarget"] = k.Locked ? null : owner.PopoverId,
        ["aria-haspopup"] = "listbox",
        ["aria-expanded"] = k.Open ? "true" : "false",
        ["aria-controls"] = owner.ListId,
        ["aria-invalid"] = k.Control["aria-invalid"],
        ["aria-required"] = k.Control["aria-required"],
        ["aria-readonly"] = k.ReadOnly ? "true" : null,
        AriaDescribedBy = k.Control.AriaDescribedBy,
        // 焦点在触发按钮上：空格选中、字母类型搜索（关闭时单选直接选中）
        ["data-rune-list"] = k.Locked ? null : "space typeahead",
        ["data-rune-backspace"] = k.Multiple && !k.Locked ? "remove-last" : null,
        ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(owner, owner.CommandAsync),
    });

    Attrs SearchDefaults => searchDefaults.Get(Config.Lang.SelectSearch, static label => new Attrs
    {
        Sx = SearchWidth,
        ["placeholder"] = label,
        ["aria-label"] = label,
    });

    // 搜索框也是列表的键盘宿主：↑↓ 移动、Enter 选中（Home / End、空格、字母留给输入）；多选时空的搜索框里按 Backspace 移除最后一项
    Attrs SearchManaged => searchManaged.Get((ListId, Multiple), this, static (k, owner) => new Attrs
    {
        ["aria-controls"] = k.ListId,
        ["autocomplete"] = "off",
        ["data-rune-list"] = "text",
        ["data-rune-backspace"] = k.Multiple ? "remove-last" : null,
        ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(owner, owner.CommandAsync),
    });

    static Style SearchWidth => Css.Define(static () => new Style { Width = CssLength.Percent(100) });

    async Task PickAsync(string value)
    {
        if (Locked) return;
        // 单选时再选已选的一项不取消（同原生 select）；要清空用清空按钮
        if (ListState.Toggle(Selected, value, SelectionMode, required: !Multiple) is not { } next) return;
        if (Shown.Creation?.Value == value)
        {
            await OnCreate.InvokeAsync(value);
            await SetSearchAsync("");
        }
        if (Multiple) await values.SetAsync(next);
        else await SetValueAsync(next.Count == 0 ? null : next[0]);
    }

    Task RemoveAsync(string value) => values.SetAsync([.. Selected.Where(v => v != value)]);

    Task ClearAsync() => Multiple ? values.SetAsync([]) : SetValueAsync(null);

    Task CommandAsync(CommandEventArgs e) =>
        e.Name == "remove-last" && Multiple && !Locked && Selected.Count > 0 ? RemoveAsync(Selected[^1]) : Task.CompletedTask;

    async Task SetSearchAsync(string? text)
    {
        await search.SetAsync(text);
        if (open) options.Request(Load, text ?? "", debounce: true);
    }

    async Task ToggledAsync(OverlayToggleEventArgs e)
    {
        if (open == e.Open) return;
        open = e.Open;
        if (open)
        {
            // 打开就按当前搜索词请求，不等防抖
            options.Request(Load, SearchText, debounce: false);
        }
        else
        {
            options.Cancel();
            // 关闭时清空搜索词：下次打开是全部选项
            if (SearchText.Length > 0) await search.SetAsync("");
        }
        await OpenChanged.InvokeAsync(open);
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        base.OnDispose();
        options.Dispose();
    }

    /// <summary>列表里显示的选项与其中“新建”的那一项（没有时为 null）。</summary>
    sealed record ShownOptions(IReadOnlyList<ListEntry<TItem>> Entries, ListboxOption? Creation);

}
