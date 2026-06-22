namespace DocUtils;

internal static class DocxConditionalAssemblyErrors {
    public static DocxConditionalAssemblyException SwitchStartWithoutEnd(string key, string part) =>
        new($"<!switch_start:{key}!> has no matching <!switch_end!> in part '{part}'.");

    public static DocxConditionalAssemblyException SwitchEndWithoutStart(string part) =>
        new($"<!switch_end!> found without a preceding <!switch_start!> in part '{part}'.");

    public static DocxConditionalAssemblyException CaseStartOutsideSwitch(string value, string part) =>
        new($"<!case_start:{value}!> found outside a switch block in part '{part}'.");

    public static DocxConditionalAssemblyException CaseEndWithoutStart(string part) =>
        new($"<!case_end!> found without a preceding <!case_start!> in part '{part}'.");

    public static DocxConditionalAssemblyException CaseStartWithoutEnd(string value, string part) =>
        new($"<!case_start:{value}!> has no matching <!case_end!> in part '{part}'.");

    public static DocxConditionalAssemblyException DefaultStartOutsideSwitch(string part) =>
        new($"<!default_start!> found outside a switch block in part '{part}'.");

    public static DocxConditionalAssemblyException DefaultEndWithoutStart(string part) =>
        new($"<!default_end!> found without a preceding <!default_start!> in part '{part}'.");

    public static DocxConditionalAssemblyException DefaultStartWithoutEnd(string part) =>
        new($"<!default_start!> has no matching <!default_end!> in part '{part}'.");

    public static DocxConditionalAssemblyException DuplicateCaseValue(string key, string value, string part) =>
        new($"Duplicate case value '{value}' in switch '{key}' in part '{part}'.");

    public static DocxConditionalAssemblyException DuplicateDefault(string part) =>
        new($"Multiple <!default_start!> blocks inside a single switch in part '{part}'.");

    public static DocxConditionalAssemblyException NestedSwitchNotSupported(string part) =>
        new($"Nested <!switch_start!> inside another switch block is not supported (part '{part}').");

    public static DocxConditionalAssemblyException MissingValueForSwitch(string key, string part) =>
        new($"No value provided for switch key '{key}' in part '{part}'.");

    public static DocxConditionalAssemblyException NoMatchingCase(string key, string value, string part) =>
        new($"No matching case '{value}' for switch key '{key}' in part '{part}'.");
}
