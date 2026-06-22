namespace DocUtils;

internal static class ParagraphTextMutator {
    public static void ApplyReplacement(
        IReadOnlyList<DocxTextNode> nodes,
        TextLocation start,
        TextLocation end,
        string replacement
    ) {
        var startIndex = start.NodeIndex;
        var endIndex = end.NodeIndex;

        if (startIndex == endIndex) {
            var original = nodes[startIndex].Text;
            nodes[startIndex].Text = original[..start.Offset] + replacement + original[end.Offset..];
            nodes[startIndex].IsDirty = true;
            return;
        }

        var prefix = nodes[startIndex].Text[..start.Offset];
        var suffix = nodes[endIndex].Text[end.Offset..];
        nodes[startIndex].Text = prefix + replacement + suffix;
        nodes[startIndex].IsDirty = true;

        for (var index = startIndex + 1; index <= endIndex; index++) {
            if (nodes[index].Text.Length == 0) {
                continue;
            }

            nodes[index].Text = string.Empty;
            nodes[index].IsDirty = true;
        }
    }
}
