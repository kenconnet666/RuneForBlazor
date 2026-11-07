namespace Rune.UI;

/// <summary>
/// 代码高亮器（同 vue-tsx-pro 的 Highlighter）：代码与语言 → 只含 span 与 br 的 HTML，代码里的文字由它自己转义。
/// 配在 <see cref="UIConfig.Highlighter"/>（AddRuneUI 或 RuneProvider）时 CodeBlock 在渲染时直接用它上色；没配时在浏览器里用 Shiki。
/// </summary>
public delegate string CodeHighlighter(string code, string lang);
