using Rune.UI;

namespace Rune.Docs.Components.Input.PeoplePicker.Demo;

/// <summary>示例用的组织架构：公司下面三个部门，采购部再分两个组；另有四个角色。</summary>
static class OrgData
{
    static PeopleNode Person(string id, string name, string description) => new(id, PeopleType.User, name) { Description = description };

    static PeopleNode Department(string id, string name, string description, params PeopleNode[] children) =>
        new(id, PeopleType.Department, name) { Description = description, Children = children };

    public static readonly IReadOnlyList<PeopleNode> Org =
    [
        Department("d-buy", "采购部", "公司",
            Department("d-buy-source", "寻源组", "采购部", Person("u-zhao", "赵一凡", "采购部 · 寻源工程师"), Person("u-sun", "孙悦", "采购部 · 寻源工程师")),
            Department("d-buy-exec", "执行组", "采购部", Person("u-zhou", "周婷", "采购部 · 采购专员"), Person("u-wu", "吴昊", "采购部 · 采购专员")),
            Person("u-lin", "林晓", "采购部 · 经理")),
        Department("d-qa", "质量部", "公司", Person("u-chen", "陈思雨", "质量部 · 经理"), Person("u-li", "李想", "质量部 · 质量工程师")),
        Department("d-fin", "财务部", "公司",
            Person("u-wang", "王建国", "财务部 · 经理"), Person("u-wu-jia", "吴佳", "财务部 · 会计"), Person("u-zheng", "郑一鸣", "财务部 · 出纳")),
    ];

    public static readonly IReadOnlyList<PeopleEntry> Roles =
    [
        new("r-buyer", PeopleType.Role, "采购专员") { Description = "采购部" },
        new("r-buy-manager", PeopleType.Role, "采购经理") { Description = "采购部" },
        new("r-qa", PeopleType.Role, "质量工程师") { Description = "质量部" },
        new("r-cashier", PeopleType.Role, "出纳") { Description = "财务部" },
    ];

    public static readonly PeopleSource Source = PeopleSource.Local(Org, Roles);
}
