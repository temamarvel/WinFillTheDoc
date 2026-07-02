namespace WinFillTheDoc.Application.Services;

public static class GitHubReleaseVersionParser
{
    public static string NormalizeVersion(string raw)
    {
        var trimmed = raw.Trim();
        return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? trimmed[1..] : trimmed;
    }

    public static bool IsVersionGreaterThan(string left, string right) => CompareVersions(left, right) > 0;

    public static int CompareVersions(string left, string right)
    {
        if (!TryParse(NormalizeVersion(left), out var leftParts) || !TryParse(NormalizeVersion(right), out var rightParts))
            return 0;

        var length = Math.Max(leftParts.Count, rightParts.Count);
        for (var index = 0; index < length; index++)
        {
            var leftValue = index < leftParts.Count ? leftParts[index] : 0;
            var rightValue = index < rightParts.Count ? rightParts[index] : 0;
            var comparison = leftValue.CompareTo(rightValue);
            if (comparison != 0) return comparison;
        }

        return 0;
    }

    private static bool TryParse(string value, out IReadOnlyList<int> parts)
    {
        parts = [];
        var rawParts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rawParts.Length == 0) return false;

        var parsed = new List<int>();
        foreach (var rawPart in rawParts)
        {
            var numericPrefix = new string(rawPart.TakeWhile(char.IsDigit).ToArray());
            if (numericPrefix.Length == 0 || !int.TryParse(numericPrefix, out var part)) return false;
            parsed.Add(part);
        }

        parts = parsed;
        return true;
    }
}
