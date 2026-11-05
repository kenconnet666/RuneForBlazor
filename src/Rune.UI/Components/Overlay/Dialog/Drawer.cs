using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>抽屉从哪一边滑出（同 vue-tsx-pro 的 DrawerSides）；Start / End 是逻辑方向，从右到左排版下左右互换。</summary>
public enum DrawerSide
{
    /// <summary>结束边（从左到右的页面是右侧，默认）：详情、筛选、表单。</summary>
    End,

    /// <summary>起始边：导航菜单。</summary>
    Start,

    /// <summary>顶部。</summary>
    Top,

    /// <summary>底部：手机上的操作面板。</summary>
    Bottom,
}

/// <summary>
/// 抽屉：贴着视口一侧滑入的模态面板（同 vue-tsx-pro 的 Drawer）。用法同 <see cref="Dialog"/>，多一个 <see cref="Side"/>。
/// 左右两侧时可以拖动靠里的那条边调整宽度（ResizeHandle：拖动、← / →、双击回到打开时的宽度）；<c>@bind-Width</c> 记住拖动后的宽度。
/// </summary>
public sealed class Drawer : DialogBase
{
    double? initialWidth;
    bool initialized;

    /// <summary>从哪一边滑出，默认结束边。</summary>
    [Parameter] public DrawerSide Side { get; set; }

    /// <summary>宽度（左右两侧）档位，默认 Xs（30rem）；上下两侧时高度由内容决定，最多到视口高度。</summary>
    [Parameter] public ContainerSize? Size { get; set; }

    /// <summary>宽度（px，左右两侧，<c>@bind-Width</c>）：写了就不按 Size 的档位；拖动后写回。</summary>
    [Parameter] public double? Width { get; set; }

    /// <summary>拖动调整了宽度。</summary>
    [Parameter] public EventCallback<double?> WidthChanged { get; set; }

    /// <summary>左右两侧时可以拖动调整宽度，默认 true。</summary>
    [Parameter] public bool? Resizable { get; set; }

    /// <summary>拖动时的最小宽度（px），默认 48：只留一窄条（几乎关上，还能拖回来）。</summary>
    [Parameter] public double MinWidth { get; set; } = 48;

    /// <summary>宽度调整手柄的部件属性。</summary>
    [Parameter] public Attrs? PropsResizeHandle { get; set; }

    Bindable<double?> width { get => field ??= new(this, () => Width, () => WidthChanged); }

    bool Inline => Side is DrawerSide.Start or DrawerSide.End;

    bool CanResize => Inline && (Resizable ?? Config.Defaults.Get<DrawerDefaults>()?.Resizable ?? true);

    /// <inheritdoc/>
    protected override PartLayers<DialogPart> Parts => DialogStyles.Drawer(
        Side,
        Inline && width.Value is not null ? null : Size ?? Config.Defaults.Get<DrawerDefaults>()?.Size ?? ContainerSize.Xs,
        CanResize,
        Config.Styles);

    /// <inheritdoc/>
    protected override string? RootInlineStyle =>
        Inline && width.Value is { } px ? $"width:{px.ToString(CultureInfo.InvariantCulture)}px" : null;

    /// <inheritdoc/>
    protected override RenderFragment? Edge => CanResize ? RenderHandle : null;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // 双击回到打开时的宽度
        if (initialized) return;
        initialized = true;
        initialWidth = Width;
    }

    void RenderHandle(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<ResizeHandle>(0);
        builder.AddAttribute(1, "aria-label", Config.Lang.DrawerResize);
        builder.AddAttribute(2, nameof(ResizeHandle.MinWidth), MinWidth);
        builder.AddAttribute(3, nameof(ResizeHandle.DefaultWidth), initialWidth);
        builder.AddMultipleAttributes(4, PropsResizeHandle);
        builder.AddAttribute(5, nameof(ResizeHandle.Edge), Side == DrawerSide.End ? ResizeEdge.Start : ResizeEdge.End);
        builder.AddAttribute(6, nameof(ResizeHandle.Width), width.Value);
        builder.AddAttribute(7, nameof(ResizeHandle.WidthChanged), EventCallback.Factory.Create<double?>(this, next => width.SetAsync(next)));
        builder.CloseComponent();
    }
}
