namespace DocUtils;

public sealed class DocxFillReport {
    public List<string> ProcessedParts { get; } = [];
    public HashSet<string> FoundKeys { get; } = [];
    public HashSet<string> ReplacedKeys { get; } = [];
    public HashSet<string> MissingKeys { get; } = [];
    public int ReplacementsCount { get; set; }
    public List<DocxProcessingIssue> Issues { get; set; } = [];
}

public sealed class DocxScanReport {
    public List<string> ProcessedParts { get; } = [];
    public List<string> OrderedKeys { get; } = [];
    public HashSet<string> FoundKeys { get; } = [];
    public Dictionary<string, int> Occurrences { get; } = [];
    public Dictionary<string, HashSet<string>> PartsByKey { get; } = [];
    public List<DocxProcessingIssue> Issues { get; set; } = [];
    public IReadOnlyList<string> SortedKeys => FoundKeys.OrderBy(static x => x, StringComparer.Ordinal).ToArray();
}

public sealed class ResolvedSwitchInfo {
    public required string Key { get; init; }
    public required string PartName { get; init; }
    public string? SelectedCase { get; init; }
    public bool UsedDefault { get; init; }
    public bool BlockRemoved { get; init; }
}

public sealed class DocxConditionalAssemblyReport {
    public List<string> ProcessedParts { get; } = [];
    public List<ResolvedSwitchInfo> ResolvedSwitches { get; } = [];
    public int RemovedControlMarkersCount { get; set; }
    public int RemovedBlocksCount { get; set; }
}

public sealed record DocxProcessingIssue(
    string PartPath,
    DocxProcessingOperation Operation,
    string Message
);

public enum DocxProcessingOperation {
    Read,
    Parse,
    Mutate,
    Write
}
