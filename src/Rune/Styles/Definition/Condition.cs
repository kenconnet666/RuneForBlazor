namespace Rune.Styles;

/// <summary>
/// 样式条件：伪类、伪元素、属性选择器或 @ 规则。写在样式的索引器里：<c>[When.Hover] = new() { … }</c>，可以嵌套。
/// </summary>
/// <param name="Key">选择器片段（<c>:hover</c>、<c>::before</c>、<c>:is([data-open])</c>）或 @ 规则（<c>@media (…)</c>）。</param>
public readonly record struct Condition(string Key)
{
    /// <summary>是 @ 规则（包在规则外面），而不是接在类名后面的选择器片段。</summary>
    internal bool IsAtRule => Key.StartsWith('@');
}

/// <summary>
/// 状态条件。状态写在元素的 aria / data 属性上（组件本来就要写，给无障碍与测试用），样式跟着属性走，
/// 不必再写 <c>selected ? s.Selected : null</c>；一个条件同时认原生、ARIA、data 几种写法。
/// </summary>
public static class When
{
    /// <summary>鼠标悬停。</summary>
    public static readonly Condition Hover = new(":hover");

    /// <summary>
    /// 可用时的鼠标悬停：不可用（<c>disabled</c>、<c>aria-disabled="true"</c>）的元素照样匹配 :hover，
    /// 悬停变色会让人以为还能点；按钮这类元素的悬停样式用它。
    /// </summary>
    public static readonly Condition HoverEnabled = new(""":hover:not(:disabled,[aria-disabled="true"])""");

    /// <summary>按下。</summary>
    public static readonly Condition Active = new(":active");

    /// <summary>获得焦点（含鼠标点击）。</summary>
    public static readonly Condition Focus = new(":focus");

    /// <summary>键盘导航时获得焦点：焦点环用它。</summary>
    public static readonly Condition FocusVisible = new(":focus-visible");

    /// <summary>自己或内部元素获得焦点。</summary>
    public static readonly Condition FocusWithin = new(":focus-within");

    /// <summary>不可用：<c>disabled</c>、<c>aria-disabled="true"</c> 或 <c>data-disabled</c>。</summary>
    public static readonly Condition Disabled = new(""":is(:disabled,[aria-disabled="true"],[data-disabled])""");

    /// <summary>选中（列表项、标签页）：<c>aria-selected="true"</c> 或 <c>data-selected</c>。</summary>
    public static readonly Condition Selected = new(""":is([aria-selected="true"],[data-selected])""");

    /// <summary>勾选：<c>:checked</c>、<c>aria-checked="true"</c> 或 <c>data-state="checked"</c>。</summary>
    public static readonly Condition Checked = new(""":is(:checked,[aria-checked="true"],[data-state="checked"])""");

    /// <summary>半选：<c>:indeterminate</c>、<c>aria-checked="mixed"</c> 或 <c>data-state="indeterminate"</c>。</summary>
    public static readonly Condition Indeterminate = new(""":is(:indeterminate,[aria-checked="mixed"],[data-state="indeterminate"])""");

    /// <summary>按下状态（切换按钮）：<c>aria-pressed="true"</c> 或 <c>data-pressed</c>。</summary>
    public static readonly Condition Pressed = new(""":is([aria-pressed="true"],[data-pressed])""");

    /// <summary>展开（触发器）：<c>aria-expanded="true"</c> 或 <c>data-expanded</c>。</summary>
    public static readonly Condition Expanded = new(""":is([aria-expanded="true"],[data-expanded])""");

    /// <summary>打开（弹层、details）：<c>open</c> 或 <c>data-state="open"</c>。</summary>
    public static readonly Condition Open = new(""":is([open],[data-state="open"])""");

    /// <summary>以 popover 属性弹出、正显示着的浮层（<c>:popover-open</c>）。</summary>
    public static readonly Condition PopoverOpen = new(":popover-open");

    /// <summary>没有子节点（空白文字也算没有）。浮层的内容第一次打开才渲染，渲染前用它把空壳藏起来。</summary>
    public static readonly Condition Empty = new(":empty");

    /// <summary>
    /// 父元素处于某个状态时：<c>[When.Parent(When.Hover)] = new() { Visibility = "visible" }</c> 生成 <c>.x:is(:hover &gt; *)</c>。
    /// 提示这类“悬停在外层上时显示里面某个部件”的样式用它，不用为父元素另起类名。
    /// </summary>
    public static Condition Parent(Condition state) => state.IsAtRule
        ? throw new ArgumentException($"父元素的状态要是选择器片段，收到 @ 规则 {state.Key}", nameof(state))
        : new($":is({state.Key} > *)");

    /// <summary>
    /// 某个祖先元素处于某个状态时：<c>[When.Inside(When.Attribute("[data-state=copied]"))]</c> 生成 <c>.x:is([data-state=copied] *)</c>。
    /// 复制按钮复制后换图标这类“外层的状态决定里面隔了几层的部件”的样式用它（同 vue-tsx-pro 的 css.when.ancestor）。
    /// </summary>
    public static Condition Inside(Condition state) => state.IsAtRule
        ? throw new ArgumentException($"祖先元素的状态要是选择器片段，收到 @ 规则 {state.Key}", nameof(state))
        : new($":is({state.Key} *)");

