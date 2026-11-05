using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 配方：一个组件各部件的样式，按档位（外观、色调、尺寸……）组合。每种组合只算一次，
/// 并统一加上配置里的按部件定制与部件标记（<see cref="Part.Of{TPart}"/>）：
/// <code>
/// public static PartLayers&lt;CollapsiblePart&gt; Of(CollapsibleVariant variant, ComponentStyles styles) =>
///     Recipe.Of("collapsible", variant, styles, static (CollapsibleVariant variant, CollapsiblePart part) => part switch
///     {
///         CollapsiblePart.Root => variant == CollapsibleVariant.Outline ? [Root, OutlineRoot] : Root,
///         CollapsiblePart.Trigger => Trigger,
///         _ => Content,
///     });
/// </code>
/// 组件渲染时取部件的默认层交给合并：<c>Attrs.Merge(parts[CollapsiblePart.Trigger], PropsTrigger, managed)</c>。
/// </summary>
public static class Recipe
{
    /// <summary>没有档位的组件（只有一种外观）：每个部件一段样式。</summary>
    /// <param name="scope">组件名（连字符小写）。</param>
    /// <param name="styles">配置里的按部件定制。</param>
    /// <param name="partStyle">某个部件的样式；写成 static lambda。</param>
    public static PartLayers<TPart> Of<TPart>(string scope, ComponentStyles styles, Func<TPart, Sx> partStyle)
        where TPart : struct, Enum =>
        Of(scope, partStyle, styles, static (Func<TPart, Sx> partStyle, TPart part) => partStyle(part));

    /// <summary>算出（或取缓存的）各部件默认层。</summary>
    /// <param name="scope">组件名（连字符小写），写在部件标记 data-scope 上。</param>
    /// <param name="key">档位的组合，通常是元组；相等的组合共用同一份结果。</param>
    /// <param name="styles">配置里的按部件定制。</param>
    /// <param name="partStyle">某个档位组合下某个部件的样式；写成 static lambda。</param>
    public static PartLayers<TPart> Of<TKey, TPart>(string scope, TKey key, ComponentStyles styles, Func<TKey, TPart, Sx> partStyle)
        where TPart : struct, Enum =>
        Css.Define((scope, key, styles, partStyle), static k => new PartLayers<TPart>(
            Enum.GetValues<TPart>().Select(part => Part.Of(k.scope, part, k.partStyle(k.key, part), k.styles))));
}

/// <summary>一个组件各部件的默认层，按部件枚举取。</summary>
public sealed class PartLayers<TPart> where TPart : struct, Enum
{
    readonly Dictionary<TPart, Attrs> layers;

    internal PartLayers(IEnumerable<Attrs> layersInEnumOrder) =>
        layers = Enum.GetValues<TPart>().Zip(layersInEnumOrder).ToDictionary(p => p.First, p => p.Second);

    /// <summary>某个部件的默认层（样式、按部件定制、部件标记）。</summary>
    public Attrs this[TPart part] => layers[part];
}
