namespace Rune.Demo.Client.Infrastructure;

/// <summary>
/// 把一个组件登记成演示：演示页按它生成目录，并能以四种渲染模式打开（<c>/server/collapsible</c>）。
/// 新增演示只要在 Demos 里放一个组件并写上：
/// <code>@attribute [ComponentDemo("collapsible", "折叠面板", Group = "展示")]</code>
/// </summary>
/// <param name="slug">地址里的名字（连字符小写，全局唯一）。</param>
/// <param name="title">目录里显示的名字。</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ComponentDemoAttribute(string slug, string title) : Attribute
{
    /// <summary>地址里的名字。</summary>
    public string Slug { get; } = slug;

    /// <summary>目录里显示的名字。</summary>
    public string Title { get; } = title;

    /// <summary>目录里的分组，默认“组件”。</summary>
    public string Group { get; init; } = "组件";

    /// <summary>同组内的排序，小的在前。</summary>
    public int Order { get; init; }
}
