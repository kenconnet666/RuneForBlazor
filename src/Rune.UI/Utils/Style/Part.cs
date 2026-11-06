using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 组件内部节点（部件）的默认属性层：组件自己的样式、配置里对这个部件的定制，以及部件标记
/// <c>data-scope="collapsible" data-part="trigger"</c>（让使用方的全局 CSS、端到端测试能稳定地选中部件，不依赖生成的类名）。
/// </summary>
public static class Part
{
    /// <summary>
    /// 创建部件的默认层。在样式定义里调用（<see cref="Css.Define{TKey,T}"/>，按档位与定制缓存），渲染时不再创建。
    /// </summary>
    /// <param name="scope">组件名（连字符小写）。</param>
    /// <param name="part">部件。</param>
    /// <param name="sx">组件自己的样式。</param>
    /// <param name="styles">配置里的按部件定制，排在组件样式之后。</param>
    public static Attrs Of<TPart>(string scope, TPart part, Sx sx, ComponentStyles styles) where TPart : struct, Enum
    {
        var customized = styles.Of(part);
        return new Attrs
        {
            Sx = customized.Count == 0 ? sx : Sx.Create([.. sx, .. customized]),
            ["data-scope"] = scope,
            ["data-part"] = NameOf(part),
        };
    }

    /// <summary>
    /// 部件名：枚举名首字母小写（<c>TriggerIcon</c> → <c>triggerIcon</c>），同 vue-tsx-pro 的 partAttrs（组件名才转连字符小写）；
    /// 两边写法一致，对照工具才能按部件配对。
    /// </summary>
    static string NameOf<TPart>(TPart part) where TPart : struct, Enum
    {
        var name = part.ToString();
        return string.Concat(char.ToLowerInvariant(name[0]).ToString(), name.AsSpan(1));
    }
}
