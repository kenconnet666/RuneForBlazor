using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace Rune.UI;

/// <summary>
/// 字段的显示名：<c>[Display(Name = …)]</c>（可以带资源类型做本地化），其次 <c>[DisplayName]</c>，同 Blazor 的 DisplayName 组件与 QuickGrid 的列标题。
/// Field 不写 Label 时用它；按模型类型与字段名缓存。
/// </summary>
static class DisplayNames
{
    static readonly ConcurrentDictionary<(Type Model, string Field), PropertyInfo?> properties = new();

    /// <summary>字段的显示名；没有标注时为 null。</summary>
    public static string? Of(FieldIdentifier field)
    {
        var property = properties.GetOrAdd((field.Model.GetType(), field.FieldName), static key => Find(key.Model, key.Field));
        // GetName 每次取：带资源类型时按当前的界面文化取文字
        return property?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? property?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "控件绑定的是 @bind-Value 的表达式（() => model.Email），表达式树引用了这个属性，裁剪时会保留它与它的特性。")]
    static PropertyInfo? Find(Type model, string name) => model.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
}
