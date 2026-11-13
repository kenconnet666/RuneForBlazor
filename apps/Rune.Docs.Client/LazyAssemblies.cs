using Rune.Docs.Site;

namespace Rune.Docs.Client;

/// <summary>
/// 只有部分页面用到的程序集（项目文件里的 BlazorWebAssemblyLazyLoad）：WebAssembly 启动时不下载，进入用到它们的页面前再加载。
/// 图表页（分类 Charts）要图表，其中只有 MapChart 页要地图数据（171 KB，别的图表页不下载）；3D 页（分类 Three）要 Rune.Three，
/// Cascader 页的地区示例要省市区数据。别的页面不能用到这些程序集里的类型
/// （单元测试 LazyAssemblyTests 检查内容项目的源码）。
/// </summary>
static class LazyAssemblies
{
    static readonly string[] Charts = ["Rune.Charts.wasm"];
    static readonly string[] Maps = ["Rune.Charts.wasm", "Rune.Charts.Maps.wasm"];
    static readonly string[] Area = ["Rune.UI.Area.wasm"];
    static readonly string[] Three = ["Rune.Three.wasm"];

    /// <summary>打开这个地址（相对站点根，如 components/line-chart、frame/app-shell/admin）前要加载的程序集。</summary>
    public static IReadOnlyList<string> For(string path)
    {
        var parts = path.Split('?', '#')[0].Trim('/').Split('/');
        // 整页示例 frame/{页面}/{名字} 跟着它所在的页面
        var page = parts is ["frame", var slug, ..] ? DocsCatalog.Find(slug) : DocsCatalog.FindByPath(string.Join('/', parts));
        return page switch
        {
            { Slug: "map-chart" } => Maps,
            { Meta.Category: DocCategory.Charts } => Charts,
            { Meta.Category: DocCategory.Three } => Three,
            { Slug: "cascader" } => Area,
            _ => [],
        };
    }
}
