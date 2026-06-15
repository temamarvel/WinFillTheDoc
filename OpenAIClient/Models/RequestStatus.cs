namespace FillTheDoc.OpenAIClient.Models;

public sealed record RequestStatus(int HttpStatus, string? RequestId, int Retries, int DurationMs) {
    public override string ToString() =>
        $"HTTP {HttpStatus}, retries={Retries}, duration={DurationMs}ms, requestId={RequestId ?? "null"}";
}