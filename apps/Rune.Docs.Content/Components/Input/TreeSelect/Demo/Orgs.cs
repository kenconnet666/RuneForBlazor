using Rune.UI;

namespace Rune.Docs.Components.Input.TreeSelect.Demo;

/// <summary>示例用的组织机构（虚构）：公司 → 中心 / 部 → 团队。</summary>
public sealed record Org(string Id, string Name, IReadOnlyList<Org>? Children = null)
{
    public static readonly Org[] All =
    [
        new("hq", "总部",
        [
            new("pmo", "项目管理办公室（PMO）", [new("pmo-plan", "计划团队"), new("pmo-quality", "质量团队")]),
            new("finance", "财务部", [new("finance-budget", "预算团队"), new("finance-settle", "结算团队")]),
        ]),
        new("rd", "研发中心", [new("rd-frontend", "前端团队"), new("rd-backend", "后端团队")]),
        new("mfg", "生产中心", [new("mfg-assembly", "装配团队"), new("mfg-testing", "测试团队"), new("mfg-packing", "包装团队")]),
        new("purchase", "采购部"),
    ];

    public static readonly TreeAccessors<Org> Accessors = new(org => org.Id, org => org.Children);

    /// <summary>只留命中的节点与它们的上级（裁掉没有命中的分枝）。</summary>
    public static IReadOnlyList<Org> Prune(IEnumerable<Org> nodes, Func<Org, bool> hit) => [.. nodes.SelectMany<Org, Org>(org =>
    {
        var children = Prune(org.Children ?? [], hit);
        return !hit(org) && children.Count == 0 ? [] : [org with { Children = children.Count > 0 ? children : null }];
    })];
}
