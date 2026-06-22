namespace DocUtils;

internal sealed class DocxPartProcessor {
    private readonly DocxProcessingScope _scope;
    private readonly PartFailurePolicy _partFailurePolicy;

    public DocxPartProcessor(DocxProcessingScope scope, PartFailurePolicy partFailurePolicy) {
        _scope = scope;
        _partFailurePolicy = partFailurePolicy;
    }

    public List<DocxProcessingIssue> ProcessPartsInMemory(string templatePath, Action<DocxPartContext> body) {
        EnsureExists(templatePath);

        using var reader = new DocxPackageReader(templatePath);
        var partPaths = DocxPartLocator.LocatePaths(reader.AllEntryPaths(), _scope);
        if (!partPaths.Contains("word/document.xml", StringComparer.Ordinal)) {
            throw new DocxMissingRequiredPartException("word/document.xml");
        }

        var issues = new List<DocxProcessingIssue>();

        foreach (var partPath in partPaths) {
            try {
                var data = reader.ReadData(partPath);
                var document = DocxXmlDocument.Parse(data, partPath);
                body(new DocxPartContext {
                    Path = partPath,
                    XmlDocument = document,
                    Scope = _scope
                });
            } catch (Exception ex) {
                HandleIssue(issues, new DocxProcessingIssue(partPath, DocxProcessingOperation.Parse, ex.Message), ex);
            }
        }

        ThrowIfCollected(issues);
        return issues;
    }

    public List<DocxProcessingIssue> ProcessPartsOnDisk(string templatePath, string outputPath, Action<string, string> body) {
        EnsureExists(templatePath);

        using var reader = new DocxPackageReader(templatePath);
        var tempRoot = Path.Combine(Path.GetTempPath(), $"docutils-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try {
            DocxPackageExtractor.ExtractSafely(reader.Entries, tempRoot);
            var partPaths = DocxPartLocator.LocateFullPaths(tempRoot, _scope);
            var issues = new List<DocxProcessingIssue>();

            foreach (var partFullPath in partPaths) {
                var relativePath = DocxPartLocator.RelativePath(partFullPath, tempRoot);
                try {
                    body(relativePath, partFullPath);
                } catch (Exception ex) {
                    HandleIssue(issues, new DocxProcessingIssue(relativePath, DocxProcessingOperation.Mutate, ex.Message), ex);
                }
            }

            ThrowIfCollected(issues);
            DocxPackageWriter.Repack(tempRoot, outputPath);
            return issues;
        } finally {
            try {
                Directory.Delete(tempRoot, recursive: true);
            } catch {
            }
        }
    }

    private static void EnsureExists(string path) {
        if (!File.Exists(path)) {
            throw new DocxFileNotFoundException(path);
        }
    }

    private void HandleIssue(List<DocxProcessingIssue> issues, DocxProcessingIssue issue, Exception ex) {
        switch (_partFailurePolicy) {
            case PartFailurePolicy.FailFast:
                throw ex;
            case PartFailurePolicy.CollectAndThrow:
            case PartFailurePolicy.ContinueAndReport:
                issues.Add(issue);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ThrowIfCollected(List<DocxProcessingIssue> issues) {
        if (_partFailurePolicy == PartFailurePolicy.CollectAndThrow && issues.Count > 0) {
            throw new DocxPartialProcessingException(issues);
        }
    }
}
