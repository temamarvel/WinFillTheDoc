namespace DocUtils;

internal enum PlaceholderResolutionKind {
    Replace,
    KeepOriginal,
    ReplaceWithEmptyString,
    MissingRequired
}

internal sealed class PlaceholderResolution {
    public required PlaceholderResolutionKind Kind { get; init; }
    public string? Value { get; init; }
    public string? Key { get; init; }
}

internal sealed class DocxPlaceholderResolver {
    private readonly IReadOnlyDictionary<string, string> _values;
    private readonly MissingKeyPolicy _missingKeyPolicy;

    public DocxPlaceholderResolver(IReadOnlyDictionary<string, string> values, MissingKeyPolicy missingKeyPolicy) {
        _values = values;
        _missingKeyPolicy = missingKeyPolicy;
    }

    public PlaceholderResolution Resolve(string key) {
        if (_values.TryGetValue(key, out var value)) {
            return new PlaceholderResolution {
                Kind = PlaceholderResolutionKind.Replace,
                Value = value
            };
        }

        return _missingKeyPolicy switch {
            MissingKeyPolicy.Error => new PlaceholderResolution {
                Kind = PlaceholderResolutionKind.MissingRequired,
                Key = key
            },
            MissingKeyPolicy.KeepPlaceholder => new PlaceholderResolution {
                Kind = PlaceholderResolutionKind.KeepOriginal
            },
            MissingKeyPolicy.ReplaceWithEmptyString => new PlaceholderResolution {
                Kind = PlaceholderResolutionKind.ReplaceWithEmptyString
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
