using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 组件内部节点（部件）的默认属性层：样式加上部件标记 <c>data-scope="collapsible" data-part="trigger"</c>。
/// 部件标记让使用方的全局 CSS、端到端测试能稳定地选中部件，不依赖生成的类名。
/// </summary>
public static class Part
{
    /// <summary>创建部件的默认层；在样式定义里调用（<see cref="Css.Define{TKey,T}"/>），每个档位只创建一次。</summary>
    /// <param name="scope">组件名（连字符小写）。</param>
    /// <param name="name">部件名（连字符小写）。</param>
    /// <param name="sx">部件的默认样式。</param>
    public static Attrs Of(string scope, string name, Sx sx) => new()
    {
        Sx = sx,
        ["data-scope"] = scope,
        ["data-part"] = name,
    };
}
