using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Docs.Site;

/// <summary>
/// 从组件类型生成 API 文档数据（同 vue-tsx-pro 文档站从 Props 类型与 JSDoc 生成的 virtual:api）：
/// 参数按反射读出（[Parameter]，不含级联参数与收集未声明属性的那个），说明取组件库的 XML 文档注释（嵌在内容程序集里），
/// 默认值取新建的组件实例上的值（与类型的默认值相同时不列）。
/// </summary>
public static partial class ComponentApiReader
{
    static readonly ConcurrentDictionary<Type, ComponentApi> cache = new();
    static readonly Lazy<DocsIndex> docs = new(LoadDocs);
    // NullabilityInfoContext 内部有不加锁的缓存：并发渲染的页面（Server 的多个线路）各用各的
    [ThreadStatic] static NullabilityInfoContext? nullability;

    /// <summary>组件的 API 数据。泛型组件（CommandPalette&lt;TItem&gt;）写开放类型 <c>typeof(CommandPalette&lt;&gt;)</c>。</summary>
    public static ComponentApi Read(Type component) => cache.GetOrAdd(component, Build);

    static ComponentApi Build(Type component)
    {
        var closed = component.IsGenericTypeDefinition ? Close(component) : component;
        // 组件列 [Parameter]；数据类型（TimelineItem、UploadFile、NumberFormat 这类项与选项）列公开属性
        var isComponent = typeof(IComponent).IsAssignableFrom(closed);
        var instance = NewInstance(closed);
        var properties = closed.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => isComponent
                ? p.GetCustomAttribute<ParameterAttribute>() is { CaptureUnmatchedValues: false }
                : p.GetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .ToList();
        var names = properties.Select(p => p.Name).ToHashSet();
        var constructorParameters = isComponent ? [] : PrimaryParameters(closed);
        var props = properties
            .OrderBy(p => Depth(p.DeclaringType!, closed))
            .ThenBy(p => constructorParameters.Contains(p.Name) ? 0 : 1)
            .Select(p => PropOf(p, instance, names) is var prop && constructorParameters.Contains(p.Name) ? prop with { Required = true } : prop)
            .ToList();
        return new ComponentApi(DisplayName(component), Summary(MemberId('T', component)), props);
    }

