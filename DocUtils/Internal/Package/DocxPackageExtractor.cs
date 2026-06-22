using System.IO.Compression;

namespace DocUtils;

internal static class DocxPackageExtractor {
    public static void ExtractSafely(IEnumerable<ZipArchiveEntry> entries, string destinationDirectory) {
        var basePath = Path.GetFullPath(destinationDirectory);

        foreach (var entry in entries) {
            var entryPath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            if (entryPath.Contains("..", StringComparison.Ordinal) ||
                entryPath.StartsWith(Path.DirectorySeparatorChar) ||
                entryPath.StartsWith(Path.AltDirectorySeparatorChar)) {
                throw new DocxUnsafeArchiveEntryException(entry.FullName);
            }

            var outputPath = Path.GetFullPath(Path.Combine(destinationDirectory, entryPath));
            if (!outputPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) {
                throw new DocxUnsafeArchiveEntryException(entry.FullName);
            }

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) {
                Directory.CreateDirectory(directory);
            }

            if (string.IsNullOrEmpty(entry.Name)) {
                Directory.CreateDirectory(outputPath);
                continue;
            }

            using var input = entry.Open();
            using var output = File.Create(outputPath);
            input.CopyTo(output);
        }
    }
}
