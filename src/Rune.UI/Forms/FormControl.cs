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
    Memo<IReadOnlyDictionary<string, object>?, Attrs> inputUserAttrs;
    Memo<Sx?, Attrs> rootUserAttrs;
    EditContext? subscribedContext;
    Expression<Func<TValue>>? identifiedExpression;
    FieldIdentifier? fieldIdentifier;

    /// <summary>值（<c>@bind-Value</c>）。</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>值改变后。</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>绑定的字段（<c>@bind-Value</c> 自动给出）：用它在表单里找到这个字段的校验状态。</summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>不可用；不写时取所在 Field 的。</summary>
    [Parameter] public bool? Disabled { get; set; }

    /// <summary>只读；不写时取所在 Field 的。</summary>
    [Parameter] public bool? ReadOnly { get; set; }

    /// <summary>必填；不写时取所在 Field 的。</summary>
    [Parameter] public bool? Required { get; set; }

    /// <summary>校验未通过；不写时看所在 Field 与表单。</summary>
    [Parameter] public bool? Invalid { get; set; }

    /// <summary>所在的 Blazor 表单。</summary>
    [CascadingParameter] EditContext? EditContext { get; set; }

    /// <summary>所在的 Field。</summary>
    [CascadingParameter] protected FieldContext? Field { get; set; }

    Bindable<TValue> value { get => field ??= new(() => Value, () => ValueChanged); }

    /// <summary>当前值（受控时是父级给的，非受控时是组件自己的）。</summary>
    protected TValue CurrentValue => value.Value;

    /// <summary>控件主元素的 id：使用方写的 id &gt; Field 给的 &gt; 自动生成的。</summary>
    protected string ControlId => AdditionalAttributes?.TryGetValue("id", out var id) == true && id is string s ? s : Field?.ControlId ?? ownId;

    /// <summary>当前生效的状态（控件上写的 &gt; Field &gt; 表单）。</summary>
    protected bool IsDisabled => Disabled ?? Field?.Disabled ?? false;

    /// <inheritdoc cref="IsDisabled"/>
    protected bool IsReadOnly => ReadOnly ?? Field?.ReadOnly ?? false;

    /// <inheritdoc cref="IsDisabled"/>
    protected bool IsRequired => Required ?? Field?.Required ?? false;

    /// <inheritdoc cref="IsDisabled"/>
    protected bool IsInvalid => Invalid ?? (Field?.Invalid == true || HasValidationMessages());

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
    /// 使用方写的属性（placeholder、name、autocomplete、maxlength……）：落在输入元素上，不落在外框上。
    /// 外框用 <see cref="RootUserAttrs"/>。
    /// </summary>
    protected Attrs InputUserAttrs => inputUserAttrs.Get(AdditionalAttributes, static attributes => Attrs.From(attributes, null));

    /// <summary>使用方的 Sx：落在外框（看得见的那个盒子）上。</summary>
    protected Attrs RootUserAttrs => rootUserAttrs.Get(Sx, static sx => sx is null ? Attrs.Empty : new Attrs { Sx = sx });

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
            }
            return fieldIdentifier;
        }
    }

    bool HasValidationMessages() =>
        EditContext is not null && FieldIdentifier is { } identifier && EditContext.GetValidationMessages(identifier).Any();

    void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) => StateHasChanged();
}
