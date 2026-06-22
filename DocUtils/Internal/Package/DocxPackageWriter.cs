using System.IO.Compression;

namespace DocUtils;

internal static class DocxPackageWriter {
    public static void Repack(string directoryPath, string outputPath) {
        try {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory);
            }

            if (File.Exists(outputPath)) {
                File.Delete(outputPath);
            }

            using var stream = File.Create(outputPath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false);
            var basePath = Path.GetFullPath(directoryPath);

            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)) {
                var relativePath = Path.GetRelativePath(basePath, filePath).Replace('\\', '/');
                archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.SmallestSize);
            }
        } catch (Exception ex) {
            throw new DocxCannotCreateOutputArchiveException(outputPath, ex);
        }
    }
}
