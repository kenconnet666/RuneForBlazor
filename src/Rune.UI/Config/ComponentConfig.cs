using System.Collections;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 组件的定制（同 vue-tsx-pro 的 <c>components</c>）：每个组件一项，默认值、按部件追加的样式、按档位取值追加的样式写在一起。
/// 组件用部件枚举（与默认值记录）指定，写错组件、部件或档位编译不过：
/// <code>
/// builder.Services.AddRuneUI(o => o.Components = new()
/// {
///     new ComponentConfig&lt;ButtonDefaults, ButtonPart&gt;
///     {
///         Defaults = new() { Variant = ButtonVariant.Outline, Radius = Radius.Full },
///         Styles = { [ButtonPart.Root] = AppStyles.Wide },
///         Variants = { [ButtonVariant.Outline] = { [ButtonPart.Root] = AppStyles.Dashed } },
///     },
///     new ComponentConfig&lt;CardPart&gt; { Styles = { [CardPart.Header] = AppStyles.BoldHeader } },
/// });
/// </code>
/// <see cref="RuneProvider"/> 的 <c>Components</c> 在一片区域里再叠一层（默认值按项合并、内层优先；样式排在外层之后）。
/// 只在创建时添加，之后不再修改（同一个实例会被缓存引用）。
/// </summary>
public sealed class ComponentConfigs : IEnumerable<ComponentConfig>
{
    readonly List<ComponentConfig> items = [];

    /// <summary>不做任何定制。</summary>
    public static readonly ComponentConfigs Empty = new();

    /// <summary>合成的组件默认值（组件经 <see cref="UIConfig.Defaults"/> 读取）。</summary>
    internal ComponentDefaults Defaults { get; private set; } = new();

    /// <summary>合成的样式定制（组件经 <see cref="UIConfig.Styles"/> 读取，配方据此追加）。</summary>
    internal ComponentStyles Styles { get; private set; } = new();

    /// <summary>添加一个组件的定制（集合初始化器用）；同一组件写两次时后者叠在前者上。</summary>
    public void Add(ComponentConfig config)
    {
        if (ReferenceEquals(this, Empty)) throw new InvalidOperationException("ComponentConfigs.Empty 是共用的，不能往里添加：新建一个");
        items.Add(config);
        config.ApplyTo(Defaults, Styles);
    }

    /// <summary>叠上内层的定制（Provider 嵌套时）：默认值按项合并、内层优先，样式排在外层之后。</summary>
    public ComponentConfigs Then(ComponentConfigs inner)
    {
        if (inner.items.Count == 0) return this;
        var merged = new ComponentConfigs { Defaults = Defaults.Then(inner.Defaults), Styles = Styles.Then(inner.Styles) };
        merged.items.AddRange(items);
        merged.items.AddRange(inner.items);
        return merged;
    }

    /// <summary>各项，按添加顺序。</summary>
    public IEnumerator<ComponentConfig> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>一个组件的定制（<see cref="ComponentConfigs"/> 的一项）：<see cref="ComponentConfig{TPart}"/> 或 <see cref="ComponentConfig{TDefaults, TPart}"/>。</summary>
public abstract class ComponentConfig
{
    private protected ComponentConfig() { }

    /// <summary>拆进两层机制：默认值、样式定制。</summary>
    internal abstract void ApplyTo(ComponentDefaults defaults, ComponentStyles styles);
}

/// <summary>一个组件的样式定制：按部件、按档位取值追加在组件自己的样式之后（没有默认值记录的组件用它）。</summary>
/// <typeparam name="TPart">组件的部件枚举（<c>CardPart</c>）。</typeparam>
public class ComponentConfig<TPart> : ComponentConfig where TPart : struct, Enum
{
    /// <summary>按部件追加的样式：<c>Styles = { [CardPart.Header] = AppStyles.BoldHeader }</c>。</summary>
    public PartStyles<TPart> Styles { get; } = new();

