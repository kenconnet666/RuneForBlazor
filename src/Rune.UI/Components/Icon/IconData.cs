namespace Rune.UI;

/// <summary>
/// 一个图标的数据：SVG 的内部元素（path、circle……），由 <see cref="Icon"/> 套上 svg 元素渲染。
/// 线条图标（Lucide 的风格）：不填充、描边用当前文字颜色。
/// <code>&lt;Icon Data="Lucide.Plus" /&gt;</code>
/// 自定义图标直接创建：<c>new IconData("my-logo", "&lt;path d=\"…\"/&gt;")</c>。
/// </summary>
/// <param name="Name">名字（连字符小写，调试与测试用，写在 data-icon 上）。</param>
/// <param name="Body">svg 里面的元素标记；渲染时原样输出，只放可信的图标数据。</param>
/// <param name="ViewBox">坐标系，默认 Lucide 的 24×24。</param>
public sealed record IconData(string Name, string Body, string ViewBox = "0 0 24 24");
