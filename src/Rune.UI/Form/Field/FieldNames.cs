using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Rune.UI;

/// <summary>
/// 字段在表单回发里的名字（同 Blazor 的 InputBase.NameAttributeValue）：<c>() =&gt; Model.Address.City</c> → <c>Model.Address.City</c>，
/// <c>() =&gt; Model.Lines[2].Amount</c> → <c>Model.Lines[2].Amount</c>。静态 SSR 提交时 <c>[SupplyParameterFromForm]</c> 按它把值绑回模型。
/// </summary>
static class FieldNames
{
    /// <summary>名字；表达式不是成员链与索引（方法调用、计算）时为 null。</summary>
    public static string? Format(LambdaExpression expression)
    {
        // 从最里面的成员往外走，入栈后按根 → 叶的顺序拼起来
        var segments = new Stack<string>();
        var node = expression.Body;
        while (true)
        {
            switch (node)
            {
                case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert:
                    node = convert.Operand;
                    continue;
                // 根：组件上的属性（this.Model）、捕获的局部变量（闭包类的字段）、静态成员
                case MemberExpression { Expression: ConstantExpression or null } root:
                    segments.Push(root.Member.Name);
                    return string.Concat(segments);
                case MemberExpression member:
                    segments.Push("." + member.Member.Name);
                    node = member.Expression;
                    continue;
                case MethodCallExpression { Method.Name: "get_Item", Arguments.Count: 1, Object: { } list } indexer
                    when IndexOf(indexer.Arguments[0]) is { } index:
                    segments.Push($"[{index}]");
                    node = list;
                    continue;
                case BinaryExpression { NodeType: ExpressionType.ArrayIndex } array when IndexOf(array.Right) is { } index:
                    segments.Push($"[{index}]");
                    node = array.Left;
                    continue;
                default:
                    return null;
            }
        }
    }

    // 下标：常量，或捕获的变量（循环里的 i）
    static string? IndexOf(Expression index) => index switch
    {
        ConstantExpression constant => Invariant(constant.Value),
        MemberExpression { Expression: ConstantExpression { Value: var target }, Member: FieldInfo field } => Invariant(field.GetValue(target)),
        MemberExpression { Expression: ConstantExpression { Value: var target }, Member: PropertyInfo property } => Invariant(property.GetValue(target)),
        _ => null,
    };

    static string? Invariant(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture);
}
