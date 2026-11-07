namespace Rune.UI;

/// <summary>
/// Radio 所在的一组（级联值）：RadioGroup，或自己管选中的组件（DataTable 单选时的勾选列，同 vue-tsx-pro 的 RadioGroupContext）。
/// </summary>
interface IRadioGroup<TValue>
{
    /// <summary>同组 radio 共用的 name。</summary>
    string GroupName { get; }

    /// <summary>字号档位。</summary>
    Size? Size { get; }

    /// <summary>选中时的色调。</summary>
    Tone Tone { get; }

    /// <summary>卡片外观。</summary>
    bool Card { get; }

    /// <summary>整组不可用。</summary>
    bool GroupDisabled { get; }

    /// <summary>整组只读：能聚焦，选中项不能改。</summary>
    bool GroupReadOnly { get; }

    /// <summary>这个值是否选中。</summary>
    bool IsSelected(TValue value);

    /// <summary>选中一个值。</summary>
    Task SelectAsync(TValue value);
}
