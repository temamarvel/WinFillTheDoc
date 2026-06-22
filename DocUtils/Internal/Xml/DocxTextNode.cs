using System.Xml;

namespace DocUtils;

internal enum DocxTextNodeKind {
    Text,
    InstrText
}

internal sealed class DocxTextNode {
    public required XmlElement Element { get; init; }
    public required DocxTextNodeKind Kind { get; init; }
    public required string Text { get; set; }
    public bool IsDirty { get; set; }
}

internal static class DocxTextNodeCollector {
    public static List<DocxTextNode> CollectEditableTextNodes(XmlElement paragraph, bool includeFieldInstructionText) {
        var nodes = paragraph.SelectNodes(".//*[local-name()='t' or local-name()='instrText']")
            ?.OfType<XmlElement>() ?? [];

        var result = new List<DocxTextNode>();

        foreach (var element in nodes) {
            var localName = element.LocalName;
            if (localName == "instrText" && !includeFieldInstructionText) {
                continue;
            }

            result.Add(new DocxTextNode {
                Element = element,
                Kind = localName == "instrText" ? DocxTextNodeKind.InstrText : DocxTextNodeKind.Text,
                Text = element.InnerText
            });
        }

        return result;
    }
}
