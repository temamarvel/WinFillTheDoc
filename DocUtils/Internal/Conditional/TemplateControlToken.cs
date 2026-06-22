using System.Xml;

namespace DocUtils;

internal sealed class TemplateControlToken {
    public required TemplateControlTokenKind Kind { get; init; }
    public required XmlElement ParagraphElement { get; init; }
}

internal abstract record TemplateControlTokenKind {
    public sealed record SwitchStart(string Key) : TemplateControlTokenKind;
    public sealed record SwitchEnd : TemplateControlTokenKind;
    public sealed record CaseStart(string Value) : TemplateControlTokenKind;
    public sealed record CaseEnd : TemplateControlTokenKind;
    public sealed record DefaultStart : TemplateControlTokenKind;
    public sealed record DefaultEnd : TemplateControlTokenKind;
}