    // 读默认值用的实例：有无参构造就用它；只有带参数的构造（record 的主构造）时传各参数类型的默认值，属性的初始值照样生效
    static object? NewInstance(Type type)
    {
        if (type.GetConstructor(Type.EmptyTypes) is not null) return Activator.CreateInstance(type);
        var constructor = type.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault();
        return constructor?.Invoke([.. constructor.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)]);
    }

    // record 主构造的参数：对应的属性是必填的
    // 没有公开构造的（枚举）没有
    static HashSet<string> PrimaryParameters(Type type) => type.GetConstructor(Type.EmptyTypes) is null && type.GetConstructors() is [_, ..] constructors
        ? [.. constructors.OrderBy(c => c.GetParameters().Length).First().GetParameters().Select(p => p.Name!)]
        : [];

    // 泛型组件随便取一个满足约束的类型参数（只读参数、默认值，不渲染）：数字组件（Slider<TValue>）要数字
    static readonly Type[] Candidates = [typeof(object), typeof(string), typeof(int), typeof(double)];

    static Type Close(Type definition)
    {
        foreach (var candidate in Candidates)
        {
            try
            {
                return definition.MakeGenericType([.. definition.GetGenericArguments().Select(_ => candidate)]);
            }
            catch (ArgumentException)
            {
                // 不满足约束：换下一个
            }
        }
        throw new InvalidOperationException($"{definition.Name} 的类型参数找不到满足约束的类型");
    }

    // 子类自己声明的参数在前，基类的在后
    static int Depth(Type declaring, Type type)
    {
        var depth = 0;
        for (var t = type; t is not null && t != declaring; t = t.BaseType) depth++;
        return depth;
    }

    static ApiProp PropOf(PropertyInfo property, object? instance, HashSet<string> names)
    {
        var type = property.PropertyType;
        var kind = KindOf(property);
        var description = Summary(MemberId('P', property.DeclaringType!, property.Name));
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return new ApiProp(property.Name, kind, TypeName(property), description)
        {
            Required = property.GetCustomAttribute<EditorRequiredAttribute>() is not null
                || property.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() is not null,
            Bindable = kind != ApiPropKind.Event && names.Contains(property.Name + "Changed"),
            Default = kind == ApiPropKind.Prop ? DefaultOf(property, instance) : null,
            Values = underlying.IsEnum ? [.. Enum.GetNames(underlying).Select(n => new ApiValue(n, Summary(MemberId('F', underlying, n))))] : null,
        };
    }

    static ApiPropKind KindOf(PropertyInfo property)
    {
        var type = property.PropertyType;
        if (type == typeof(EventCallback) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>)) return ApiPropKind.Event;
        if (type == typeof(RenderFragment) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>)) return ApiPropKind.Content;
        if (type == typeof(Attrs) && property.Name.StartsWith("Props", StringComparison.Ordinal)) return ApiPropKind.Part;
        return ApiPropKind.Prop;
    }

    // 实例上的值与类型的默认值（null、false、0）相同时不列；枚举总是列出（第一项也是有意义的默认）
    static string? DefaultOf(PropertyInfo property, object? instance)
    {
        // 抽象的基类（SegmentedField）没有实例：默认值列在子类的表里
        if (instance is null) return null;
        object? value;
        try
        {
            value = property.GetValue(instance);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
        return value switch
        {
            null => null,
            bool b => b ? "true" : null,
            string s => s.Length == 0 ? null : $"\"{s}\"",
            Enum e => $"{e.GetType().Name}.{e}",
            int or long or double or float or decimal when Convert.ToDouble(value, CultureInfo.InvariantCulture) == 0 => null,
            IFormattable f when value.GetType().IsPrimitive || value is decimal => f.ToString(null, CultureInfo.InvariantCulture),
            _ when value.GetType().IsValueType && value.Equals(Activator.CreateInstance(value.GetType())) => null,
            System.Collections.IEnumerable list when !list.GetEnumerator().MoveNext() => null,
            _ => value.ToString(),
        };
    }

    // C# 的写法：bool、string?、IReadOnlyList<string>、EventCallback<bool>
    static string TypeName(PropertyInfo property)
    {
        var name = TypeName(property.PropertyType);
        if (!property.PropertyType.IsValueType && (nullability ??= new()).Create(property).ReadState == NullabilityState.Nullable) name += "?";
        return name;
    }

    static string TypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } inner) return TypeName(inner) + "?";
        if (type.IsArray) return TypeName(type.GetElementType()!) + "[]";
        // 枚举的 TypeCode 是它的底层类型（int）：先按名字
        if (type.IsEnum) return type.Name;
        var keyword = Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "bool",
            TypeCode.String => "string",
            TypeCode.Int32 => "int",
            TypeCode.Int64 => "long",
            TypeCode.Double => "double",
            TypeCode.Single => "float",
            TypeCode.Decimal => "decimal",
            _ => null,
        };
        if (keyword is not null) return keyword;
        if (type == typeof(object)) return "object";
        if (type.IsGenericParameter) return type.Name;
        if (!type.IsGenericType) return type.Name;
        var args = string.Join(", ", type.GetGenericArguments().Select(TypeName));
        return $"{type.Name[..type.Name.IndexOf('`')]}<{args}>";
    }

    // 组件名：CommandPalette<TItem>
    static string DisplayName(Type type) => type.IsGenericTypeDefinition
        ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(a => a.Name))}>"
        : type.Name;

    // XML 文档的成员 id：T:Rune.UI.Button、P:Rune.UI.Button.Disabled、F:Rune.UI.Size.Md（泛型类型带 `1）
    static string MemberId(char kind, Type type, string? member = null)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        var name = definition.FullName!.Replace('+', '.');
        return member is null ? $"{kind}:{name}" : $"{kind}:{name}.{member}";
    }

    static string Summary(string id)
    {
        if (docs.Value.Find(id)?.Element("summary") is not { } summary) return "";
        return Whitespace().Replace(TextOf(summary), " ").Trim();
    }

    // <c>x</c> 写成 x，<see cref="T:Rune.UI.Button"/> 写成 Button，其余取文字
    static string TextOf(XElement element) => string.Concat(element.Nodes().Select(node => node switch
    {
        XText text => text.Value,
        XElement { Name.LocalName: "see" or "seealso" } see => see.Attribute("langword")?.Value
            ?? (see.Value.Length > 0 ? see.Value : ShortName(see.Attribute("cref")?.Value ?? "")),
        XElement { Name.LocalName: "paramref" or "typeparamref" } reference => reference.Attribute("name")?.Value ?? "",
        XElement child => TextOf(child),
        _ => "",
    }));

    static string ShortName(string cref)
    {
        var name = cref.Contains(':') ? cref[(cref.IndexOf(':') + 1)..] : cref;
        if (name.IndexOf('(') is var paren and > 0) name = name[..paren];
        var parts = name.Split('.');
        // 成员写成“类型.成员”，类型只写名字
        return parts.Length >= 2 && cref.StartsWith("T:", StringComparison.Ordinal) is false ? $"{parts[^2]}.{parts[^1]}" : parts[^1];
    }

    // 各个包（Rune.UI、Rune.UI.Dates、Rune.Charts）的注释接在一起建一个索引
    static DocsIndex LoadDocs()
    {
        var assembly = typeof(ComponentApiReader).Assembly;
        var text = new System.Text.StringBuilder();
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.StartsWith("api/", StringComparison.Ordinal)))
        {
            using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
            text.Append(reader.ReadToEnd());
        }
        return new DocsIndex(text.ToString());
    }

    /// <summary>
    /// XML 文档注释的索引：只记下每个成员在原文里的位置，用到时才解析那一段。
    /// 整份建成 XDocument（三千多个成员）在 WebAssembly 里要大半秒，第一个有 API 表的页面会因此卡住。
    /// </summary>
    sealed class DocsIndex
    {
        const string Open = "<member name=\"";
        const string Close = "</member>";
        readonly string text;
        readonly Dictionary<string, (int Start, int Length)> members = [];

        public DocsIndex(string text)
        {
            this.text = text;
            for (var at = text.IndexOf(Open, StringComparison.Ordinal); at >= 0; at = text.IndexOf(Open, at + 1, StringComparison.Ordinal))
            {
                var nameStart = at + Open.Length;
                var nameEnd = text.IndexOf('"', nameStart);
                if (nameEnd < 0) break;
                var end = text.IndexOf(Close, nameEnd, StringComparison.Ordinal);
                if (end < 0) break;
                // 同名的取第一个（同 XML 里的先后）
                members.TryAdd(text[nameStart..nameEnd], (at, end + Close.Length - at));
            }
        }

        /// <summary>成员的 member 元素；没有文档时为 null。</summary>
        public XElement? Find(string id) =>
            members.TryGetValue(id, out var range) ? XElement.Parse(text.Substring(range.Start, range.Length)) : null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
