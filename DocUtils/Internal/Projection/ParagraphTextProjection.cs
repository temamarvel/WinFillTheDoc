using System.Xml;

namespace DocUtils;

internal sealed class ParagraphTextProjection {
    public required XmlElement ParagraphNode { get; init; }
    public required List<DocxTextNode> TextNodes { get; init; }
    public required string FullText { get; init; }
    public required TextOffsetMapper OffsetMapper { get; init; }

    public static ParagraphTextProjection Build(XmlElement paragraph, bool includeFieldInstructionText) {
        var textNodes = DocxTextNodeCollector.CollectEditableTextNodes(paragraph, includeFieldInstructionText);
        var fullText = string.Concat(textNodes.Select(static x => x.Text));
        var lengths = textNodes.Select(static x => x.Text.Length).ToArray();

        return new ParagraphTextProjection {
            ParagraphNode = paragraph,
            TextNodes = textNodes,
            FullText = fullText,
            OffsetMapper = new TextOffsetMapper(TextOffsetMapper.ComputePrefixSums(lengths))
        };
    }
}
