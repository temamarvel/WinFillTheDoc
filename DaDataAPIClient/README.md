# FillTheDoc.DaDataClient

Порт Swift-библиотеки `DaDataAPIClient` на `.NET 10` в виде независимой инфраструктурной DLL.

## Что умеет

- поиск компании по `ИНН/ОГРН` через `findById/party`;
- подсказки адресов через `suggest/address`;
- retry для `429` и `5xx`;
- retry для типовых транспортных ошибок;
- таймауты с отдельным `DaDataTimeoutException`;
- typed DTO-модели и `DaDataResult<T>` со статусом запроса;
- интеграция через `IServiceCollection`.

## Структура

```text
src/FillTheDoc.DaDataClient
tests/FillTheDoc.DaDataClient.Tests
```

## Быстрый пример

```csharp
using FillTheDoc.DaDataClient;
using FillTheDoc.DaDataClient.Abstractions;
using FillTheDoc.DaDataClient.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDaDataClient(
    options =>
    {
        options.BaseUrl = new Uri("https://suggestions.dadata.ru/");
        options.Timeout = TimeSpan.FromSeconds(15);
        options.RetryPolicy = DaDataRetryPolicy.Default;
    },
    _ => new StaticDaDataTokenProvider("<DADATA_TOKEN>"));

await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IDaDataClient>();

var company = await client.FetchCompanyInfoFirstAsync("7707083893");
var address = await client.SuggestAddressFirstAsync("Москва Тверская 1");
```

## Запуск тестов

```powershell
cd C:\Home\WinFillTheDoc\DaDataAPIClient

dotnet test .\tests\FillTheDoc.DaDataClient.Tests\FillTheDoc.DaDataClient.Tests.csproj
```

## Основные публичные типы

- `IDaDataClient`
- `IDaDataTokenProvider`
- `DaDataClientOptions`
- `DaDataRetryPolicy`
- `DaDataResult<T>`
- `DaDataRequestStatus`
- `DaDataSuggestion<T>`
- `DaDataCompanyInfo`
- `DaDataAddress`
- `DaDataAddressData`
- `DaDataClientException` и производные исключения

