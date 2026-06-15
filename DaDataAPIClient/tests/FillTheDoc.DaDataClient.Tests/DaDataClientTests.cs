using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FillTheDoc.DaDataClient.Abstractions;
using FillTheDoc.DaDataClient.Configuration;
using FillTheDoc.DaDataClient.Errors;
using FillTheDoc.DaDataClient.Tests.TestInfrastructure;

namespace FillTheDoc.DaDataClient.Tests;

[TestFixture]
public sealed class DaDataClientTests
{
    [Test]
    public async Task FetchCompanyInfoAsync_ReturnsSuggestions_OnSuccess()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.OK, CompanyResponseJson())));
        var client = CreateClient(handler);

        var result = await client.FetchCompanyInfoAsync("7707083893", count: 2);

        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value[0].Data.Inn, Is.EqualTo("7707083893"));
        Assert.That(result.Value[0].Data.Name?.ShortWithOpf, Is.EqualTo("СБЕРБАНК"));
        Assert.That(result.Status.HttpStatus, Is.EqualTo(200));
        Assert.That(result.Status.Attempts, Is.EqualTo(1));

        var request = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://suggestions.dadata.ru/suggestions/api/4_1/rs/findById/party")));
        Assert.That(request.Authorization?.Scheme, Is.EqualTo("Token"));
        Assert.That(request.Authorization?.Parameter, Is.EqualTo("test-token"));
        Assert.That(request.Accept, Does.Contain("application/json"));
        Assert.That(request.ContentType, Is.EqualTo("application/json"));
        Assert.That(request.Body, Does.Contain("\"query\":\"7707083893\""));
        Assert.That(request.Body, Does.Contain("\"count\":2"));
    }

    [Test]
    public async Task FetchCompanyInfoFirstAsync_ReturnsFirstSuggestion()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.OK, CompanyResponseJson())));
        var client = CreateClient(handler);

        var result = await client.FetchCompanyInfoFirstAsync("7707083893");

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Value, Is.EqualTo("ПАО СБЕРБАНК"));
        Assert.That(result.Status.Attempts, Is.EqualTo(1));
    }

    [Test]
    public async Task FetchCompanyInfoFirstAsync_ReturnsNull_WhenNoSuggestions()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.OK, EmptyResponseJson())));
        var client = CreateClient(handler);

        var result = await client.FetchCompanyInfoFirstAsync("7707083893");

        Assert.That(result.Value, Is.Null);
        Assert.That(result.Status.HttpStatus, Is.EqualTo(200));
    }

    [Test]
    public async Task SuggestAddressAsync_ReturnsSuggestions_OnSuccess()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.OK, AddressResponseJson())));
        var client = CreateClient(handler);

        var result = await client.SuggestAddressAsync("Москва Тверская 1", count: 3);

        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value[0].Data.Data?.IsPreciseToHouseLevel, Is.True);
        Assert.That(result.Value[0].Data.Data?.FormattedShort, Does.Contain("д. 1"));

        var request = handler.Requests.Single();
        var json = JsonDocument.Parse(request.Body).RootElement;
        Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://suggestions.dadata.ru/suggestions/api/4_1/rs/suggest/address")));
        Assert.That(json.GetProperty("query").GetString(), Is.EqualTo("Москва Тверская 1"));
        Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(3));
    }

    [Test]
    public async Task SuggestAddressFirstAsync_ReturnsFirstSuggestion()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.OK, AddressResponseJson())));
        var client = CreateClient(handler);

        var result = await client.SuggestAddressFirstAsync("Москва Тверская 1");

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Data.Value, Does.Contain("Тверская"));
    }

    [Test]
    public async Task SuggestAddressFirstAsync_ReturnsNull_WhenNoSuggestions()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.OK, EmptyResponseJson())));
        var client = CreateClient(handler);

        var result = await client.SuggestAddressFirstAsync("Москва Тверская 1");

        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public async Task SendWithRetryAsync_Retries_On429()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            ResponseWithRetryAfter(HttpStatusCode.TooManyRequests, "{\"message\":\"rate limit\"}", seconds: null),
            FakeHttpMessageHandler.Json(HttpStatusCode.OK, CompanyResponseJson())
        });

        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(responses.Dequeue()));
        var client = CreateClient(handler);

        var result = await client.FetchCompanyInfoAsync("7707083893");

        Assert.That(handler.SendCount, Is.EqualTo(2));
        Assert.That(result.Status.Attempts, Is.EqualTo(2));
        Assert.That(result.Value[0].Data.Inn, Is.EqualTo("7707083893"));
    }

    [Test]
    public async Task SendWithRetryAsync_Retries_On5xx()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            FakeHttpMessageHandler.Json(HttpStatusCode.BadGateway, "{\"error\":\"gateway\"}"),
            FakeHttpMessageHandler.Json(HttpStatusCode.OK, AddressResponseJson())
        });

        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(responses.Dequeue()));
        var client = CreateClient(handler);

        var result = await client.SuggestAddressAsync("Москва Тверская 1");

        Assert.That(handler.SendCount, Is.EqualTo(2));
        Assert.That(result.Status.Attempts, Is.EqualTo(2));
        Assert.That(result.Value[0].Data.Value, Is.Not.Null);
    }

    [Test]
    public async Task SendWithRetryAsync_UsesRetryAfterHeader()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            ResponseWithRetryAfter(HttpStatusCode.TooManyRequests, "{\"message\":\"wait\"}", seconds: 0.1),
            FakeHttpMessageHandler.Json(HttpStatusCode.OK, CompanyResponseJson())
        });

        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(responses.Dequeue()));
        var client = CreateClient(handler, new DaDataClientOptions
        {
            BaseUrl = new Uri("https://suggestions.dadata.ru/"),
            Timeout = TimeSpan.FromSeconds(2),
            RetryPolicy = new DaDataRetryPolicy(3, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(200))
        });

        var stopwatch = Stopwatch.StartNew();
        var result = await client.FetchCompanyInfoAsync("7707083893");
        stopwatch.Stop();

        Assert.That(stopwatch.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(80)));
        Assert.That(result.Status.Attempts, Is.EqualTo(2));
    }

    [Test]
    public void SendWithRetryAsync_ThrowsHttpException_WhenNonRetryableStatus()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.BadRequest, "{\"error\":\"bad request\"}")));
        var client = CreateClient(handler);

        var exception = Assert.ThrowsAsync<DaDataHttpException>(async () => await client.FetchCompanyInfoAsync("7707083893"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.StatusCode, Is.EqualTo(400));
        Assert.That(exception.ResponseSnippet, Does.Contain("bad request"));
        Assert.That(handler.SendCount, Is.EqualTo(1));
    }

    [Test]
    public void SendWithRetryAsync_ThrowsDecodingException_WhenInvalidJson()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.OK, "not-json")));
        var client = CreateClient(handler);

        var exception = Assert.ThrowsAsync<DaDataDecodingException>(async () => await client.FetchCompanyInfoAsync("7707083893"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ResponseSnippet, Is.EqualTo("not-json"));
    }

    [Test]
    public void SendWithRetryAsync_ThrowsNetworkException_OnTransportFailure()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("DNS failure"));
        var client = CreateClient(handler, new DaDataClientOptions
        {
            BaseUrl = new Uri("https://suggestions.dadata.ru/"),
            Timeout = TimeSpan.FromSeconds(2),
            RetryPolicy = new DaDataRetryPolicy(1, TimeSpan.Zero, TimeSpan.Zero)
        });

        var exception = Assert.ThrowsAsync<DaDataNetworkException>(async () => await client.FetchCompanyInfoAsync("7707083893"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.InnerException, Is.TypeOf<HttpRequestException>());
    }

    [Test]
    public void SendWithRetryAsync_PropagatesCancellation()
    {
        var handler = new FakeHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, CompanyResponseJson());
        });

        var client = CreateClient(handler, new DaDataClientOptions
        {
            BaseUrl = new Uri("https://suggestions.dadata.ru/"),
            Timeout = TimeSpan.FromSeconds(30),
            RetryPolicy = new DaDataRetryPolicy(1, TimeSpan.Zero, TimeSpan.Zero)
        });

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        Assert.That(
            async () => await client.FetchCompanyInfoAsync("7707083893", cancellationToken: cancellationTokenSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void SendWithRetryAsync_ThrowsTimeoutException_WhenRequestTimesOut()
    {
        var handler = new FakeHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, CompanyResponseJson());
        });

        var client = CreateClient(handler, new DaDataClientOptions
        {
            BaseUrl = new Uri("https://suggestions.dadata.ru/"),
            Timeout = TimeSpan.FromMilliseconds(50),
            RetryPolicy = new DaDataRetryPolicy(1, TimeSpan.Zero, TimeSpan.Zero)
        });

        Assert.ThrowsAsync<DaDataTimeoutException>(async () => await client.FetchCompanyInfoAsync("7707083893"));
    }

    private static DaDataClient CreateClient(
        HttpMessageHandler handler,
        DaDataClientOptions? options = null,
        IDaDataTokenProvider? tokenProvider = null)
    {
        var effectiveOptions = options ?? new DaDataClientOptions
        {
            BaseUrl = new Uri("https://suggestions.dadata.ru/"),
            Timeout = TimeSpan.FromSeconds(2),
            RetryPolicy = new DaDataRetryPolicy(3, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(10))
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = effectiveOptions.BaseUrl,
            Timeout = Timeout.InfiniteTimeSpan
        };

        return new DaDataClient(
            httpClient,
            tokenProvider ?? new StaticDaDataTokenProvider("test-token"),
            effectiveOptions);
    }

    private static HttpResponseMessage ResponseWithRetryAfter(HttpStatusCode statusCode, string json, double? seconds)
    {
        var response = FakeHttpMessageHandler.Json(statusCode, json);
        if (seconds is not null)
        {
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(seconds.Value));
        }

        return response;
    }

    private static string CompanyResponseJson()
    {
        return """
               {
                 "suggestions": [
                   {
                     "value": "ПАО СБЕРБАНК",
                     "unrestricted_value": "ПАО СБЕРБАНК",
                     "data": {
                       "inn": "7707083893",
                       "kpp": "773601001",
                       "ogrn": "1027700132195",
                       "type": "LEGAL",
                       "name": {
                         "full_with_opf": "ПУБЛИЧНОЕ АКЦИОНЕРНОЕ ОБЩЕСТВО СБЕРБАНК РОССИИ",
                         "short_with_opf": "СБЕРБАНК",
                         "latin": "SBERBANK",
                         "full": "СБЕРБАНК РОССИИ",
                         "short": "СБЕРБАНК"
                       },
                       "management": {
                         "name": "ИВАНОВ ИВАН",
                         "post": "ГЕНЕРАЛЬНЫЙ ДИРЕКТОР"
                       },
                       "address": {
                         "value": "г Москва, ул Вавилова, д 19",
                         "unrestricted_value": "117997, г Москва, ул Вавилова, д 19",
                         "data": {
                           "postal_code": "117997",
                           "country": "Россия",
                           "region_with_type": "г Москва",
                           "city_with_type": "г Москва",
                           "street_with_type": "ул Вавилова",
                           "house": "19",
                           "fias_id": "fias-company",
                           "kladr_id": "7700000000000"
                         }
                       },
                       "state": {
                         "status": "ACTIVE",
                         "registration_date": 631152000000,
                         "liquidation_date": null
                       },
                       "okved": "64.19",
                       "okved_type": "2014"
                     }
                   }
                 ]
               }
               """;
    }

    private static string AddressResponseJson()
    {
        return """
               {
                 "suggestions": [
                   {
                     "value": "г Москва, ул Тверская, д 1",
                     "unrestricted_value": "125009, г Москва, ул Тверская, д 1",
                     "data": {
                       "value": "г Москва, ул Тверская, д 1",
                       "unrestricted_value": "125009, г Москва, ул Тверская, д 1",
                       "data": {
                         "postal_code": "125009",
                         "country": "Россия",
                         "region_with_type": "г Москва",
                         "city_with_type": "г Москва",
                         "street_with_type": "ул Тверская",
                         "house": "1",
                         "block": null,
                         "flat": null,
                         "fias_id": "fias-address",
                         "kladr_id": "7700000000000"
                       }
                     }
                   }
                 ]
               }
               """;
    }

    private static string EmptyResponseJson()
    {
        return """
               {
                 "suggestions": []
               }
               """;
    }
}


