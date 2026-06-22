using System.Text.RegularExpressions;

namespace DocUtils;

public sealed class DocxPlaceholderPattern {
    public string Prefix { get; init; } = "<!";
    public string Suffix { get; init; } = "!>";
    public string KeyPattern { get; init; } = "[A-Za-z0-9_]+";

    internal Regex BuildRegex() {
        var prefix = Regex.Escape(Prefix);
        var suffix = Regex.Escape(Suffix);
        return new Regex($"{prefix}({KeyPattern}){suffix}", RegexOptions.Compiled);
    }
}
