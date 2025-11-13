using System.Collections.Generic;

namespace QOL;

public class HybridAutoParams(List<List<string>> indexedParams, Dictionary<string, object> treeParams)
{
    public List<List<string>> IndexedParams { get; set; } = indexedParams;
    public Dictionary<string, object> TreeParams { get; set; } = treeParams;
}
