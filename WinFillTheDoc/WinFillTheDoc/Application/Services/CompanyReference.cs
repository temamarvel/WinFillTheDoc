namespace WinFillTheDoc.Application.Services;

public sealed record CompanyReference(IReadOnlyDictionary<string, string> Values);

public sealed record CompanyReferenceResolution(
    IReadOnlyDictionary<string, FieldReferenceIssue> Issues,
    IReadOnlyDictionary<string, string> ReferenceValues)
{
    public static CompanyReferenceResolution Empty { get; } = new(
        new Dictionary<string, FieldReferenceIssue>(),
        new Dictionary<string, string>());
}

public sealed record FieldReferenceIssue(string Key, string Message);
