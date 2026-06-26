using System.Text;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.Services;

public static class PromptBuilder
{
    public static string BuildSystem(IReadOnlyList<PlaceholderDescriptor> schemaDescriptors)
    {
        var schemaKeys = string.Join(", ", schemaDescriptors.Select(x => $"\"{x.Key}\""));
        var customRules = schemaDescriptors
            .Where(x => false)
            .Select(x => $"- {x.Key}: {x.Description}")
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine("You are a precision information extraction engine for Russian legal entity and sole proprietor requisites.");
        builder.AppendLine("Return ONLY a single valid JSON object.");
        builder.AppendLine();
        builder.AppendLine("Hard rules:");
        builder.AppendLine("- Output must be a single JSON object.");
        builder.AppendLine("- No markdown, no comments, no explanations, no extra text.");
        builder.AppendLine($"- Use exactly these keys: {schemaKeys}");
        builder.AppendLine("- Do not add any extra keys.");
        builder.AppendLine("- Every value must be either a string or null.");
        builder.AppendLine("- If a value is missing, unknown, unreadable, ambiguous, or not explicitly present in the source text, return null.");
        builder.AppendLine("- Do not guess, infer, or invent values.");
        builder.AppendLine("- Preserve original spelling from the source when possible.");
        builder.AppendLine("- Trim surrounding whitespace from all string values.");
        builder.AppendLine();
        builder.AppendLine("Built-in field rules:");
        builder.AppendLine(BuiltInFieldRules);
        builder.AppendLine();
        builder.AppendLine("Custom field rules:");
        builder.AppendLine(customRules.Count == 0 ? "- No custom extracted fields." : string.Join(Environment.NewLine, customRules));
        return builder.ToString();
    }

    public static string BuildUser(string sourceText) =>
        $"""
        Extract requisites from the SOURCE TEXT below.

        Notes:
        - Requisites often appear near labels like: "Реквизиты", "ИНН", "КПП", "ОГРН/ОГРНИП", "Генеральный директор/Директор", "E-mail/Email".
        - If multiple companies are present, prefer the main organization.

        SOURCE TEXT:
        ---
        {sourceText}
        ---
        """;

    private const string BuiltInFieldRules = """
    - company_name:
      Extract only the company or entrepreneur name itself, without legal form, if they are clearly separable.

    - legal_form:
      Allowed values only: "ООО", "ЗАО", "АО", "ИП", "ПАО".
      Map full Russian names to the corresponding short form.
      If the legal form is not one of these values, return null.

    - ceo_full_name:
      Extract the full name of the head, signer, or entrepreneur only if explicitly present.

    - ceo_full_genitive_name:
      Return the full name in genitive case only when present or safely derivable with high confidence.

    - ceo_shorten_name:
      Return the shortened name in format "Фамилия И.О." only when present or safely derivable with high confidence.

    - ogrn:
      Return digits only. Valid lengths are 13 digits for OGRN and 15 digits for OGRNIP.

    - inn:
      Return digits only. Valid lengths are 10 digits for legal entities and 12 digits for sole proprietors.

    - kpp:
      Return digits only. Must contain exactly 9 digits. Return null if missing.

    - email:
      Extract only an explicit syntactically valid email address.

    - address:
      Extract the most complete official address explicitly present in the text.

    - phone:
      Extract only if explicitly present. Minor normalization is allowed.
    """;
}
