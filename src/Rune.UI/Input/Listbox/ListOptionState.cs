namespace Rune.UI;

/// <summary>自定义选项内容拿到的东西：原始数据、它的选项信息与是否已选。</summary>
/// <param name="Item">Items 里的那一项。</param>
/// <param name="Option">它的选项信息（值、文字……）。</param>
/// <param name="Selected">已选。</param>
public readonly record struct ListOptionState<TItem>(TItem Item, ListboxOption Option, bool Selected);
