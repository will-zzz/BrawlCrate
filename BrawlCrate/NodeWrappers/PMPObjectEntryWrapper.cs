using BrawlLib.SSBB;
using BrawlLib.SSBB.ResourceNodes;

namespace BrawlCrate.NodeWrappers
{
    [NodeWrapper(ResourceType.PMPObjectEntry)]
    public class PMPObjectEntryWrapper : GenericWrapper
    {
        public override string ExportFilter => FileFilters.Raw;
        public override string ImportFilter => FileFilters.Raw;

        public PMPObjectEntryWrapper() { }
    }
}
