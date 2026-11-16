using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary><see cref="StepsForm"/> 的一步。</summary>
/// <param name="Title">步骤条上的标题。</param>
/// <param name="Fields">这一步的字段（模型上的属性名，同 nameof(Model.Company)）：“下一步”只校验它们，出错时这一步标成出错。</param>
/// <param name="Content">这一步的内容：Field 们。</param>
public sealed record FormStep(string Title, IReadOnlyList<string> Fields, RenderFragment Content)
{
    /// <summary>步骤条上的说明。</summary>
    public string? Description { get; init; }
}

/// <summary>分步表单的部件。</summary>
public enum StepsFormPart
{
    /// <summary>步骤条、内容、按钮的外层。</summary>
    Root,

    /// <summary>步骤条（Steps）。</summary>
    Steps,

    /// <summary>这一步的内容。</summary>
    Panel,

    /// <summary>按钮行（Flex）。</summary>
    Actions,

    /// <summary>上一步。</summary>
    Previous,

    /// <summary>下一步。</summary>
    Next,

    /// <summary>提交。</summary>
    Submit,
}

/// <summary>分步表单的样式（同 vue-tsx-pro 的 stepsFormStyles）。</summary>
public static class StepsFormStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<StepsFormPart> Of(ComponentStyles styles) =>
        Recipe.Of("steps-form", styles, static (StepsFormPart part) => part switch
        {
            StepsFormPart.Root => Root,
            StepsFormPart.Panel => Panel,
            _ => Sx.Of((Style?)null),
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Lg });

    // 换步后焦点在这里：不画焦点环（内容本身不是控件），读屏念出它的名称
    static Style Panel => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Md,
        OutlineStyle = OutlineStyle.None,
    });
}
