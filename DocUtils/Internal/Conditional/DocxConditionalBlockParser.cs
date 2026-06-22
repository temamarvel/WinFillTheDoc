using System.Xml;

namespace DocUtils;

internal static class DocxConditionalBlockParser {
    public static List<ConditionalSwitchBlock> Parse(IReadOnlyList<XmlElement> blockNodes, string partName) {
        var tokens = new Dictionary<int, TemplateControlToken>();

        for (var index = 0; index < blockNodes.Count; index++) {
            if (blockNodes[index].LocalName != "p") {
                continue;
            }

            var token = DocxControlTokenScanner.Scan(blockNodes[index]);
            if (token is not null) {
                tokens[index] = token;
            }
        }

        var result = new List<ConditionalSwitchBlock>();
        var i = 0;

        while (i < blockNodes.Count) {
            if (!tokens.TryGetValue(i, out var token)) {
                i++;
                continue;
            }

            switch (token.Kind) {
                case TemplateControlTokenKind.SwitchStart switchStart:
                    var parsed = ParseSwitchBlock(switchStart.Key, blockNodes[i], i, blockNodes, tokens, partName);
                    result.Add(parsed.Block);
                    i = parsed.NextIndex;
                    break;
                case TemplateControlTokenKind.SwitchEnd:
                    throw DocxConditionalAssemblyErrors.SwitchEndWithoutStart(partName);
                case TemplateControlTokenKind.CaseStart caseStart:
                    throw DocxConditionalAssemblyErrors.CaseStartOutsideSwitch(caseStart.Value, partName);
                case TemplateControlTokenKind.CaseEnd:
                    throw DocxConditionalAssemblyErrors.CaseEndWithoutStart(partName);
                case TemplateControlTokenKind.DefaultStart:
                    throw DocxConditionalAssemblyErrors.DefaultStartOutsideSwitch(partName);
                case TemplateControlTokenKind.DefaultEnd:
                    throw DocxConditionalAssemblyErrors.DefaultEndWithoutStart(partName);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return result;
    }

    private static (ConditionalSwitchBlock Block, int NextIndex) ParseSwitchBlock(
        string key,
        XmlElement switchStartNode,
        int startIndex,
        IReadOnlyList<XmlElement> blockNodes,
        IReadOnlyDictionary<int, TemplateControlToken> tokens,
        string partName
    ) {
        var cases = new List<ConditionalCaseBlock>();
        ConditionalDefaultBlock? defaultBlock = null;
        var seenCaseValues = new HashSet<string>(StringComparer.Ordinal);
        var index = startIndex + 1;

        while (index < blockNodes.Count) {
            if (!tokens.TryGetValue(index, out var token)) {
                index++;
                continue;
            }

            switch (token.Kind) {
                case TemplateControlTokenKind.SwitchEnd:
                    return (new ConditionalSwitchBlock {
                        Key = key,
                        Cases = cases,
                        DefaultBlock = defaultBlock,
                        SwitchStartNode = switchStartNode,
                        SwitchEndNode = blockNodes[index]
                    }, index + 1);
                case TemplateControlTokenKind.CaseStart caseStart:
                    if (!seenCaseValues.Add(caseStart.Value)) {
                        throw DocxConditionalAssemblyErrors.DuplicateCaseValue(key, caseStart.Value, partName);
                    }

                    var caseBlock = ParseCaseBlock(caseStart.Value, blockNodes[index], index, blockNodes, tokens, partName);
                    cases.Add(caseBlock.Block);
                    index = caseBlock.NextIndex;
                    break;
                case TemplateControlTokenKind.DefaultStart:
                    if (defaultBlock is not null) {
                        throw DocxConditionalAssemblyErrors.DuplicateDefault(partName);
                    }

                    var parsedDefault = ParseDefaultBlock(blockNodes[index], index, blockNodes, tokens, partName);
                    defaultBlock = parsedDefault.Block;
                    index = parsedDefault.NextIndex;
                    break;
                case TemplateControlTokenKind.SwitchStart:
                    throw DocxConditionalAssemblyErrors.NestedSwitchNotSupported(partName);
                case TemplateControlTokenKind.CaseEnd:
                    throw DocxConditionalAssemblyErrors.CaseEndWithoutStart(partName);
                case TemplateControlTokenKind.DefaultEnd:
                    throw DocxConditionalAssemblyErrors.DefaultEndWithoutStart(partName);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        throw DocxConditionalAssemblyErrors.SwitchStartWithoutEnd(key, partName);
    }

    private static (ConditionalCaseBlock Block, int NextIndex) ParseCaseBlock(
        string value,
        XmlElement caseStartNode,
        int startIndex,
        IReadOnlyList<XmlElement> blockNodes,
        IReadOnlyDictionary<int, TemplateControlToken> tokens,
        string partName
    ) {
        var contentNodes = new List<XmlElement>();
        var index = startIndex + 1;

        while (index < blockNodes.Count) {
            if (tokens.TryGetValue(index, out var token)) {
                if (token.Kind is TemplateControlTokenKind.CaseEnd) {
                    return (new ConditionalCaseBlock {
                        Value = value,
                        ContentNodes = contentNodes,
                        CaseStartNode = caseStartNode,
                        CaseEndNode = blockNodes[index]
                    }, index + 1);
                }

                throw DocxConditionalAssemblyErrors.CaseStartWithoutEnd(value, partName);
            }

            contentNodes.Add(blockNodes[index]);
            index++;
        }

        throw DocxConditionalAssemblyErrors.CaseStartWithoutEnd(value, partName);
    }

    private static (ConditionalDefaultBlock Block, int NextIndex) ParseDefaultBlock(
        XmlElement defaultStartNode,
        int startIndex,
        IReadOnlyList<XmlElement> blockNodes,
        IReadOnlyDictionary<int, TemplateControlToken> tokens,
        string partName
    ) {
        var contentNodes = new List<XmlElement>();
        var index = startIndex + 1;

        while (index < blockNodes.Count) {
            if (tokens.TryGetValue(index, out var token)) {
                if (token.Kind is TemplateControlTokenKind.DefaultEnd) {
                    return (new ConditionalDefaultBlock {
                        ContentNodes = contentNodes,
                        DefaultStartNode = defaultStartNode,
                        DefaultEndNode = blockNodes[index]
                    }, index + 1);
                }

                throw DocxConditionalAssemblyErrors.DefaultStartWithoutEnd(partName);
            }

            contentNodes.Add(blockNodes[index]);
            index++;
        }

        throw DocxConditionalAssemblyErrors.DefaultStartWithoutEnd(partName);
    }
}
