namespace WinFillTheDoc.Application.Services;

public interface ICompanyReferenceService
{
    Task<CompanyReference?> FindAsync(string innOrOgrn, CancellationToken cancellationToken = default);
}
