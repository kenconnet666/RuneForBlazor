namespace Rune.UI;

/// <summary>
/// 自动完成（同 vue-tsx-pro 的 Autocomplete）：值就是输入框里的文字，列表只是建议（邮箱后缀、常用地址、供应商名称）。
/// 选一项等于把它的文字填进来；不选也行，输入什么就是什么。要从固定的选项里选一个（值是选项的 value）时用 Combobox。
/// <code>
/// &lt;Field Label="邮箱"&gt;&lt;Autocomplete @bind-Value="email" Items="Suggest(email)" LocalFilter="false" /&gt;&lt;/Field&gt;
/// &lt;Autocomplete TItem="string" @bind-Value="keyword" Load="SuggestAsync" aria-label="搜索" /&gt;
/// </code>
/// 建在 Combobox 上：允许自由输入、不显示展开按钮，没有可选的建议时不弹出列表；↓ 打开建议，↑↓ 移动，Enter 填入，
/// Esc 关闭（关闭时再按 Esc 清空）。写了 Name 时表单提交的是输入框里的文字。
/// </summary>
/// <typeparam name="TItem">建议的数据：ListboxOption、字符串，或任意数据加 ToOption。</typeparam>
public sealed class Autocomplete<TItem> : Combobox<TItem>
{
    /// <inheritdoc/>
    protected override bool TextIsValue => true;
}
