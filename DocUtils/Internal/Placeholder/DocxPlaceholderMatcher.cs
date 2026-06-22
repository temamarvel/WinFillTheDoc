namespace DocUtils;

internal sealed record PlaceholderMatch(int StartIndex, int EndIndex, string Key);

internal sealed class DocxPlaceholderMatcher {
    private readonly DocxPlaceholderPattern _pattern;

    public DocxPlaceholderMatcher(DocxPlaceholderPattern pattern) {
        _pattern = pattern;
    }

    public List<PlaceholderMatch> FindMatches(string text) {
        var matches = _pattern.BuildRegex().Matches(text);
        var result = new List<PlaceholderMatch>(matches.Count);

        foreach (System.Text.RegularExpressions.Match match in matches) {
            if (match.Groups.Count < 2) {
                continue;
            }

            result.Add(new PlaceholderMatch(
                match.Index,
                match.Index + match.Length,
                match.Groups[1].Value));
        }

        return result;
    }
}