    /// <summary>
    /// 某个档位取值时追加的样式，排在 <see cref="Styles"/> 之后：<c>Variants = { [ButtonVariant.Outline] = { [ButtonPart.Root] = Dashed } }</c>。
    /// 档位是组件外观里的枚举（变体、色调、尺寸、圆角……），组件当时取这个值才追加。
    /// </summary>
    public VariantStyles<TPart> Variants { get; } = new();

    internal override void ApplyTo(ComponentDefaults defaults, ComponentStyles styles)
    {
        foreach (var (part, sx) in Styles)
            foreach (var style in sx) styles.Add(part, style);
        foreach (var (variant, parts) in Variants)
            foreach (var (part, sx) in parts)
                foreach (var style in sx) styles.AddVariant(variant, part, style);
    }
}

/// <summary>一个组件的定制：默认值与样式（有默认值记录的组件用它）。</summary>
/// <typeparam name="TDefaults">组件的默认值记录（<c>ButtonDefaults</c>）。</typeparam>
/// <typeparam name="TPart">组件的部件枚举（<c>ButtonPart</c>）。</typeparam>
public sealed class ComponentConfig<TDefaults, TPart> : ComponentConfig<TPart>
    where TDefaults : class, IComponentDefaults<TDefaults>
    where TPart : struct, Enum
{
    /// <summary>档位的默认值：组件上写的参数 &gt; 最近一层 Provider &gt; 应用级 &gt; 组件自己的默认值。</summary>
    public TDefaults? Defaults { get; init; }

    internal override void ApplyTo(ComponentDefaults defaults, ComponentStyles styles)
    {
        base.ApplyTo(defaults, styles);
        if (Defaults is not null) defaults.Add(Defaults);
    }
}

/// <summary>各部件追加的样式（对象初始化器里写：<c>{ [CardPart.Header] = AppStyles.BoldHeader }</c>，几段写 <c>Sx.Of(a, b)</c>）。</summary>
public sealed class PartStyles<TPart> : IEnumerable<KeyValuePair<TPart, Sx>> where TPart : struct, Enum
{
    readonly List<KeyValuePair<TPart, Sx>> entries = [];

    /// <summary>给这个部件追加样式（同一部件写几次依次追加）。</summary>
    public Sx this[TPart part] { set => entries.Add(new(part, value)); }

    /// <summary>按书写顺序。</summary>
    public IEnumerator<KeyValuePair<TPart, Sx>> GetEnumerator() => entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>按档位取值追加的样式：档位取值（枚举值）→ 各部件的样式（<c>{ [ButtonVariant.Outline] = { [ButtonPart.Root] = Dashed } }</c>）。</summary>
public sealed class VariantStyles<TPart> : IEnumerable<KeyValuePair<Enum, PartStyles<TPart>>> where TPart : struct, Enum
{
    readonly List<KeyValuePair<Enum, PartStyles<TPart>>> entries = [];

    /// <summary>这个档位取值的各部件样式（对象初始化器里接着写部件）。</summary>
    public PartStyles<TPart> this[Enum variant]
    {
        get
        {
            foreach (var (value, parts) in entries)
                if (value.Equals(variant)) return parts;
            var added = new PartStyles<TPart>();
            entries.Add(new(variant, added));
            return added;
        }
    }

    /// <summary>按书写顺序。</summary>
    public IEnumerator<KeyValuePair<Enum, PartStyles<TPart>>> GetEnumerator() => entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// 外观档位（配方的键，如 <c>ButtonAppearance</c>）：交出其中的枚举取值，按档位的定制（<see cref="ComponentConfig{TPart}.Variants"/>）据此匹配。
/// 配方的键是元组时不用实现，元组里的枚举直接取。
/// </summary>
public interface IAppearance
{
    /// <summary>各档位的取值；没有取值（可空档位为 null）的写 null。</summary>
    IEnumerable<Enum?> Variants { get; }
}
