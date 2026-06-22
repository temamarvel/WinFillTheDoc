namespace DocUtils;

public class DocxException : Exception {
    public DocxException(string message, Exception? innerException = null) : base(message, innerException) {
    }
}

public sealed class DocxFileNotFoundException : DocxException {
    public DocxFileNotFoundException(string path) : base($"DOCX file not found: {path}") {
    }
}

public sealed class DocxInvalidArchiveException : DocxException {
    public DocxInvalidArchiveException(string path, Exception? innerException = null) : base($"File is not a valid DOCX archive: {path}", innerException) {
    }
}

public sealed class DocxCannotCreateOutputArchiveException : DocxException {
    public DocxCannotCreateOutputArchiveException(string path, Exception? innerException = null) : base($"Cannot create output DOCX archive at: {path}", innerException) {
    }
}

public sealed class DocxUnsafeArchiveEntryException : DocxException {
    public DocxUnsafeArchiveEntryException(string path) : base($"Unsafe ZIP entry path detected: {path}") {
    }
}

public sealed class DocxMissingRequiredPartException : DocxException {
    public DocxMissingRequiredPartException(string partName) : base($"DOCX does not contain required part: {partName}") {
    }
}

public sealed class DocxReadPartException : DocxException {
    public DocxReadPartException(string path, string message, Exception? innerException = null) : base($"Failed to read XML part {path}: {message}", innerException) {
    }
}

public sealed class DocxParsePartException : DocxException {
    public DocxParsePartException(string path, string message, Exception? innerException = null) : base($"Failed to parse XML part {path}: {message}", innerException) {
    }
}

public sealed class DocxWritePartException : DocxException {
    public DocxWritePartException(string path, string message, Exception? innerException = null) : base($"Failed to write XML part {path}: {message}", innerException) {
    }
}

public sealed class DocxMissingPlaceholderValuesException : DocxException {
    public IReadOnlyCollection<string> Keys { get; }

    public DocxMissingPlaceholderValuesException(IEnumerable<string> keys)
        : base($"Missing values for placeholders: {string.Join(", ", keys.OrderBy(static x => x, StringComparer.Ordinal))}") {
        Keys = keys.OrderBy(static x => x, StringComparer.Ordinal).ToArray();
    }
}

public sealed class DocxPartialProcessingException : DocxException {
    public IReadOnlyList<DocxProcessingIssue> Issues { get; }

    public DocxPartialProcessingException(IReadOnlyList<DocxProcessingIssue> issues)
        : base($"Partial processing completed with {issues.Count} issue(s).") {
        Issues = issues;
    }
}

public class DocxConditionalAssemblyException : DocxException {
    public DocxConditionalAssemblyException(string message) : base(message) {
    }
}
