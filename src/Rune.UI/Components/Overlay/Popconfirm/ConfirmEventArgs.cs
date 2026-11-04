namespace Rune.UI;

/// <summary>确认事件的参数（Popconfirm 的 OnConfirm）：处理时发现不能继续（校验没过、请求失败）就设 <see cref="KeepOpen"/>。</summary>
public sealed class ConfirmEventArgs : EventArgs
{
    /// <summary>保持打开，不关闭（由调用方提示原因）。</summary>
    public bool KeepOpen { get; set; }
}
