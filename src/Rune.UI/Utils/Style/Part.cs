using System.Runtime.CompilerServices;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 组件内部节点（部件）的默认属性层：组件自己的样式、配置里对这个部件的定制，以及部件标记
/// <c>data-scope="collapsible" data-part="trigger"</c>（让使用方的全局 CSS、端到端测试能稳定地选中部件，不依赖生成的类名）。
/// </summary>
public static class Part
{
    static readonly ConditionalWeakTable<Attrs, Attrs> unmarked = new();

    /// <summary>
    /// 部件的默认层去掉部件标记，只留样式与按部件定制：部件是本库的另一个组件、而 vue-tsx-pro 在那里保留子组件自己的标记时用
    /// （分页的页码按钮、标签栏的滚动与新建按钮是 button/root）。按层缓存（层本身按配方缓存），渲染时不分配。
    /// </summary>
    public static Attrs Unmarked(Attrs layer) => unmarked.GetValue(layer, static l => l.Without("data-scope", "data-part"));

    /// <summary>
    /// 创建部件的默认层。在样式定义里调用（<see cref="Css.Define{TKey,T}"/>，按档位与定制缓存），渲染时不再创建。
    /// </summary>
    /// <param name="scope">组件名（连字符小写）。</param>
    /// <param name="part">部件。</param>
    /// <param name="sx">组件自己的样式。</param>
    /// <param name="styles">配置里的按部件定制，排在组件样式之后。</param>
    /// <param name="variants">组件当时的档位取值：配置里这些取值的定制排在按部件的定制之后。</param>
    public static Attrs Of<TPart>(string scope, TPart part, Sx sx, ComponentStyles styles, IReadOnlyCollection<Enum>? variants = null) where TPart : struct, Enum
    {
        var customized = variants is null ? styles.Of(part) : styles.Of(part, variants);
        return new Attrs
        {
            // 配置的定制单独一段（一个类、一层）：组件的档位与定制相加而不是相乘
            Sx = customized.Count == 0 ? sx : Sx.Layers(sx, Sx.Create([.. customized])),
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
