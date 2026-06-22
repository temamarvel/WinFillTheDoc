namespace DocUtils;

public sealed class DocxProcessingScope {
    public PartSelection Selection { get; init; } = PartSelection.Standard;
    public bool IncludeFootnotes { get; init; } = true;
    public bool IncludeEndnotes { get; init; } = true;
    public bool IncludeComments { get; init; } = true;
    public bool IncludeFieldInstructionText { get; init; }

    public enum PartSelection {
        Standard,
        AllWordXml
    }
}
