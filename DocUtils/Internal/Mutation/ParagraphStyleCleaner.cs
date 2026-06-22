using System.Xml;

namespace DocUtils;

internal sealed class ParagraphStyleCleaner {
    private readonly ReplacementStylePolicy _policy;

    public ParagraphStyleCleaner(ReplacementStylePolicy policy) {
        _policy = policy;
    }

    public void CleanAfterReplacement(IReadOnlyList<DocxTextNode> nodes, int startIndex, int endIndex) {
        if (_policy != ReplacementStylePolicy.RemoveHighlightOnly || startIndex > endIndex) {
            return;
        }

        var handledRuns = new HashSet<XmlElement>();

        for (var index = startIndex; index <= endIndex; index++) {
            var run = FindOwningRun(nodes[index].Element);
            if (run is null || !handledRuns.Add(run)) {
                continue;
            }

            ClearHighlightAttributes(run);
        }
    }

    private static XmlElement? FindOwningRun(XmlElement element) {
        var current = element.ParentNode;
        while (current is not null) {
            if (current is XmlElement currentElement && currentElement.LocalName == "r") {
                return currentElement;
            }

            current = current.ParentNode;
        }

        return null;
    }

    private static void ClearHighlightAttributes(XmlElement run) {
        var runProperties = run.SelectSingleNode("./*[local-name()='rPr']") as XmlElement;
        if (runProperties is null) {
            runProperties = run.OwnerDocument!.CreateElement(run.Prefix, "rPr", run.NamespaceURI);
            if (run.HasChildNodes) {
                run.InsertBefore(runProperties, run.FirstChild);
            } else {
                run.AppendChild(runProperties);
            }
        }

        RemoveChildren(runProperties, "shd");
        RemoveChildren(runProperties, "highlight");
    }

    private static void RemoveChildren(XmlElement element, string localName) {
        var children = element.ChildNodes.OfType<XmlElement>()
            .Where(child => child.LocalName == localName)
            .ToArray();

        foreach (var child in children) {
            element.RemoveChild(child);
        }
    }
}
