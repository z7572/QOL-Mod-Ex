using System.Collections.Generic;

namespace QOL;

public class HybridAutoParams
{
    public List<List<string>> IndexedParams { get; set; }
    public Dictionary<string, object> TreeParams { get; set; }

    public HybridAutoParams(List<List<string>> indexedParams, Dictionary<string, object> treeParams)
    {
        IndexedParams = indexedParams;
        TreeParams = treeParams;
    }
}
