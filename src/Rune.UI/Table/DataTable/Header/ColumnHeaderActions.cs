namespace Rune.UI;

/// <summary>列菜单里的一项操作（同 vue-tsx-pro 的 ColumnMenuActions）。</summary>
public enum ColumnMenuAction
{
    /// <summary>按这一列正序。</summary>
    SortAscending,

    /// <summary>按这一列倒序。</summary>
    SortDescending,

    /// <summary>取消这一列的排序。</summary>
    Unsort,

    /// <summary>固定在开头。</summary>
    PinStart,

    /// <summary>固定在结尾。</summary>
    PinEnd,

    /// <summary>取消固定。</summary>
    Unpin,

    /// <summary>往前移一列。</summary>
    MoveBackward,

    /// <summary>往后移一列。</summary>
    MoveForward,

    /// <summary>隐藏这一列。</summary>
    Hide,
}

/// <summary>列的表头筛选方式。</summary>
public enum ColumnFilterKind
{
    /// <summary>勾选值（可以多选）：列出这一列出现过的值（状态列按状态的顺序、显示状态名），或列的 FilterOptions。</summary>
    Values,

    /// <summary>输入关键字：这一列的文字包含它（按语言忽略大小写与重音）。</summary>
    Keyword,
}
