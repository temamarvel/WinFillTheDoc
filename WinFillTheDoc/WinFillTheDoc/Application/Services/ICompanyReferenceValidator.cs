namespace WinFillTheDoc.Application.Services;

public interface ICompanyReferenceValidator
{
    Task<CompanyReferenceResolution> ResolveAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default);
}
