using Rune.Styles;

namespace Rune.Tests.Components.TestComponents;

public static class PlainHostStyles
{
    public static Style Badge => Css.Define(static () => new Style { Color = "teal" });
}
