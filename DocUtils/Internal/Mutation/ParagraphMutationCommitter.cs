namespace DocUtils;

internal static class ParagraphMutationCommitter {
    public static void CommitChanges(IEnumerable<DocxTextNode> nodes) {
        foreach (var node in nodes) {
            if (!node.IsDirty) {
                continue;
            }

            DocxXmlDocument.SetExactText(node.Text, node.Element);
            if (node.Kind == DocxTextNodeKind.Text && DocxXmlDocument.NeedsXmlSpacePreserve(node.Text)) {
                DocxXmlDocument.EnsureXmlSpacePreserve(node.Element);
            }

            node.IsDirty = false;
        }
    }
}
