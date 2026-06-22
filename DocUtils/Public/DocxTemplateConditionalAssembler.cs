namespace DocUtils;

public sealed class DocxTemplateConditionalAssembler {
    public DocxConditionalAssemblyReport Assemble(
        string templatePath,
        string outputPath,
        IReadOnlyDictionary<string, string> values,
        DocxConditionalAssemblyOptions? options = null
    ) {
        options ??= new DocxConditionalAssemblyOptions();

        var processor = new DocxPartProcessor(options.Scope, PartFailurePolicy.FailFast);
        var report = new DocxConditionalAssemblyReport();

        processor.ProcessPartsOnDisk(templatePath, outputPath, (partPath, partFullPath) => {
            byte[] data;
            try {
                data = File.ReadAllBytes(partFullPath);
            } catch (Exception ex) {
                throw new DocxReadPartException(partPath, ex.Message, ex);
            }

            var document = DocxXmlDocument.Parse(data, partPath);
            var resolver = new DocxConditionalBlockResolver(values, options, partPath);
            var result = resolver.Resolve(document);

            if (result.Infos.Count == 0) {
                return;
            }

            DocxXmlDocument.Save(document, partFullPath, partPath);
            report.ProcessedParts.Add(partPath);
            report.ResolvedSwitches.AddRange(result.Infos);
            report.RemovedControlMarkersCount += result.RemovedControlMarkers;
            report.RemovedBlocksCount += result.RemovedBlocks;
        });

        return report;
    }
}
