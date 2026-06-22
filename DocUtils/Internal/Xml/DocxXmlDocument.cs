using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace DocUtils;

internal static partial class DocxXmlDocument {
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    public static XmlDocument Parse(byte[] data, string partPath) {
        try {
            var xml = ProtectWhitespaceOnlyTextNodes(data);
            var document = new XmlDocument {
                PreserveWhitespace = true
            };
            document.LoadXml(xml);
            return document;
        } catch (Exception ex) {
            throw new DocxParsePartException(partPath, ex.Message, ex);
        }
    }

    public static void SetExactText(string value, XmlElement element) {
        while (element.HasChildNodes) {
            element.RemoveChild(element.FirstChild!);
        }

        if (value.Length > 0) {
            element.AppendChild(element.OwnerDocument!.CreateTextNode(value));
        }
    }

    public static void EnsureXmlSpacePreserve(XmlElement element) {
        if (element.HasAttribute("space", XmlNamespace)) {
            element.SetAttribute("space", XmlNamespace, "preserve");
            return;
        }

        var attribute = element.OwnerDocument!.CreateAttribute("xml", "space", XmlNamespace);
        attribute.Value = "preserve";
        element.Attributes.Append(attribute);
    }

    public static bool NeedsXmlSpacePreserve(string text) {
        if (text.Length == 0) {
            return false;
        }

        return char.IsWhiteSpace(text[0]) ||
               char.IsWhiteSpace(text[^1]) ||
               text.Contains("  ", StringComparison.Ordinal) ||
               text.Contains('\t') ||
               text.Contains('\n') ||
               text.Contains('\r');
    }

    public static void Save(XmlDocument document, string path, string partPath) {
        try {
            var settings = new XmlWriterSettings {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = false,
                NewLineHandling = NewLineHandling.None
            };

            using var writer = XmlWriter.Create(path, settings);
            document.Save(writer);
        } catch (Exception ex) {
            throw new DocxWritePartException(partPath, ex.Message, ex);
        }
    }

    private static string ProtectWhitespaceOnlyTextNodes(byte[] data) {
        var xml = Encoding.UTF8.GetString(data);
        return WhitespaceOnlyTextRegex().Replace(xml, static match => {
            var content = match.Groups[2].Value
                .Replace(" ", "&#x20;", StringComparison.Ordinal)
                .Replace("\t", "&#x09;", StringComparison.Ordinal)
                .Replace("\r", "&#xD;", StringComparison.Ordinal)
                .Replace("\n", "&#xA;", StringComparison.Ordinal);
            return match.Groups[1].Value + content + match.Groups[3].Value;
        });
    }

    [GeneratedRegex("(<(?:\\w+:)?(?:t|instrText)\\b[^>]*>)([ \\t\\r\\n]+)(</(?:\\w+:)?(?:t|instrText)>)", RegexOptions.Compiled)]
    private static partial Regex WhitespaceOnlyTextRegex();
}
