using Rune.Styles;

namespace Rune.Tests.TestComponents;

public static class PlainHostStyles
{
    public static Style Badge => Css.Define(static () => new Style { Color = "teal" });
}
