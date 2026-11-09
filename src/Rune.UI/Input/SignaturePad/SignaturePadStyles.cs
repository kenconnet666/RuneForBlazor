using Rune.Styles;

namespace Rune.UI;

/// <summary>手写签名的部件（同 vue-tsx-pro 的 SignaturePad）。</summary>
public enum SignaturePadPart
{
    /// <summary>外壳（div，输入框的外壳）。</summary>
    Root,

    /// <summary>画布（canvas，role="img"）。</summary>
    Canvas,

    /// <summary>还没签时的提示。</summary>
    Placeholder,

    /// <summary>撤销、清空按钮那一组。</summary>
    Toolbar,
}

/// <summary>手写签名的样式（同 vue-tsx-pro 的 signaturePadStyles）：外壳用输入框的配方，底色与墨色是纸面。</summary>
public static class SignaturePadStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它
    internal static readonly CssVar<CssLength> HeightVar = CssVar.Length("rune-signature-height", "10rem");

    /// <summary>能不能写时各部件的默认层。</summary>
    public static PartLayers<SignaturePadPart> Of(bool writable, ComponentStyles styles) =>
        Recipe.Of("signature-pad", writable, styles, static (bool writable, SignaturePadPart part) => part switch
        {
            SignaturePadPart.Root => Sx.Of(SharedStyles.Paper, Root, writable ? Writable : null),
            SignaturePadPart.Canvas => Canvas,
            SignaturePadPart.Placeholder => Placeholder,
            _ => Toolbar,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Padding = 0,
        Overflow = Overflow.Hidden,
        // 输入框外壳按里面的 input 判断出错、不可用；画布不是 input，按外壳上的属性
        [When.Attribute("[data-invalid]")] = new()
        {
            BorderColor = Tones.Danger.Solid,
            OutlineColor = Tones.Danger.Solid,
            [When.Hover] = new() { BorderColor = Tones.Danger.Solid },
        },
        [When.Attribute("[data-disabled]")] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed },
    });

    static Style Writable => Css.Define(static () => new Style { Cursor = Cursor.Crosshair });

    // 触摸时不滚动页面、不缩放：手指的移动都是笔画
    static Style Canvas => Css.Define(static () => new Style
    {
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Height = HeightVar,
        TouchAction = TouchAction.None,
    });

    // 签了（JS 写 data-signed）就不显示
    static Style Placeholder => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        Color = Colors.TextPlaceholder,
        PointerEvents = PointerEvents.None,
        UserSelect = UserSelect.None,
        [When.Parent(When.Attribute("[data-signed]"))] = new() { Display = Display.None },
    });

    // 签了才显示
    static Style Toolbar => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetBlockStart = Space.Xs,
        InsetInlineEnd = Space.Xs,
        Display = Display.None,
        Gap = Space.Xs,
        [When.Parent(When.Attribute("[data-signed]"))] = new() { Display = Display.Flex },
    });
}
