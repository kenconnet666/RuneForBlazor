using System.Collections;
using Microsoft.AspNetCore.Components.Web;
using Rune.Styles;

namespace Rune;

/// <summary>
/// 一层元素属性：常用属性有类型与说明，其余写索引器；样式写 <see cref="Sx"/>。
/// 可以直接交给 <c>@attributes</c> 展开，也可以几层交给 <see cref="Merge"/> 合并：
/// <code>
/// &lt;button @attributes="Attrs.Merge(s.Trigger, PropsTrigger, new() { AriaExpanded = open, OnClick = Toggle })"&gt;
/// </code>
/// </summary>
/// <remarks>只在对象初始化器里赋值，创建后不再修改。</remarks>
public sealed partial class Attrs : IEnumerable<KeyValuePair<string, object>>
{
    /// <summary>空的一层。</summary>
    public static readonly Attrs Empty = new();

    // 属性表：没有属性时不创建；自己创建的按 HTML 的规则不区分大小写。
    // From 直接引用组件收到的 AdditionalAttributes（只读，不复制），所以类型是只读接口
    IReadOnlyDictionary<string, object>? values;

    /// <summary>样式，展开时换成 class；合并时各层的样式按层的顺序叠加。</summary>
    public Sx? Sx { get; init; }

    /// <summary>元素 id。</summary>
    public string? Id { get => Get<string>("id"); init => Set("id", value); }

    /// <summary>额外的类名（宿主的全局 CSS、工具类）；组件自己的样式写 <see cref="Sx"/>。合并时各层依次拼接。</summary>
    public string? Class { get => Get<string>("class"); init => Set("class", value); }

    /// <summary>元素的 style 属性（行内样式文本）；合并时各层依次拼接。</summary>
    public string? InlineStyle { get => Get<string>("style"); init => AppendStyle(value); }

    /// <summary>元素上的 CSS 变量（一直在变的值，见 <see cref="CssVar{T}"/>）：<c>Vars = [FillWidth.Set("40%")]</c>。写进 style 属性。</summary>
    public IReadOnlyList<CssVarValue>? Vars
    {
        get => null;
        init
        {
            if (value is { Count: > 0 }) AppendStyle(string.Join(";", value));
        }
    }

    // InlineStyle 与 Vars 都写 style 属性：在初始化器里谁先谁后都不互相覆盖
    void AppendStyle(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Set("style", Get<string>("style") is { } existing ? $"{existing};{text}" : text);
    }

    /// <summary>悬停提示。</summary>
    public string? Title { get => Get<string>("title"); init => Set("title", value); }

    /// <summary>无障碍角色。</summary>
    public string? Role { get => Get<string>("role"); init => Set("role", value); }

    /// <summary>Tab 键顺序：0 可聚焦，-1 只能用代码聚焦。</summary>
    public int? TabIndex { get => Get<int?>("tabindex"); init => Set("tabindex", value); }

    /// <summary>隐藏元素（HTML 的 hidden 属性）。</summary>
    public bool? Hidden { get => Get<bool?>("hidden"); init => Set("hidden", value); }

    /// <summary>无障碍名称（没有可见文字时必写，如图标按钮）。</summary>
    public string? AriaLabel { get => Get<string>("aria-label"); init => Set("aria-label", value); }

    /// <summary>用其他元素的文字作为名称：写那些元素的 id。</summary>
    public string? AriaLabelledBy { get => Get<string>("aria-labelledby"); init => Set("aria-labelledby", value); }

    /// <summary>用其他元素的文字作为说明：写那些元素的 id。</summary>
    public string? AriaDescribedBy { get => Get<string>("aria-describedby"); init => Set("aria-describedby", value); }

    /// <summary>这个元素控制的元素的 id（展开的面板、弹层）。</summary>
    public string? AriaControls { get => Get<string>("aria-controls"); init => Set("aria-controls", value); }

    /// <summary>是否展开（触发器）。</summary>
    public bool? AriaExpanded { get => GetFlag("aria-expanded"); init => SetFlag("aria-expanded", value); }

