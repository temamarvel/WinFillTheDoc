namespace FillTheDoc.OpenAIClient.Internal;

internal static class StringSnippetExtensions {
    public static string ToSnippet(this string value, int limit = 8_000) =>
        value.Length > limit ? value[..limit] + "…" : value;
}