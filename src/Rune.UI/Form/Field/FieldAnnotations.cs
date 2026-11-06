using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace Rune.UI;

/// <summary>
/// 绑定字段上的标注（Field 没写时用）：显示名取 <c>[Display(Name = …)]</c>（可以带资源类型做本地化），其次 <c>[DisplayName]</c>，
/// 同 Blazor 的 DisplayName 组件与 QuickGrid 的列标题；必填看 <c>[Required]</c>（同 vue-tsx-pro 的 form.Field 按 schema 标必填）。
/// 属性按模型类型与字段名缓存。
/// </summary>
static class FieldAnnotations
{
    static readonly ConcurrentDictionary<(Type Model, string Field), PropertyInfo?> properties = new();

    /// <summary>字段的显示名；没有标注时为 null。</summary>
    public static string? DisplayName(FieldIdentifier field)
    {
        var property = PropertyOf(field);
        // GetName 每次取：带资源类型时按当前的界面文化取文字
        return property?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? property?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
    }

    /// <summary>字段标了 <c>[Required]</c>。</summary>
    public static bool IsRequired(FieldIdentifier field) => PropertyOf(field)?.IsDefined(typeof(RequiredAttribute), inherit: true) == true;

    static PropertyInfo? PropertyOf(FieldIdentifier field) =>
        properties.GetOrAdd((field.Model.GetType(), field.FieldName), static key => Find(key.Model, key.Field));

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "控件绑定的是 @bind-Value 的表达式（() => model.Email），表达式树引用了这个属性，裁剪时会保留它与它的特性。")]
    static PropertyInfo? Find(Type model, string name) => model.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
}
