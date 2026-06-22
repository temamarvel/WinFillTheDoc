using System.Xml;

namespace DocUtils;

internal static class DocxParagraphLocator {
    public static IReadOnlyList<XmlElement> FindParagraphs(XmlDocument document) {
        return document.SelectNodes("//*[local-name()='p']")
            ?.OfType<XmlElement>()
            .ToArray() ?? [];
    }
}
