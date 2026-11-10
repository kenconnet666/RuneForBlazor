using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 宿主可以改写的光标。浏览器里是张开、握紧的手（grab、grabbing）；WinUI 托管的 WebView2（MAUI 桌面应用）只认能对应到系统光标的 CSS 光标，
/// grab 会退回成箭头：桌面适配包把 <c>--rune-cursor-grab</c>、<c>--rune-cursor-grabbing</c> 改成 pointer（系统的手形）。
/// 能拖动的部件（滑块的把手、拖动排序与表格列、图片墙、裁剪框、看大图时平移、地球、模型）用它们，不直接写 Cursor.Grab。
/// </summary>
public static class Cursors
{
    /// <summary>能抓住拖动：默认 grab。</summary>
    public static readonly CssKeyword Grab = "var(--rune-cursor-grab, grab)";

    /// <summary>正在拖动：默认 grabbing。</summary>
    public static readonly CssKeyword Grabbing = "var(--rune-cursor-grabbing, grabbing)";
}
