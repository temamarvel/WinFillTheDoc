namespace DocUtils;

public enum MissingKeyPolicy {
    Error,
    KeepPlaceholder,
    ReplaceWithEmptyString
}

public enum PartFailurePolicy {
    FailFast,
    CollectAndThrow,
    ContinueAndReport
}

public enum ReplacementStylePolicy {
    KeepRunFormatting,
    RemoveHighlightOnly
}

public enum MissingSwitchValuePolicy {
    Error,
    RemoveBlock,
    UseDefaultIfPresent
}

public enum UnknownCasePolicy {
    Error,
    RemoveBlock,
    UseDefaultIfPresent
}

public sealed class DocxFillOptions {
    public DocxProcessingScope Scope { get; init; } = new();
    public DocxPlaceholderPattern Pattern { get; init; } = new();
    public MissingKeyPolicy MissingKeyPolicy { get; init; } = MissingKeyPolicy.Error;
    public PartFailurePolicy PartFailurePolicy { get; init; } = PartFailurePolicy.FailFast;
    public ReplacementStylePolicy ReplacementStylePolicy { get; init; } = ReplacementStylePolicy.RemoveHighlightOnly;
}

public sealed class DocxConditionalAssemblyOptions {
    public DocxProcessingScope Scope { get; init; } = new();
    public MissingSwitchValuePolicy MissingSwitchValuePolicy { get; init; } = MissingSwitchValuePolicy.UseDefaultIfPresent;
    public UnknownCasePolicy UnknownCasePolicy { get; init; } = UnknownCasePolicy.UseDefaultIfPresent;
}
