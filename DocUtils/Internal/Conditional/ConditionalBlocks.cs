using System.Xml;

namespace DocUtils;

internal sealed class ConditionalCaseBlock {
    public required string Value { get; init; }
    public required List<XmlElement> ContentNodes { get; init; }
    public required XmlElement CaseStartNode { get; init; }
    public required XmlElement CaseEndNode { get; init; }
}

internal sealed class ConditionalDefaultBlock {
    public required List<XmlElement> ContentNodes { get; init; }
    public required XmlElement DefaultStartNode { get; init; }
    public required XmlElement DefaultEndNode { get; init; }
}

internal sealed class ConditionalSwitchBlock {
    public required string Key { get; init; }
    public required List<ConditionalCaseBlock> Cases { get; init; }
    public ConditionalDefaultBlock? DefaultBlock { get; init; }
    public required XmlElement SwitchStartNode { get; init; }
    public required XmlElement SwitchEndNode { get; init; }
}
