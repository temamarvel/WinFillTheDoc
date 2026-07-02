namespace WinFillTheDoc.Application.Services;

public interface IAppUpdateChecker
{
    Task<UpdateAvailability?> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}