    /// <summary>是否选中（标签页、选项）。</summary>
    public bool? AriaSelected { get => GetFlag("aria-selected"); init => SetFlag("aria-selected", value); }

    /// <summary>是否按下（切换按钮）。</summary>
    public bool? AriaPressed { get => GetFlag("aria-pressed"); init => SetFlag("aria-pressed", value); }

    /// <summary>是否不可用（仍可聚焦）。</summary>
    public bool? AriaDisabled { get => GetFlag("aria-disabled"); init => SetFlag("aria-disabled", value); }

    /// <summary>是否对读屏隐藏（装饰性图标）。</summary>
    public bool? AriaHidden { get => GetFlag("aria-hidden"); init => SetFlag("aria-hidden", value); }

    /// <summary>点击。合并时各层的处理器按层的顺序依次执行。</summary>
    public Func<MouseEventArgs, Task>? OnClick { get => Get<Func<MouseEventArgs, Task>>("onclick"); init => Set("onclick", value); }

    /// <summary>按键。</summary>
    public Func<KeyboardEventArgs, Task>? OnKeyDown { get => Get<Func<KeyboardEventArgs, Task>>("onkeydown"); init => Set("onkeydown", value); }

    /// <summary>获得焦点。</summary>
    public Func<FocusEventArgs, Task>? OnFocus { get => Get<Func<FocusEventArgs, Task>>("onfocus"); init => Set("onfocus", value); }

    /// <summary>失去焦点。</summary>
    public Func<FocusEventArgs, Task>? OnBlur { get => Get<Func<FocusEventArgs, Task>>("onblur"); init => Set("onblur", value); }

    /// <summary>其他属性：<c>["data-state"] = "open"</c>、<c>["onpointerdown"] = handler</c>。值为 null 时不写。</summary>
    public object? this[string name]
    {
        get => values?.GetValueOrDefault(name);
        init => Set(name, value);
    }

    /// <summary>
    /// 把组件收到的 <c>AdditionalAttributes</c> 与 <c>Sx</c> 包成一层（<see cref="RuneComponent"/> 的 Rest 就是它）。
    /// </summary>
    public static Attrs From(IReadOnlyDictionary<string, object>? attributes, Sx? sx)
    {
        if (attributes is null || attributes.Count == 0) return sx is null ? Empty : new Attrs { Sx = sx };
        // 每次渲染都会包一次：直接引用，不复制字典
        return new Attrs { Sx = sx, values = attributes };
    }

    /// <summary>只有样式的一层：<c>Attrs.Merge(ButtonStyles.Root, Rest)</c>。</summary>
    public static implicit operator Attrs(Style style) => new() { Sx = style };

    /// <summary>
    /// 展开成元素属性。有 <see cref="Sx"/> 时算出类名并与 class 合在一起（合并过的结果里已经没有 Sx）。
    /// </summary>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        var className = Sx is null ? null : CompileClass(Sx);
        if (className is not null)
        {
            yield return new("class", values?.GetValueOrDefault("class") is string extra ? $"{extra} {className}" : className);
        }
        if (values is null) yield break;
        foreach (var pair in values)
            if (className is null || !pair.Key.Equals("class", StringComparison.OrdinalIgnoreCase)) yield return pair;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    T? Get<T>(string name) => values is not null && values.TryGetValue(name, out var value) && value is T typed ? typed : default;

    void Set(string name, object? value)
    {
        if (value is null) return;
        // 只在对象初始化器里调用，这时 values 要么为空、要么是这个对象自己刚建的字典（From 包装的字典不会走到这里）
        var own = values as Dictionary<string, object> ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        own[name] = value;
        values = own;
    }

    // aria 的布尔属性要写成字符串 "true" / "false"：Blazor 会把 false 的布尔值当成“不写这个属性”
    bool? GetFlag(string name) => Get<string>(name) is { } text ? text == "true" : null;

    void SetFlag(string name, bool? value) => Set(name, value is null ? null : value.Value ? "true" : "false");
}
