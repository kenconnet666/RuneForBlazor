namespace Rune.UI;

/// <summary>文档站用到、Lucide 里没有的图标（同 vue-tsx-pro 的 GithubIcon）。</summary>
public static class DocsKitIcons
{
    /// <summary>GitHub 标志：Lucide 已移除品牌图标，按它原来的描线画法写（ISC 许可）。文档站顶栏的仓库链接用。</summary>
    public static IconData Github => field ??= new("github",
        "<path d=\"M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4\"/>" +
        "<path d=\"M9 18c-4.51 2-5-2-7-2\"/>");
}
