namespace Rune.UI;

// DataTable 的行选择：勾选一行、全选（只算能勾选的行）、单选时各行的 Radio。
public partial class DataTable<TItem>
{
    bool IsSelectable(TItem item) => IsRowSelectable?.Invoke(item) ?? true;

    Task ToggleRowAsync(string key)
    {
        var mode = SelectionMode ?? UI.SelectionMode.Multiple;
        return ListState.Toggle(selected.Value, key, mode, required: false) is { } next ? selected.SetAsync(next) : Task.CompletedTask;
    }

    // 用集合查找：逐个 Contains 是 O(n²)，十万行全选会卡住页面；不能勾选的行跳过
    Task ToggleAllAsync()
    {
        var keys = Rows.Where(IsSelectable).Select(GetKey).ToList();
        var current = SelectedKeys;
        if (keys.Count == 0 || !keys.All(current.Contains)) return selected.SetAsync([.. selected.Value.Union(keys)]);
        var shown = keys.ToHashSet();
        return selected.SetAsync([.. selected.Value.Where(key => !shown.Contains(key))]);
    }

    (bool All, bool Some, bool Any) SelectAllState
    {
        get
        {
            var current = SelectedKeys;
            var total = 0;
            var picked = 0;
            foreach (var item in Rows)
            {
                if (!IsSelectable(item)) continue;
                total++;
                if (current.Contains(GetKey(item))) picked++;
            }
            return (total > 0 && picked == total, picked > 0 && picked < total, total > 0);
        }
    }

    /// <summary>单选时各行的 Radio 接入这一组：同一个 name（方向键在可见的行之间移动），选中即写回；选中的行变了时换一个对象，各行的 Radio 随之更新。</summary>
    SelectionRadios Radios => radios.Get(selected.Value.FirstOrDefault(), this, static (current, owner) => new(owner.radioName, current, owner.ToggleRowAsync));

    sealed class SelectionRadios(string name, string? current, Func<string, Task> select) : IRadioGroup<string>
    {
        public string GroupName => name;

        public Size? Size => null;

        public Tone Tone => Tone.Accent;

        public bool GroupDisabled => false;

        public bool GroupReadOnly => false;

        public bool IsSelected(string value) => value == current;

        public Task SelectAsync(string value) => select(value);
    }
}
