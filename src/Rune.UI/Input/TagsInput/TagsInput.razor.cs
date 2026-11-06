using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Rune.UI;

/// <summary>标签输入：输入框里排着标签，回车或分隔符加入，退格删最后一个。用法见 TagsInput.razor 顶部的说明。</summary>
/// <typeparam name="TItem">建议的数据：ListboxOption、字符串，或任意数据加 ToOption。</typeparam>
public partial class TagsInput<TItem>
{
    /// <summary>默认的分隔符：英文、中文的逗号与分号。</summary>
    public static readonly IReadOnlyList<string> DefaultDelimiters = [",", "，", ";", "；"];

    Memo<(Attrs Control, string ListId, string PopoverId, bool Open, bool Editable, bool Suggestions, string Text, string Delimiters), Attrs> inputManaged;
    Memo<(string Id, bool Open, bool Disabled), Attrs> shellManaged;
    Memo<string, Attrs> refocusInput;
    bool open;

    /// <summary>建议：ListboxOption、字符串，或任意数据加 ToOption；不给时是纯自由输入。</summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>数据 → 选项；Items 是 ListboxOption 或字符串时可以不写。</summary>
    [Parameter] public Func<TItem, ListboxOption>? ToOption { get; set; }

    /// <summary>允许加入不在建议里的文字，默认 true。</summary>
    [Parameter] public bool AllowCustomValue { get; set; } = true;

    /// <summary>失焦时把输入框里还没确认的文字加成标签，默认 true（同 Mantine）；false 时文字留在框里。</summary>
    [Parameter] public bool AcceptOnBlur { get; set; } = true;

    /// <summary>把输入拆成标签的分隔符（单个字符），默认 <see cref="DefaultDelimiters"/>；回车与粘贴里的换行总是分隔。</summary>
    [Parameter] public IReadOnlyList<string> Delimiters { get; set; } = DefaultDelimiters;

    /// <summary>最多几个标签；到了上限不再加入。</summary>
    [Parameter] public int? Max { get; set; }

    /// <summary>输入框里的文字（<c>@bind-InputValue</c>）：异步取建议时据此搜索。</summary>
    [Parameter] public string? InputValue { get; set; }

    /// <summary>输入框里的文字变了。</summary>
    [Parameter] public EventCallback<string?> InputValueChanged { get; set; }

    /// <summary>受控的打开状态（<c>@bind-Open</c>）；不写时由用户操作决定。</summary>
    [Parameter] public bool? Open { get; set; }

    /// <summary>打开或关闭后。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>怎样算匹配（数据、输入）；默认显示文字包含输入（忽略大小写与重音）。</summary>
    [Parameter] public Func<TItem, string, bool>? Filter { get; set; }

    /// <summary>在本地按输入过滤建议，默认 true；异步取建议（按 InputValue 换 Items）时设为 false，这时建议列表一直在、等待时显示加载中。</summary>
    [Parameter] public bool LocalFilter { get; set; } = true;

    /// <summary>正在取建议：列表里换成骨架行。</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>弹出位置，默认下方、与起始边对齐。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>有标签时显示清空按钮。</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>前置图标。</summary>
    [Parameter] public IconData? Icon { get; set; }

    /// <summary>自定义建议的内容。</summary>
    [Parameter] public RenderFragment<ListOptionState<TItem>>? ChildContent { get; set; }

    /// <summary>清空按钮的名称（读屏读出）；不写时取语言包的“清空”。</summary>
    [Parameter] public string? ClearLabel { get; set; }

    /// <summary>每个标签的部件属性（交给 Tag），按值计算。</summary>
    [Parameter] public Func<string, Attrs?>? PropsTag { get; set; }

    /// <summary>前置图标的部件属性（交给 Icon）。</summary>
    [Parameter] public Attrs? PropsIcon { get; set; }

    /// <summary>清空按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsClear { get; set; }

    /// <summary>弹出层的部件属性。</summary>
    [Parameter] public Attrs? PropsPopover { get; set; }

    /// <summary>建议列表的部件属性（交给 ListboxView）。</summary>
    [Parameter] public Attrs? PropsListbox { get; set; }

    Bindable<string?> inputValue { get => field ??= new(this, () => InputValue, () => InputValueChanged); }

    OptionSource<TItem> options { get => field ??= new(() => InvokeAsync(StateHasChanged)); }

    IReadOnlyList<string> Tags => CurrentValue ?? [];

    string Text => inputValue.Value ?? "";

    // 过滤建议用的词
    string Query => Text.Trim();

    bool Editable => !IsDisabled && !IsReadOnly;

    // 有建议列表：给了建议，或异步取建议（结果可能还没回来）
    bool HasSuggestions => !LocalFilter || AllEntries.Count > 0;

    string ShellId => $"{ControlId}-shell";

    string PopoverId => $"{ControlId}-popover";

    string ListId => $"{ControlId}-listbox";

    string? AriaLabel => AdditionalAttributes?.TryGetValue("aria-label", out var label) == true ? label as string : null;

    IReadOnlyList<ListEntry<TItem>> AllEntries => options.All(Items, ToOption);

