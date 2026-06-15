namespace FillTheDoc.OpenAIClient.Models;

public sealed record OpenAIResult<T>(T Value, RequestStatus Status);