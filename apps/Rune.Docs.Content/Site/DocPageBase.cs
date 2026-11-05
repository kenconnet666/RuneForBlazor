namespace Rune.Docs.Site;

/// <summary>文档页的基类：取自己的 <see cref="DocMetaAttribute"/>（标题、简介），交给 DocPage。</summary>
public abstract class DocPageBase : RuneComponent
{
    /// <summary>这一页的标题、简介、分类与顺序。</summary>
    protected DocMetaAttribute Meta => DocsCatalog.MetaOf(GetType());
}
