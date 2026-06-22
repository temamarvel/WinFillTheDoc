using System.Xml;

namespace DocUtils;

internal sealed class DocxConditionalBlockResolver {
    private readonly IReadOnlyDictionary<string, string> _values;
    private readonly DocxConditionalAssemblyOptions _options;
    private readonly string _partName;

    public DocxConditionalBlockResolver(
        IReadOnlyDictionary<string, string> values,
        DocxConditionalAssemblyOptions options,
        string partName
    ) {
        _values = values;
        _options = options;
        _partName = partName;
    }

    public (List<ResolvedSwitchInfo> Infos, int RemovedControlMarkers, int RemovedBlocks) Resolve(XmlDocument document) {
        var containers = FindBlockContainers(document);
        var infos = new List<ResolvedSwitchInfo>();
        var removedMarkers = 0;
        var removedBlocks = 0;

        foreach (var container in containers) {
            var blockNodes = DirectBlockChildren(container);
            var switchBlocks = DocxConditionalBlockParser.Parse(blockNodes, _partName);

            for (var index = switchBlocks.Count - 1; index >= 0; index--) {
                var result = ResolveSwitch(switchBlocks[index]);
                infos.Add(result.Info);
                removedMarkers += result.RemovedMarkers;
                removedBlocks += result.RemovedBlocks;
            }
        }

        return (infos, removedMarkers, removedBlocks);
    }

    private (ResolvedSwitchInfo Info, int RemovedMarkers, int RemovedBlocks) ResolveSwitch(ConditionalSwitchBlock block) {
        _values.TryGetValue(block.Key, out var value);

        List<XmlElement>? keepNodes = null;
        string? selectedCase = null;
        var usedDefault = false;

        if (value is not null) {
            var matchingCase = block.Cases.FirstOrDefault(c => c.Value == value);
            if (matchingCase is not null) {
                keepNodes = matchingCase.ContentNodes;
                selectedCase = value;
            } else {
                switch (_options.UnknownCasePolicy) {
                    case UnknownCasePolicy.Error:
                        throw DocxConditionalAssemblyErrors.NoMatchingCase(block.Key, value, _partName);
                    case UnknownCasePolicy.RemoveBlock:
                        break;
                    case UnknownCasePolicy.UseDefaultIfPresent:
                        if (block.DefaultBlock is not null) {
                            keepNodes = block.DefaultBlock.ContentNodes;
                            usedDefault = true;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        } else {
            switch (_options.MissingSwitchValuePolicy) {
                case MissingSwitchValuePolicy.Error:
                    throw DocxConditionalAssemblyErrors.MissingValueForSwitch(block.Key, _partName);
                case MissingSwitchValuePolicy.RemoveBlock:
                    break;
                case MissingSwitchValuePolicy.UseDefaultIfPresent:
                    if (block.DefaultBlock is not null) {
                        keepNodes = block.DefaultBlock.ContentNodes;
                        usedDefault = true;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var parent = block.SwitchStartNode.ParentNode as XmlElement;
        if (parent is null) {
            return (new ResolvedSwitchInfo {
                Key = block.Key,
                PartName = _partName,
                SelectedCase = selectedCase,
                UsedDefault = usedDefault,
                BlockRemoved = keepNodes is null
            }, 0, 0);
        }

        var allSiblings = parent.ChildNodes.OfType<XmlElement>().ToList();
        var startIndex = allSiblings.FindIndex(x => ReferenceEquals(x, block.SwitchStartNode));
        var endIndex = allSiblings.FindIndex(x => ReferenceEquals(x, block.SwitchEndNode));

        if (startIndex < 0 || endIndex < 0 || startIndex > endIndex) {
            return (new ResolvedSwitchInfo {
                Key = block.Key,
                PartName = _partName,
                SelectedCase = selectedCase,
                UsedDefault = usedDefault,
                BlockRemoved = keepNodes is null
            }, 0, 0);
        }

        var nodesInSwitch = allSiblings.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
        var keepSet = keepNodes?.ToHashSet() ?? [];
        var removedMarkers = 0;
        var removedBlocks = 0;

        foreach (var node in nodesInSwitch) {
            if (IsMarker(node, block)) {
                removedMarkers++;
            } else if (!keepSet.Contains(node)) {
                removedBlocks++;
            }

            parent.RemoveChild(node);
        }

        if (keepNodes is not null && keepNodes.Count > 0) {
            XmlNode? anchor = parent.ChildNodes.Count > startIndex ? parent.ChildNodes[startIndex] : null;
            foreach (var node in keepNodes) {
                if (anchor is null) {
                    parent.AppendChild(node);
                } else {
                    parent.InsertBefore(node, anchor);
                }
            }
        }

        return (new ResolvedSwitchInfo {
            Key = block.Key,
            PartName = _partName,
            SelectedCase = selectedCase,
            UsedDefault = usedDefault,
            BlockRemoved = keepNodes is null
        }, removedMarkers, removedBlocks);
    }

    private static bool IsMarker(XmlElement node, ConditionalSwitchBlock block) {
        if (ReferenceEquals(node, block.SwitchStartNode) || ReferenceEquals(node, block.SwitchEndNode)) {
            return true;
        }

        foreach (var item in block.Cases) {
            if (ReferenceEquals(node, item.CaseStartNode) || ReferenceEquals(node, item.CaseEndNode)) {
                return true;
            }
        }

        return block.DefaultBlock is not null &&
               (ReferenceEquals(node, block.DefaultBlock.DefaultStartNode) ||
                ReferenceEquals(node, block.DefaultBlock.DefaultEndNode));
    }

    private static List<XmlElement> FindBlockContainers(XmlDocument document) {
        var xpaths = new[] {
            "//*[local-name()='body']",
            "//*[local-name()='hdr']",
            "//*[local-name()='ftr']",
            "//*[local-name()='footnote']",
            "//*[local-name()='endnote']",
            "//*[local-name()='comment']"
        };

        var result = new List<XmlElement>();
        foreach (var xpath in xpaths) {
            var nodes = document.SelectNodes(xpath)?.OfType<XmlElement>();
            if (nodes is not null) {
                result.AddRange(nodes);
            }
        }

        return result;
    }

    private static List<XmlElement> DirectBlockChildren(XmlElement element) =>
        element.ChildNodes.OfType<XmlElement>().ToList();
}
