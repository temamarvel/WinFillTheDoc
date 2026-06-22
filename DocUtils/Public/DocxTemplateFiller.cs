namespace DocUtils;

public sealed class DocxTemplateFiller {
    public DocxFillReport Fill(
        string templatePath,
        string outputPath,
        IReadOnlyDictionary<string, string> values,
        DocxFillOptions? options = null
    ) {
        options ??= new DocxFillOptions();

        var processor = new DocxPartProcessor(options.Scope, options.PartFailurePolicy);
        var matcher = new DocxPlaceholderMatcher(options.Pattern);
        var resolver = new DocxPlaceholderResolver(values, options.MissingKeyPolicy);
        var styleCleaner = new ParagraphStyleCleaner(options.ReplacementStylePolicy);
        var report = new DocxFillReport();

        var issues = processor.ProcessPartsOnDisk(templatePath, outputPath, (partPath, partFullPath) => {
            byte[] data;
            try {
                data = File.ReadAllBytes(partFullPath);
            } catch (Exception ex) {
                throw new DocxReadPartException(partPath, ex.Message, ex);
            }

            var document = DocxXmlDocument.Parse(data, partPath);
            var paragraphs = DocxParagraphLocator.FindParagraphs(document);
            var didChangeAnyParagraph = false;

            foreach (var paragraph in paragraphs) {
                var projection = ParagraphTextProjection.Build(paragraph, options.Scope.IncludeFieldInstructionText);
                if (projection.TextNodes.Count == 0) {
                    continue;
                }

                var matches = matcher.FindMatches(projection.FullText);
                if (matches.Count == 0) {
                    continue;
                }

                var didChangeCurrentParagraph = false;

                for (var index = matches.Count - 1; index >= 0; index--) {
                    var match = matches[index];
                    report.FoundKeys.Add(match.Key);

                    var resolution = resolver.Resolve(match.Key);
                    string? replacement = null;

                    switch (resolution.Kind) {
                        case PlaceholderResolutionKind.Replace:
                            replacement = resolution.Value;
                            report.ReplacedKeys.Add(match.Key);
                            break;
                        case PlaceholderResolutionKind.KeepOriginal:
                            break;
                        case PlaceholderResolutionKind.ReplaceWithEmptyString:
                            replacement = string.Empty;
                            report.ReplacedKeys.Add(match.Key);
                            break;
                        case PlaceholderResolutionKind.MissingRequired:
                            report.MissingKeys.Add(resolution.Key!);
                            break;
                    }

                    if (replacement is null) {
                        continue;
                    }

                    var start = projection.OffsetMapper.LocateStart(match.StartIndex);
                    var end = projection.OffsetMapper.LocateEnd(match.EndIndex, projection.FullText.Length);
                    if (start is null || end is null) {
                        continue;
                    }

                    ParagraphTextMutator.ApplyReplacement(projection.TextNodes, start, end, replacement);
                    styleCleaner.CleanAfterReplacement(projection.TextNodes, start.NodeIndex, end.NodeIndex);
                    report.ReplacementsCount++;
                    didChangeCurrentParagraph = true;
                }

                if (didChangeCurrentParagraph) {
                    ParagraphMutationCommitter.CommitChanges(projection.TextNodes);
                    didChangeAnyParagraph = true;
                }
            }

            if (didChangeAnyParagraph) {
                report.ProcessedParts.Add(partPath);
                DocxXmlDocument.Save(document, partFullPath, partPath);
            }
        });

        report.Issues = issues;

        if (options.MissingKeyPolicy == MissingKeyPolicy.Error && report.MissingKeys.Count > 0) {
            throw new DocxMissingPlaceholderValuesException(report.MissingKeys);
        }

        return report;
    }
}
