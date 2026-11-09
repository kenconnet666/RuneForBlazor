using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 表单控件（TextField、Checkbox……）的基类：按 Blazor 的 <c>@bind-Value</c> 协议取值、改值，并接入 Blazor 自己的表单（<see cref="EditContext"/>）：
/// <list type="bullet">
/// <item><description>在 <c>EditForm</c> 里用 <c>@bind-Value="model.Email"</c> 时，改值后通知表单（触发 DataAnnotations 等校验），
/// 表单里这个字段有错误时控件标为 <c>aria-invalid</c>；</description></item>
/// <item><description>在 <see cref="Field"/> 里时，id、aria-describedby、必填、不可用等取自 Field（控件上显式写的优先），并把绑定的字段报给 Field 显示错误；</description></item>
/// <item><description>单独使用时就是普通的受控 / 非受控控件（<see cref="Bindable{T}"/> 的语义）。</description></item>
/// </list>
/// </summary>
/// <typeparam name="TValue">值的类型。</typeparam>
public abstract class FormControl<TValue> : UIComponent
{
    readonly string ownId = UniqueId.Next("control");
    Memo<(string Id, string? DescribedBy, bool Invalid, bool Required, bool Disabled, bool ReadOnly), Attrs> controlAttrs;
    Memo<(string Id, string Role, string? LabelledBy, string? DescribedBy, Orientation Orientation, bool Invalid, bool Required, bool Disabled, bool ReadOnly, Attrs? Extra), Attrs> groupAttrs;
    Memo<IReadOnlyDictionary<string, object>?, Attrs> inputUserAttrs;
    Memo<(Attrs? Props, Sx? Sx), Attrs> rootUserAttrs;
    Memo<(Attrs? Props, Attrs Direct), Attrs> rootRest;
    EditContext? subscribedContext;
    Expression<Func<TValue>>? identifiedExpression;
    FieldIdentifier? fieldIdentifier;
    (bool Done, string? Name) formattedName;
    Memo<(IReadOnlyDictionary<string, object>? Attributes, string? Name), Attrs> namedInputUserAttrs;

    /// <summary>值（<c>@bind-Value</c>）。</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>值改变后。</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>
    /// 根元素的部件属性：输入框一类是外壳（看得见的那个盒子），勾选框、开关是整个标签。
    /// 这类控件上直接写的属性（placeholder、name……）落在输入元素上，要给外壳的 class、data-* 写在这里；Sx 也落在外壳上，排在它之后。
    /// </summary>
    [Parameter] public Attrs? PropsRoot { get; set; }

    /// <summary>绑定的字段（<c>@bind-Value</c> 自动给出）：用它在表单里找到这个字段的校验状态。</summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>
    /// 表单提交时的名字（原生的输入元素、隐藏的 FormValue 用）；写成 <c>name="…"</c> 也是它（Razor 按参数名匹配时不区分大小写）。
    /// 不写时，在服务端渲染的 EditForm 里按 <c>@bind-Value</c> 的字段生成（同 Blazor 的 InputBase：<c>() =&gt; Model.Email</c> → <c>Model.Email</c>），
    /// 静态 SSR 下 <c>[SupplyParameterFromForm]</c> 按它把值绑回模型。
    /// </summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>不可用；不写时取所在 Field、Form、FormSection 的。</summary>
    [Parameter] public bool? Disabled { get; set; }

    /// <summary>只读；不写时取所在 Field、Form、FormSection 的。</summary>
    [Parameter] public bool? ReadOnly { get; set; }

    /// <summary>必填；不写时取所在 Field 的。</summary>
    [Parameter] public bool? Required { get; set; }

    /// <summary>校验未通过；不写时看所在 Field 与表单。</summary>
    [Parameter] public bool? Invalid { get; set; }

    /// <summary>所在的 Blazor 表单。</summary>
    [CascadingParameter] EditContext? EditContext { get; set; }

    /// <summary>所在的 Field。</summary>
    [CascadingParameter] protected FieldContext? Field { get; set; }

    /// <summary>所在的 Form、FormSection 的只读、不可用（控件不在 Field 里时也按它）。</summary>
    [CascadingParameter] FieldLayout? Layout { get; set; }

    Bindable<TValue> value { get => field ??= new(this, () => Value, () => ValueChanged); }

    /// <summary>
    /// 控件的主元素（输入框、触发按钮、勾选框），交互渲染之后才有值；用 <see cref="FocusAsync"/> 聚焦（同 Blazor 的 <c>InputBase.Element</c>）。
    /// 一组控件（RadioGroup、CheckboxGroup、ToggleGroup……）没有单一的主元素，是 null。
    /// </summary>
    public ElementReference? Element { get; protected set; }

