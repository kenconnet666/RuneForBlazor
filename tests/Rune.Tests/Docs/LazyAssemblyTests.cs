using System.Reflection;
using System.Reflection.Emit;
using Rune.Docs.Site;
using Rune.UI;

namespace Rune.Tests.Docs;

/// <summary>
/// 文档站按页面懒加载的程序集（Rune.Docs.Client 的 LazyAssemblies）：图表与地图数据只在图表页、3D 效果只在 3D 页、省市区只在 Cascader 页用到。
/// 别的页面的代码碰到这些程序集里的类型时，WebAssembly 里还没加载它们，执行到就会失败：这里检查编译出的代码
/// （基类、字段、方法签名、方法体里引用的类型与成员），按名字扫源码分不清参数名与同名的类型。
/// </summary>
public class LazyAssemblyTests
{
    static readonly Assembly Content = typeof(DocsCatalog).Assembly;

    [Fact]
    public void 图表与地图的类型只在图表页里用() =>
        Assert.Empty(Usages([typeof(LineChart<>).Assembly, typeof(ChartMaps).Assembly], allowed: "Rune.Docs.Components.Charts."));

    [Fact]
    public void 三维效果的类型只在3D页里用() =>
        Assert.Empty(Usages([typeof(ThreeCanvas).Assembly], allowed: "Rune.Docs.Components.Three."));

    [Fact]
    public void 省市区的类型只在Cascader页里用() =>
        Assert.Empty(Usages([typeof(Area).Assembly], allowed: "Rune.Docs.Components.Input.Cascader."));

    // 允许的命名空间以外，用到这些程序集的类型：“类型 → 用到的东西”
    static List<string> Usages(Assembly[] lazy, string allowed)
    {
        var found = new List<string>();
        foreach (var type in Content.GetTypes())
        {
            var owner = type;
            while (owner.DeclaringType is not null) owner = owner.DeclaringType;
            // Razor 为泛型组件推断类型参数生成的辅助类在 __Blazor.{原命名空间} 下，算作原来的页面
            var name = owner.FullName!.StartsWith("__Blazor.", StringComparison.Ordinal) ? owner.FullName["__Blazor.".Length..] : owner.FullName!;
            if (name.StartsWith(allowed, StringComparison.Ordinal)) continue;
            foreach (var used in Referenced(type))
                if (Involves(used, lazy)) found.Add($"{type.FullName} → {used}");
        }
        return found;
    }

    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // 类型本身的签名与它各方法体里引用的类型、方法、字段
    static IEnumerable<object> Referenced(Type type)
    {
        if (type.BaseType is { } baseType) yield return baseType;
        foreach (var field in type.GetFields(All)) yield return field.FieldType;
        foreach (var method in type.GetMethods(All).Cast<MethodBase>().Concat(type.GetConstructors(All)))
        {
            if (method is MethodInfo info) yield return info.ReturnType;
            foreach (var parameter in method.GetParameters()) yield return parameter.ParameterType;
            foreach (var member in BodyReferences(method)) yield return member;
        }
    }

    static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => o.Value);

    // 方法体（IL）里带元数据记号的指令引用的成员
    static IEnumerable<object> BodyReferences(MethodBase method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null) yield break;
        var typeArgs = method.DeclaringType!.IsGenericType ? method.DeclaringType.GetGenericArguments() : null;
        var methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;
        for (var at = 0; at < il.Length;)
        {
            var code = il[at] == 0xFE ? OpCodesByValue[(short)(0xFE00 | il[at + 1])] : OpCodesByValue[il[at]];
            at += code.Size;
            switch (code.OperandType)
            {
                case OperandType.InlineType or OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineTok:
                    var token = BitConverter.ToInt32(il, at);
                    object? member = null;
                    try
                    {
                        member = method.Module.ResolveMember(token, typeArgs, methodArgs);
                    }
                    catch (ArgumentException)
                    {
                        // 记号不是成员（极少见的 ldtoken 形式）：跳过
                    }
                    if (member is not null) yield return member;
                    at += 4;
                    break;
                case OperandType.InlineSwitch:
                    at += 4 + 4 * BitConverter.ToInt32(il, at);
                    break;
                default:
                    at += code.OperandType switch
                    {
                        OperandType.InlineNone => 0,
                        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                        OperandType.InlineVar => 2,
                        OperandType.InlineI8 or OperandType.InlineR => 8,
                        _ => 4,
                    };
                    break;
            }
        }
    }

    // 类型、方法、字段是否来自（或泛型参数里有）这些程序集
    static bool Involves(object used, Assembly[] lazy) => used switch
    {
        Type t when t.HasElementType => Involves(t.GetElementType()!, lazy),
        Type { IsGenericParameter: true } => false,
        Type t => lazy.Contains(t.Assembly) || t.IsGenericType && t.GetGenericArguments().Any(a => Involves(a, lazy)),
        MethodInfo m => Involves(m.DeclaringType!, lazy) || m.IsGenericMethod && m.GetGenericArguments().Any(a => Involves(a, lazy)),
        MemberInfo m => m.DeclaringType is { } declaring && Involves(declaring, lazy),
        _ => false,
    };
}
