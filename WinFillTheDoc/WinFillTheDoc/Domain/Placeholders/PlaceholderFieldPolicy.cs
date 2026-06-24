namespace WinFillTheDoc.Domain.Placeholders;

public sealed class PlaceholderFieldPolicy
{
    public PlaceholderFieldPolicy(Func<string, string>? normalize = null, Func<string, FieldIssue?>? validate = null)
    {
        Normalize = normalize ?? FieldNormalizers.Trim;
        Validate = validate ?? (_ => null);
    }

    public Func<string, string> Normalize { get; }
    public Func<string, FieldIssue?> Validate { get; }
}