    /// <summary>把焦点移到控件上；静态渲染（没有交互）时什么也不做。</summary>
    /// <param name="preventScroll">不把它滚动到可见范围。</param>
    public virtual ValueTask FocusAsync(bool preventScroll = false) =>
        RendererInfo.IsInteractive && Element is { } element ? element.FocusAsync(preventScroll) : ValueTask.CompletedTask;

    /// <summary>交给 <see cref="ValueElement"/> 的元素引用回调：记进 <see cref="Element"/>。</summary>
    protected Action<ElementReference> CaptureElement => field ??= element => Element = element;

    /// <summary>实际提交用的名字：<see cref="Name"/>，或按绑定的字段生成的（EditForm 要求生成时，见 <see cref="EditContext.ShouldUseFieldIdentifiers"/>）。</summary>
    protected string? FieldName => Name ?? (EditContext is { ShouldUseFieldIdentifiers: true } ? FormattedName : null);


    /// <summary>当前值（受控时是父级给的，非受控时是组件自己的）。</summary>
    protected TValue CurrentValue => value.Value;

    /// <summary>控件主元素的 id：使用方写的 id &gt; Field 给的 &gt; 自动生成的。</summary>
    protected string ControlId => AdditionalAttributes?.TryGetValue("id", out var id) == true && id is string s ? s : Field?.ControlId ?? ownId;

    /// <summary>当前生效的状态（控件上写的 &gt; 组 &gt; Field &gt; Form、FormSection）。</summary>
    protected bool IsDisabled => Disabled ?? InheritedState?.Disabled ?? Field?.Disabled ?? Layout?.Disabled ?? false;

    /// <inheritdoc cref="IsDisabled"/>
    protected bool IsReadOnly => ReadOnly ?? InheritedState?.ReadOnly ?? Field?.ReadOnly ?? Layout?.ReadOnly ?? false;

    /// <inheritdoc cref="IsDisabled"/>
    protected bool IsRequired => Required ?? Field?.Required ?? false;

    /// <inheritdoc cref="IsDisabled"/>
    protected bool IsInvalid => Invalid ?? (InheritedState?.Invalid == true || Field?.Invalid == true || HasValidationMessages());

    /// <summary>
    /// 所在的组（CheckboxGroup）交下来的不可用、只读、校验未通过；不在组里时为 null。组挡住了外面 Field 的级联（Field 的 id、说明属于组），
    /// 组里的控件按这里的状态。
    /// </summary>
    protected virtual InheritedControlState? InheritedState => null;

    /// <summary>
    /// 控件主元素（input）上由组件管理的属性：id、aria-describedby、aria-invalid、aria-required、disabled、readonly；输入不变时是同一个对象。
    /// </summary>
    protected Attrs ControlAttrs => controlAttrs.Get(
        (ControlId, Field?.DescribedBy, IsInvalid, IsRequired, IsDisabled, IsReadOnly),
        static k => new Attrs
        {
            Id = k.Id,
            AriaDescribedBy = k.DescribedBy,
            ["aria-invalid"] = k.Invalid ? "true" : null,
            // aria-required 而不是原生 required：原生的会在提交前拦下表单、弹浏览器自己的气泡，绕过 EditForm 的校验与错误显示
            ["aria-required"] = k.Required ? "true" : null,
            ["disabled"] = k.Disabled ? true : null,
            ["readonly"] = k.ReadOnly ? true : null,
        });

    /// <summary>
    /// 一组控件（RadioGroup、CheckboxGroup、ToggleGroup）的根元素上由组件管理的属性：id、role、所在 Field 的标签与说明
    /// （使用方写了 aria-label 时不再 aria-labelledby，后者会盖过前者）、方向与状态。
    /// div 没有原生的 disabled、required：radiogroup 用 aria-invalid / aria-required / aria-readonly，group 不支持这几个，只写 data-*。
    /// </summary>
    /// <param name="role"><c>radiogroup</c> 或 <c>group</c>。</param>
    /// <param name="orientation">排列方向，写在 data-orientation 上（role="group" 不允许 aria-orientation）。</param>
    /// <param name="extra">组件自己的固定属性（漫游焦点、样式标记），一起缓存；传静态实例。</param>
    protected Attrs GroupAttrs(string role, Orientation orientation, Attrs? extra = null) => groupAttrs.Get(
        (ControlId, role, AdditionalAttributes?.ContainsKey("aria-label") == true ? null : Field?.LabelId, Field?.DescribedBy,
            orientation, IsInvalid, IsRequired, IsDisabled, IsReadOnly, extra),
        static k =>
        {
            var radio = k.Role == "radiogroup";
            return Attrs.Merge(k.Extra, new Attrs
            {
                Id = k.Id,
                Role = k.Role,
                AriaLabelledBy = k.LabelledBy,
                AriaDescribedBy = k.DescribedBy,
                ["aria-invalid"] = radio && k.Invalid ? "true" : null,
                ["aria-required"] = radio && k.Required ? "true" : null,
                ["aria-readonly"] = radio && k.ReadOnly ? "true" : null,
                AriaDisabled = k.Disabled ? true : null,
                ["data-orientation"] = k.Orientation == Orientation.Vertical ? "vertical" : "horizontal",
                ["data-disabled"] = k.Disabled ? true : null,
                ["data-readonly"] = k.ReadOnly ? true : null,
            });
        });

