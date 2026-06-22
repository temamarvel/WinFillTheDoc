namespace DocUtils;

internal sealed record TextLocation(int NodeIndex, int Offset);

internal sealed class TextOffsetMapper {
    private readonly int[] _prefixSums;

    public TextOffsetMapper(int[] prefixSums) {
        _prefixSums = prefixSums;
    }

    public static int[] ComputePrefixSums(IReadOnlyList<int> lengths) {
        var result = new int[lengths.Count + 1];
        for (var index = 0; index < lengths.Count; index++) {
            result[index + 1] = result[index] + lengths[index];
        }

        return result;
    }

    public TextLocation? LocateStart(int position) {
        for (var index = 0; index < _prefixSums.Length - 1; index++) {
            var start = _prefixSums[index];
            var end = _prefixSums[index + 1];
            if (position >= start && position < end) {
                return new TextLocation(index, position - start);
            }
        }

        return null;
    }

    public TextLocation? LocateEnd(int position, int fullLength) {
        if (position == fullLength) {
            for (var index = _prefixSums.Length - 2; index >= 0; index--) {
                var start = _prefixSums[index];
                var end = _prefixSums[index + 1];
                if (end > start) {
                    return new TextLocation(index, end - start);
                }
            }
        }

        for (var index = 0; index < _prefixSums.Length - 1; index++) {
            var start = _prefixSums[index];
            var end = _prefixSums[index + 1];
            if (end <= start) {
                continue;
            }

            if (position > start && position <= end) {
                return new TextLocation(index, position - start);
            }
        }

        return null;
    }
}