    /// <summary>
    /// 紧挨着的前一个兄弟元素处于某个状态时：<c>[When.After(When.Checked)]</c> 生成 <c>.x:is(:checked + *)</c>。
    /// 勾选框里跟在 input 后面的对勾图标用它。
    /// </summary>
    public static Condition After(Condition state) => state.IsAtRule
        ? throw new ArgumentException($"兄弟元素的状态要是选择器片段，收到 @ 规则 {state.Key}", nameof(state))
        : new($":is({state.Key} + *)");

    /// <summary>
    /// 里面有符合选择器的元素时：<c>[When.Has("[aria-invalid=true]")]</c> 生成 <c>.x:has([aria-invalid=true])</c>。
    /// 输入框的外框按里面 input 的状态（校验未通过、不可用）变化时用它。
    /// </summary>
    public static Condition Has(string selector) => new($":has({selector})");

    /// <summary>
    /// 进场的起始样式（<c>@starting-style</c>）：元素刚显示（插入、display 从 none 变成可见、弹出）时从这里过渡到正常样式。
    /// 退场配合 <c>transition-behavior: allow-discrete</c> 过渡 display / overlay，不需要 JS。
    /// </summary>
    public static readonly Condition Starting = new("@starting-style");

    /// <summary>当前项（导航）：<c>aria-current</c>（不为 false）或 <c>data-current</c>。</summary>
    public static readonly Condition Current = new(""":is([aria-current]:not([aria-current="false"]),[data-current])""");

    /// <summary>校验不通过：<c>aria-invalid="true"</c> 或 <c>data-invalid</c>。</summary>
    public static readonly Condition Invalid = new(""":is([aria-invalid="true"],[data-invalid])""");

    /// <summary>只读：<c>aria-readonly="true"</c> 或 <c>data-readonly</c>。</summary>
    public static readonly Condition ReadOnly = new(""":is([aria-readonly="true"],[data-readonly])""");

    /// <summary>键盘高亮（菜单、列表的活动项）：<c>data-highlighted</c>。</summary>
    public static readonly Condition Highlighted = new(":is([data-highlighted])");

    /// <summary>拖动中：<c>data-dragging</c>。</summary>
    public static readonly Condition Dragging = new(":is([data-dragging])");

    /// <summary>从右到左排版。</summary>
    public static readonly Condition Rtl = new(":dir(rtl)");

    /// <summary>
    /// 任意属性选择器：<c>When.Attribute("[data-tone=danger]")</c>。
    /// 别名覆盖不到的组件自有状态用它。
    /// </summary>
    public static Condition Attribute(string selector) =>
        selector.StartsWith('[') && selector.EndsWith(']')
            ? new($":is({selector})")
            : throw new ArgumentException($"属性选择器要写成 [name] 或 [name=value]，收到 {selector}", nameof(selector));
}

/// <summary>伪元素。</summary>
public static class Pseudo
{
    /// <summary>内容之前插入的盒子（需要写 Content）。</summary>
    public static readonly Condition Before = new("::before");

    /// <summary>内容之后插入的盒子（需要写 Content）。</summary>
    public static readonly Condition After = new("::after");

    /// <summary>模态对话框、全屏元素下面的遮罩。</summary>
    public static readonly Condition Backdrop = new("::backdrop");

    /// <summary>输入框的占位文字。</summary>
    public static readonly Condition Placeholder = new("::placeholder");

    /// <summary>选中的文字。</summary>
    public static readonly Condition Selection = new("::selection");
}

/// <summary>媒体查询与环境条件。</summary>
public static class Media
{
    /// <summary>视口宽度不小于 <paramref name="pixels"/>。</summary>
    public static Condition Up(int pixels) => new($"@media (min-width: {pixels}px)");

    /// <summary>视口宽度小于 <paramref name="pixels"/>。</summary>
    /// <remarks>上限减 0.02px（同 Bootstrap）：与 <see cref="Up"/> 同一个断点不会同时成立。</remarks>
    public static Condition Down(int pixels) => new(FormattableString.Invariant($"@media (max-width: {pixels - 0.02:0.##}px)"));

    /// <summary>用户要求减少动态效果：动画、过渡在这里关掉或减弱。</summary>
    public static readonly Condition ReducedMotion = new("@media (prefers-reduced-motion: reduce)");

    /// <summary>Windows 高对比度等强制配色模式。</summary>
    public static readonly Condition ForcedColors = new("@media (forced-colors: active)");

    /// <summary>系统深色模式。</summary>
    public static readonly Condition Dark = new("@media (prefers-color-scheme: dark)");

    /// <summary>任意 @media 查询：<c>Media.Query("(hover: hover)")</c>。</summary>
    public static Condition Query(string query) => new($"@media {query}");
}
