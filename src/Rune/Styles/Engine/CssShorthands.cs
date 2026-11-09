namespace Rune.Styles;

/// <summary>
/// 简写重设哪些长属性（同 vue-tsx-pro 的 LONGHANDS）：合并样式时后写的简写要去掉前面写的、被它重设的长属性。
/// 只收组件里会写到的简写；名字前缀相同却不归它管的不算（border 不管 border-radius，outline 不管 outline-offset，flex 不管 flex-direction）。
/// </summary>
internal static class CssShorthands
{
    static readonly string[] Sides = ["top", "right", "bottom", "left"];

    // 每个简写只列往下一层，CoveredBy 逐层展开
    static readonly Dictionary<string, string[]> longhands = Build();

    static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string[]> covering = new(StringComparer.Ordinal);

    /// <summary>一个简写重设的全部长属性（逐层展开）；不是简写时为空。</summary>
    public static IReadOnlyList<string> CoveredBy(string property) =>
        covering.GetOrAdd(property, static name =>
        {
            if (!longhands.TryGetValue(name, out var direct)) return [];
            return [.. direct.Concat(direct.SelectMany(CoveredBy)).Distinct(StringComparer.Ordinal)];
        });

    static Dictionary<string, string[]> Build()
    {
        var table = new Dictionary<string, string[]>(StringComparer.Ordinal);
        Box("padding", "padding-");
        Box("margin", "margin-");
        table["inset"] = [.. Sides, "inset-block", "inset-inline"];
        table["inset-block"] = ["inset-block-start", "inset-block-end", "top", "bottom"];
        table["inset-inline"] = ["inset-inline-start", "inset-inline-end", "left", "right"];
        Box("border-width", "border-", "-width");
        Box("border-style", "border-", "-style");
        Box("border-color", "border-", "-color");
        table["border"] = ["border-width", "border-style", "border-color", "border-image"];
        foreach (var side in (string[])[.. Sides, "block-start", "block-end", "inline-start", "inline-end"])
            table[$"border-{side}"] = [$"border-{side}-width", $"border-{side}-style", $"border-{side}-color"];
        foreach (var axis in (string[])["block", "inline"])
            table[$"border-{axis}"] = [$"border-{axis}-start", $"border-{axis}-end", $"border-{axis}-width", $"border-{axis}-style", $"border-{axis}-color"];
        table["border-radius"] =
        [
            "border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius",
            "border-start-start-radius", "border-start-end-radius", "border-end-start-radius", "border-end-end-radius",
        ];
        table["border-image"] = ["border-image-source", "border-image-slice", "border-image-width", "border-image-outset", "border-image-repeat"];
        table["gap"] = ["row-gap", "column-gap"];
        table["overflow"] = ["overflow-x", "overflow-y", "overflow-block", "overflow-inline"];
        table["outline"] = ["outline-color", "outline-style", "outline-width"];
        table["flex"] = ["flex-grow", "flex-shrink", "flex-basis"];
        table["flex-flow"] = ["flex-direction", "flex-wrap"];
        table["place-items"] = ["align-items", "justify-items"];
        table["place-content"] = ["align-content", "justify-content"];
        table["place-self"] = ["align-self", "justify-self"];
        table["background"] =
        [
            "background-color", "background-image", "background-position", "background-size",
            "background-repeat", "background-attachment", "background-origin", "background-clip",
        ];
        table["background-position"] = ["background-position-x", "background-position-y"];
        table["text-decoration"] = ["text-decoration-line", "text-decoration-color", "text-decoration-style", "text-decoration-thickness"];
        table["list-style"] = ["list-style-type", "list-style-position", "list-style-image"];
        table["font"] = ["font-style", "font-variant", "font-weight", "font-stretch", "font-size", "line-height", "font-family"];
        table["transition"] = ["transition-property", "transition-duration", "transition-timing-function", "transition-delay", "transition-behavior"];
        table["animation"] =
        [
            "animation-name", "animation-duration", "animation-timing-function", "animation-delay",
            "animation-iteration-count", "animation-direction", "animation-fill-mode", "animation-play-state",
        ];
        table["grid-template"] = ["grid-template-rows", "grid-template-columns", "grid-template-areas"];
        table["grid-area"] = ["grid-row", "grid-column"];
        table["grid-row"] = ["grid-row-start", "grid-row-end"];
        table["grid-column"] = ["grid-column-start", "grid-column-end"];
        table["columns"] = ["column-width", "column-count"];
        return table;

        // padding、margin 这类四边的简写：四个物理边、两个逻辑方向（再往下是起止两边）
        void Box(string name, string prefix, string suffix = "")
        {
            table[name] = [.. Sides.Select(side => prefix + side + suffix), $"{prefix}block{suffix}", $"{prefix}inline{suffix}"];
            table[$"{prefix}block{suffix}"] = [$"{prefix}block-start{suffix}", $"{prefix}block-end{suffix}", $"{prefix}top{suffix}", $"{prefix}bottom{suffix}"];
            table[$"{prefix}inline{suffix}"] = [$"{prefix}inline-start{suffix}", $"{prefix}inline-end{suffix}", $"{prefix}left{suffix}", $"{prefix}right{suffix}"];
        }
    }
}
