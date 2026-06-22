namespace DocUtils;

public sealed class DocxTemplateScanner {
    public DocxScanReport Scan(
        string templatePath,
        DocxProcessingScope? scope = null,
        DocxPlaceholderPattern? pattern = null
    ) {
        var effectiveScope = scope ?? new DocxProcessingScope();
        var processor = new DocxPartProcessor(effectiveScope, PartFailurePolicy.ContinueAndReport);
        var matcher = new DocxPlaceholderMatcher(pattern ?? new DocxPlaceholderPattern());
        var report = new DocxScanReport();

        var issues = processor.ProcessPartsInMemory(templatePath, context => {
            var paragraphs = DocxParagraphLocator.FindParagraphs(context.XmlDocument);
            var partHasKeys = false;

            foreach (var paragraph in paragraphs) {
                var projection = ParagraphTextProjection.Build(paragraph, context.Scope.IncludeFieldInstructionText);
                if (projection.FullText.Length == 0) {
                    continue;
                }

                foreach (var match in matcher.FindMatches(projection.FullText)) {
                    if (report.FoundKeys.Add(match.Key)) {
                        report.OrderedKeys.Add(match.Key);
                    }

                    report.Occurrences[match.Key] = report.Occurrences.GetValueOrDefault(match.Key) + 1;

                    if (!report.PartsByKey.TryGetValue(match.Key, out var parts)) {
                        parts = [];
                        report.PartsByKey[match.Key] = parts;
                    }

                    parts.Add(context.Path);
                    partHasKeys = true;
                }
            }

            if (partHasKeys) {
                report.ProcessedParts.Add(context.Path);
            }
        });

        report.Issues = issues;
        return report;
    }

    public IReadOnlyList<string> ScanKeys(
        string templatePath,
        DocxProcessingScope? scope = null,
        DocxPlaceholderPattern? pattern = null
    ) {
        return Scan(templatePath, scope, pattern).OrderedKeys;
    }
}
