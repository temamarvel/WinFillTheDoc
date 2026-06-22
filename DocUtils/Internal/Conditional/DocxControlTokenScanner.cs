using System.Text.RegularExpressions;
using System.Xml;

namespace DocUtils;

internal static partial class DocxControlTokenScanner {
    public static TemplateControlToken? Scan(XmlElement paragraph) {
        var projection = ParagraphTextProjection.Build(paragraph, includeFieldInstructionText: false);
        var text = projection.FullText.Trim();
        if (text.Length == 0) {
            return null;
        }

        var switchStart = SwitchStartPattern().Match(text);
        if (switchStart.Success) {
            return new TemplateControlToken {
                Kind = new TemplateControlTokenKind.SwitchStart(switchStart.Groups[1].Value),
                ParagraphElement = paragraph
            };
        }

        if (SwitchEndPattern().IsMatch(text)) {
            return new TemplateControlToken {
                Kind = new TemplateControlTokenKind.SwitchEnd(),
                ParagraphElement = paragraph
            };
        }

        var caseStart = CaseStartPattern().Match(text);
        if (caseStart.Success) {
            return new TemplateControlToken {
                Kind = new TemplateControlTokenKind.CaseStart(caseStart.Groups[1].Value.Trim()),
                ParagraphElement = paragraph
            };
        }

        if (CaseEndPattern().IsMatch(text)) {
            return new TemplateControlToken {
                Kind = new TemplateControlTokenKind.CaseEnd(),
                ParagraphElement = paragraph
            };
        }

        if (DefaultStartPattern().IsMatch(text)) {
            return new TemplateControlToken {
                Kind = new TemplateControlTokenKind.DefaultStart(),
                ParagraphElement = paragraph
            };
        }

        if (DefaultEndPattern().IsMatch(text)) {
            return new TemplateControlToken {
                Kind = new TemplateControlTokenKind.DefaultEnd(),
                ParagraphElement = paragraph
            };
        }

        return null;
    }

    [GeneratedRegex("^<!switch_start:([A-Za-z0-9_]+)!>$", RegexOptions.Compiled)]
    private static partial Regex SwitchStartPattern();

    [GeneratedRegex("^<!switch_end!>$", RegexOptions.Compiled)]
    private static partial Regex SwitchEndPattern();

    [GeneratedRegex("^<!case_start:([^!]+)!>$", RegexOptions.Compiled)]
    private static partial Regex CaseStartPattern();

    [GeneratedRegex("^<!case_end!>$", RegexOptions.Compiled)]
    private static partial Regex CaseEndPattern();

    [GeneratedRegex("^<!default_start!>$", RegexOptions.Compiled)]
    private static partial Regex DefaultStartPattern();

    [GeneratedRegex("^<!default_end!>$", RegexOptions.Compiled)]
    private static partial Regex DefaultEndPattern();
}
