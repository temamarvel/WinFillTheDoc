using FillTheDoc.DaDataClient.Models;

namespace FillTheDoc.DaDataClient.Abstractions;

public interface IDaDataClient {
    Task<DaDataResult<IReadOnlyList<DaDataSuggestion<DaDataCompanyInfo>>>> FetchCompanyInfoAsync(
        string innOrOgrn,
        int count = 1,
        CancellationToken cancellationToken = default);

    Task<DaDataResult<DaDataSuggestion<DaDataCompanyInfo>?>> FetchCompanyInfoFirstAsync(
        string innOrOgrn,
        CancellationToken cancellationToken = default);

    Task<DaDataResult<IReadOnlyList<DaDataSuggestion<DaDataAddress>>>> SuggestAddressAsync(
        string query,
        int count = 1,
        CancellationToken cancellationToken = default);

    Task<DaDataResult<DaDataSuggestion<DaDataAddress>?>> SuggestAddressFirstAsync(
        string query,
        CancellationToken cancellationToken = default);
}