    /// <summary>
    /// 使用方写的属性（placeholder、name、autocomplete、maxlength……）：落在输入元素上，不落在外框上。
    /// 外框用 <see cref="RootUserAttrs"/>。
    /// </summary>
    protected Attrs InputUserAttrs => inputUserAttrs.Get(AdditionalAttributes, static attributes => Attrs.From(attributes, null));

    /// <summary>
    /// <see cref="InputUserAttrs"/> 加上提交用的 name（<see cref="FieldName"/>）：给值就在原生元素上的控件（input、textarea、select）。
    /// 值交给隐藏的 FormValue 的控件（Select、NumberField……）把名字给 FormValue，看得见的元素上用 InputUserAttrs。
    /// </summary>
    protected Attrs NamedInputUserAttrs => namedInputUserAttrs.Get((AdditionalAttributes, FieldName), static k =>
        k.Name is null ? Attrs.From(k.Attributes, null) : Attrs.Merge(Attrs.From(k.Attributes, null), new Attrs { ["name"] = k.Name }));

    /// <summary>外壳上由组件管理的标记：点在外壳的空白处（内边距、图标、前后缀）时 JS 初始化器把焦点交给里面的输入元素。</summary>
    protected static readonly Attrs ShellAttrs = new() { ["data-rune-control"] = true };

    /// <summary>使用方给外框（看得见的那个盒子）的：<see cref="PropsRoot"/> 与 Sx。</summary>
    protected Attrs RootUserAttrs => rootUserAttrs.Get((PropsRoot, Sx), static k => k.Sx is null ? k.Props ?? Attrs.Empty : Attrs.Layer(k.Props, new Attrs { Sx = k.Sx }));

    /// <summary>根元素就是控件本身的（一组控件）：<see cref="PropsRoot"/> 在下，直接写的属性与 Sx 在上。</summary>
    protected Attrs RootRest => rootRest.Get((PropsRoot, Rest), static k => k.Props is null ? k.Direct : Attrs.Layer(k.Props, k.Direct));

    /// <summary>改值：受控时交给父级，否则改自己的；然后通知表单这个字段变了（触发校验）。</summary>
    protected async Task SetValueAsync(TValue newValue)
    {
        await value.SetAsync(newValue);
        if (EditContext is not null && FieldIdentifier is { } identifier) EditContext.NotifyFieldChanged(identifier);
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        // 表单换了（极少见）时换订阅：校验状态变了要重新渲染 aria-invalid
        if (!ReferenceEquals(subscribedContext, EditContext))
        {
            if (subscribedContext is not null) subscribedContext.OnValidationStateChanged -= OnValidationStateChanged;
            subscribedContext = EditContext;
            if (subscribedContext is not null) subscribedContext.OnValidationStateChanged += OnValidationStateChanged;
        }
        if (FieldIdentifier is { } identifier) Field?.Registration.Register(identifier);
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        if (subscribedContext is not null) subscribedContext.OnValidationStateChanged -= OnValidationStateChanged;
    }

    // 表达式不变时不重新解析（每次渲染父级都会传同一个表达式树的新实例时，按字段比较也一样）
    FieldIdentifier? FieldIdentifier
    {
        get
        {
            if (ValueExpression is null) return null;
            if (!ReferenceEquals(identifiedExpression, ValueExpression))
            {
                identifiedExpression = ValueExpression;
                fieldIdentifier = Microsoft.AspNetCore.Components.Forms.FieldIdentifier.Create(ValueExpression);
                formattedName = default;
            }
            return fieldIdentifier;
        }
    }

    // 按字段生成的名字：表达式变了（FieldIdentifier 里重置）才重新拼
    string? FormattedName
    {
        get
        {
            if (FieldIdentifier is null) return null;
            if (!formattedName.Done) formattedName = (true, FieldNames.Format(ValueExpression!));
            return formattedName.Name;
        }
    }

    bool HasValidationMessages() =>
        EditContext is not null && FieldIdentifier is { } identifier && EditContext.GetValidationMessages(identifier).Any();

    void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) => StateHasChanged();
}

/// <summary>组交给组里控件的状态（见 <see cref="FormControl{TValue}.InheritedState"/>）。</summary>
/// <param name="Disabled">整组不可用。</param>
/// <param name="ReadOnly">整组只读。</param>
/// <param name="Invalid">整组校验未通过（如“至少选一项”）。</param>
public readonly record struct InheritedControlState(bool Disabled, bool ReadOnly, bool Invalid);
