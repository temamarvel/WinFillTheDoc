using System.Xml;

namespace DocUtils;

internal sealed class DocxPartContext {
    public required string Path { get; init; }
    public required XmlDocument XmlDocument { get; init; }
    public required DocxProcessingScope Scope { get; init; }
}
