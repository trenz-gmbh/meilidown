using TRENZ.Docs.API.Interfaces;
using TRENZ.Docs.API.Models;
using TRENZ.Docs.API.Models.Sources;

namespace TRENZ.Docs.API.Services
{
    /// <inheritdoc/>
    public class TreeOrderService : ITreeOrderService
    {
        private readonly ISourcesProvider _sourcesProvider;
        private readonly ILogger<TreeOrderService> _logger;

        public TreeOrderService(ISourcesProvider sourcesProvider,
                                ILogger<TreeOrderService> logger)
        {
            _sourcesProvider = sourcesProvider;
            _logger = logger;
        }

        public async Task ReorderTree(Dictionary<string, NavNode> tree)
        {
            var orderFiles = _sourcesProvider.GetSources()
                                             .SelectMany(source => source.FindFiles(new("\\.order")))
                                             .ToDictionary(sf => sf.RelativePath.Split(Path.DirectorySeparatorChar)[0..^1],
                                                           sf => sf);

            await SetOrder(tree, orderFiles);
        }

        private async Task SetOrder(Dictionary<string, NavNode> tree, Dictionary<string[], ISourceFile> orderFiles)
        {
            int i = 0;

            foreach (var kvp in tree.OrderBy(x => x.Key))
            {
                var node = kvp.Value;

                await SetOrderByParent(node, orderFiles, i++);
            }

            await SetChildrenOrderByOrderFile(new string[] { }, tree.Values, orderFiles);
        }

        private async Task SetOrderByParent(NavNode node, Dictionary<string[], ISourceFile> orderFiles, int index)
        {
            node.Order = index;

            if (node.Children == null)
                return;

            int childIndex = 0;

            // recurse all children
            foreach (var treeNode in node.Children.OrderBy(x => x.Key))
                await SetOrderByParent(treeNode.Value, orderFiles, childIndex++);

            await SetChildrenOrderByOrderFile(node.LocationParts, node.Children.Values, orderFiles);
        }

        private async Task SetChildrenOrderByOrderFile(IEnumerable<string> pathParts,
                                                       IEnumerable<NavNode> children,
                                                       Dictionary<string[], ISourceFile> orderFiles)
        {
            if (children == null)
                return;

            // if this particular folder has a .order, override the order
            var orderFile = orderFiles.SingleOrDefault(of => of.Key.SequenceEqual(pathParts));

            if (orderFile.Value == null)
                return;

            var lines = await orderFile.Value.GetLinesAsync();

            foreach (var item in children)
            {
                var newIndex = Array.IndexOf(lines, item.FileName);
                item.Order = newIndex;

                _logger.LogDebug(newIndex < 0 ? $"Hiding {item.Location}, according to `.order`" : $"Moving {item.Location} to {newIndex}, according to `.order`");
            }
        }
    }
}
