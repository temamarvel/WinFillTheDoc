namespace DocUtils;

internal static class DocxPartLocator {
    public static IReadOnlyList<string> LocatePaths(IEnumerable<string> allPaths, DocxProcessingScope scope) {
        var paths = allPaths.ToHashSet(StringComparer.Ordinal);

        return scope.Selection switch {
            DocxProcessingScope.PartSelection.Standard => LocateStandard(paths, scope),
            DocxProcessingScope.PartSelection.AllWordXml => paths
                .Where(static path => path.StartsWith("word/", StringComparison.Ordinal) &&
                                      path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                                      !path.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
                .OrderBy(static path => path, StringComparer.Ordinal)
                .ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(scope.Selection), scope.Selection, null)
        };
    }

    public static IReadOnlyList<string> LocateFullPaths(string rootDirectory, DocxProcessingScope scope) {
        var mainDocumentPath = Path.Combine(rootDirectory, "word", "document.xml");
        if (!File.Exists(mainDocumentPath)) {
            throw new DocxMissingRequiredPartException("word/document.xml");
        }

        return scope.Selection switch {
            DocxProcessingScope.PartSelection.Standard => LocateStandardFullPaths(rootDirectory, scope),
            DocxProcessingScope.PartSelection.AllWordXml => Directory
                .EnumerateFiles(Path.Combine(rootDirectory, "word"), "*.xml", SearchOption.AllDirectories)
                .Where(static path => !path.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(scope.Selection), scope.Selection, null)
        };
    }

    public static string RelativePath(string fullPath, string rootDirectory) =>
        Path.GetRelativePath(rootDirectory, fullPath).Replace('\\', '/');

    private static string[] LocateStandard(HashSet<string> allPaths, DocxProcessingScope scope) {
        var result = new List<string>();

        if (allPaths.Contains("word/document.xml")) {
            result.Add("word/document.xml");
        }

        result.AddRange(allPaths.Where(static path => path.StartsWith("word/header", StringComparison.Ordinal) && path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.Ordinal));
        result.AddRange(allPaths.Where(static path => path.StartsWith("word/footer", StringComparison.Ordinal) && path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.Ordinal));

        if (scope.IncludeFootnotes && allPaths.Contains("word/footnotes.xml")) {
            result.Add("word/footnotes.xml");
        }

        if (scope.IncludeEndnotes && allPaths.Contains("word/endnotes.xml")) {
            result.Add("word/endnotes.xml");
        }

        if (scope.IncludeComments && allPaths.Contains("word/comments.xml")) {
            result.Add("word/comments.xml");
        }

        return result.ToArray();
    }

    private static string[] LocateStandardFullPaths(string rootDirectory, DocxProcessingScope scope) {
        var wordDirectory = Path.Combine(rootDirectory, "word");
        var result = new List<string> { Path.Combine(wordDirectory, "document.xml") };

        result.AddRange(Directory.EnumerateFiles(wordDirectory, "header*.xml", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase));
        result.AddRange(Directory.EnumerateFiles(wordDirectory, "footer*.xml", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase));

        MaybeAdd(result, Path.Combine(wordDirectory, "footnotes.xml"), scope.IncludeFootnotes);
        MaybeAdd(result, Path.Combine(wordDirectory, "endnotes.xml"), scope.IncludeEndnotes);
        MaybeAdd(result, Path.Combine(wordDirectory, "comments.xml"), scope.IncludeComments);

        return result.ToArray();
    }

    private static void MaybeAdd(List<string> list, string path, bool include) {
        if (include && File.Exists(path)) {
            list.Add(path);
        }
    }
}
