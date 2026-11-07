namespace Rune.Docs.Components.Date;

/// <summary>日期类组件共用的按键说明（KeyboardReference 的 Keys，同 vue-tsx-pro 文档的 date/keyboard.ts）。</summary>
static class DateKeys
{
    /// <summary>分段输入（DateField、TimeField 与各选择器的输入框）。</summary>
    public static readonly (string Key, string Action)[] Segments =
    [
        ("数字键", "在聚焦的一段里键入；一段填满（或不可能再多一位）时跳到下一段"),
        ("← / →", "移到上一段 / 下一段（从右到左排版时反过来）"),
        ("↑ / ↓", "这一段加一 / 减一，到头时回绕"),
        ("PageUp / PageDown", "大步加减（年 ±5、月 ±2、日 ±7、时 ±2、分秒 ±15）"),
        ("Home / End", "这一段取最小 / 最大值"),
        ("Backspace / Delete", "删一位；删空后回到上一段"),
    ];
}
