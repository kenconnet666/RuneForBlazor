namespace Rune.UI;

/// <summary>加载条的状态。</summary>
public enum LoadingBarStatus
{
    /// <summary>没在加载。</summary>
    Idle,

    /// <summary>加载中。</summary>
    Loading,

    /// <summary>完成：走满后淡出。</summary>
    Done,

    /// <summary>出错：危险色走满后淡出。</summary>
    Failed,
}

/// <summary>
/// 加载条的控制器（同 vue-tsx-pro 的 createLoadingBar）：组件内外都能用，交给 <see cref="LoadingBar"/> 显示。
/// <code>
/// readonly LoadingBarController bar = new();      // 应用里一个（如注册成服务），页面上放 &lt;LoadingBar Controller="bar" /&gt;
/// bar.Start(); await LoadAsync(); bar.Finish();
/// </code>
/// 这里只记状态；先快后慢地涨、很快完成时不显示、走满后淡出，都由 JS 初始化器在浏览器里做（Server 模式不用每 300ms 往返一次）。
/// </summary>
public sealed class LoadingBarController
{
    /// <summary>当前状态。</summary>
    public LoadingBarStatus Status { get; private set; }

    /// <summary>第几次开始：已经在走时再开始，浏览器据此重新计时。</summary>
    public int Run { get; private set; }

    /// <summary>状态变了（显示它的 LoadingBar 重新渲染）。</summary>
    public event Action? Changed;

    /// <summary>开始（已经在走时重新开始计时）。</summary>
    public void Start()
    {
        Status = LoadingBarStatus.Loading;
        Run++;
        Changed?.Invoke();
    }

    /// <summary>完成：走满后淡出。</summary>
    public void Finish() => End(LoadingBarStatus.Done);

    /// <summary>出错：变成危险色、走满后淡出。</summary>
    public void Error() => End(LoadingBarStatus.Failed);

    void End(LoadingBarStatus status)
    {
        if (Status != LoadingBarStatus.Loading) return;
        Status = status;
        Changed?.Invoke();
    }
}