    IReadOnlyList<ListEntry<TItem>> ShownEntries => LocalFilter ? options.Filter(AllEntries, Query, Filter, Config) : AllEntries;

    string? Results(int count) => OptionSource<TItem>.Results(Config.Lang, Query, count);

    Attrs RefocusInput => refocusInput.Get(ControlId, static id => new Attrs { ["data-rune-refocus"] = id });

    // 外壳里的标签不是原生控件的一部分：不可用要写在外壳上，辅助技术才把它们一起当作不可用
    Attrs ShellManaged => shellManaged.Get((ShellId, open, IsDisabled), static k => Attrs.Merge(ShellAttrs, new Attrs
    {
        Id = k.Id,
        AriaDisabled = k.Disabled ? true : null,
        ["data-state"] = k.Open ? "open" : "closed",
        ["data-disabled"] = k.Disabled ? true : null,
    }));

    // 输入框：有建议时是 combobox 与列表的宿主；Enter、Backspace、粘贴转成命令（见 JS 初始化器的 data-rune-enter 等）
    Attrs InputManaged => inputManaged.Get(
        (ControlAttrs, ListId, PopoverId, open, Editable, HasSuggestions, Text, string.Concat(Delimiters)),
        this,
        static (k, owner) => Attrs.Merge(k.Control, new Attrs
        {
            Role = k.Suggestions ? "combobox" : null,
            ["value"] = k.Text,
            ["autocomplete"] = "off",
            ["aria-autocomplete"] = k.Suggestions ? "list" : null,
            ["aria-expanded"] = k.Suggestions ? (k.Open ? "true" : "false") : null,
            ["aria-controls"] = k.Suggestions ? k.ListId : null,
            ["data-rune-list"] = k.Suggestions && k.Editable ? "text" : null,
            ["data-rune-popover"] = k.Suggestions && k.Editable ? k.PopoverId : null,
            ["data-rune-enter"] = k.Editable ? "add" : null,
            ["data-rune-backspace"] = k.Editable ? "remove-last" : null,
            ["data-rune-paste"] = k.Editable ? "paste" : null,
            ["data-rune-delimiters"] = k.Editable ? k.Delimiters : null,
            ["oninput"] = EventCallback.Factory.Create<ChangeEventArgs>(owner, owner.TypedAsync),
            ["onblur"] = EventCallback.Factory.Create<FocusEventArgs>(owner, owner.BlurAsync),
            ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(owner, owner.CommandAsync),
        }));

    // 按分隔符、换行、制表符拆开
    string[] Split(string text) => text.Split([.. Delimiters, "\n", "\r", "\t"], StringSplitOptions.None);

    /// <summary>把一段文字变成值：是建议的值或文字时取建议的值，否则（允许时）原样；空的、不允许的为 null。</summary>
    string? Resolve(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0) return null;
        foreach (var entry in AllEntries)
            if (entry.Option.Value == trimmed || entry.Option.Label == trimmed) return entry.Option.Value;
        return AllowCustomValue ? trimmed : null;
    }

    /// <summary>加入几段文字；重复的、超出上限的不加；返回是否加入了。</summary>
    async Task<bool> AddAsync(IEnumerable<string> texts)
    {
        var next = Tags.ToList();
        foreach (var text in texts)
        {
            if (Resolve(text) is not { } tag || next.Contains(tag) || next.Count >= (Max ?? int.MaxValue)) continue;
            next.Add(tag);
        }
        if (next.Count == Tags.Count) return false;
        await SetValueAsync(next);
        return true;
    }

    Task RemoveAsync(string tag) => SetValueAsync([.. Tags.Where(item => item != tag)]);

    // 输入了分隔符：分隔符前的部分加成标签，剩下的留在输入框里
    async Task TypedAsync(ChangeEventArgs e)
    {
        var parts = Split(e.Value as string ?? "");
        if (parts.Length > 1) await AddAsync(parts[..^1]);
        await inputValue.SetAsync(parts[^1]);
    }

    // 点选（或 Enter 选中高亮项）：加入或移除那一项，输入框清空、焦点留在输入框里
    async Task PickAsync(string value)
    {
        if (!Editable) return;
        if (ListState.Toggle(Tags, value, SelectionMode.Multiple, required: false) is { } next
            && (next.Count < Tags.Count || next.Count <= (Max ?? int.MaxValue)))
            await SetValueAsync(next);
        await inputValue.SetAsync("");
    }

    async Task BlurAsync()
    {
        if (AcceptOnBlur && Editable && Query.Length > 0 && await AddAsync([Text])) await inputValue.SetAsync("");
    }

    async Task ClearAsync() => await SetValueAsync([]);

    async Task CommandAsync(CommandEventArgs e)
    {
        if (!Editable) return;
        switch (e.Name)
        {
            case "add":
                if (await AddAsync([Text])) await inputValue.SetAsync("");
                break;
            case "remove-last" when Tags.Count > 0:
                await RemoveAsync(Tags[^1]);
                break;
            case "paste" when e.Text is { } text:
                await AddAsync(Split(text));
                break;
        }
    }

    async Task ToggledAsync(OverlayToggleEventArgs e)
    {
        if (open == e.Open) return;
        open = e.Open;
        await OpenChanged.InvokeAsync(open);
    }
}
