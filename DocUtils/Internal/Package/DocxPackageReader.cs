using System.IO.Compression;

namespace DocUtils;

internal sealed class DocxPackageReader : IDisposable {
    private readonly FileStream _stream;
    private readonly ZipArchive _archive;

    public DocxPackageReader(string path) {
        try {
            _stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            _archive = new ZipArchive(_stream, ZipArchiveMode.Read, leaveOpen: false);
        } catch (Exception ex) {
            throw new DocxInvalidArchiveException(path, ex);
        }
    }

    public IEnumerable<string> AllEntryPaths() => _archive.Entries.Select(static x => x.FullName);

    public byte[] ReadData(string path) {
        var entry = _archive.GetEntry(path);
        if (entry is null) {
            throw new DocxReadPartException(path, "Entry not found");
        }

        try {
            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        } catch (Exception ex) {
            throw new DocxReadPartException(path, ex.Message, ex);
        }
    }

    public IEnumerable<ZipArchiveEntry> Entries => _archive.Entries;

    public void Dispose() {
        _archive.Dispose();
        _stream.Dispose();
    }
}
