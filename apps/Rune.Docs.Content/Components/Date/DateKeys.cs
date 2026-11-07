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

    /// <summary>日历网格（Calendar、RangeCalendar 与选择器弹出的日历）。</summary>
    public static readonly (string Key, string Action)[] Calendar =
    [
        ("← / →", "前一天 / 后一天（从右到左排版时反过来）；月份、年份视图里是前一格 / 后一格"),
        ("↑ / ↓", "上一周 / 下一周；月份、年份视图里是上一行 / 下一行"),
        ("PageUp / PageDown", "上一个月 / 下一个月；月份、年份视图里翻一页"),
        ("Shift+PageUp / Shift+PageDown", "上一年 / 下一年"),
        ("Home / End", "这周的第一天 / 最后一天（按地区的一周第一天）"),
        ("Enter / Space", "选中聚焦的日期；区间日历里先选开始、再选结束"),
    ];

    /// <summary>弹出选择器的开关（DatePicker、DateRangePicker、TimePicker）。</summary>
    public static readonly (string Key, string Action)[] Picker =
    [
        ("Alt+↓", "在输入框里：打开弹层，焦点落在已选（或今天）那一格"),
        ("Esc", "关闭弹层，焦点回到打开它的按钮"),
    ];
}
