using TRENZ.Docs.API.Interfaces;
using TRENZ.Docs.API.Models;
using TRENZ.Docs.API.Models.Sources;

namespace TRENZ.Docs.API.Services;

public class NodeFlaggingService : INodeFlaggingService
{
    private readonly ISourcesProvider _sourcesProvider;

    public NodeFlaggingService(ISourcesProvider sourcesProvider)
    {
        _sourcesProvider = sourcesProvider;
    }

    /// <inheritdoc />
    public async Task UpdateHasContentFlag(Dictionary<string, NavNode> tree)
    {
        var contentFiles = _sourcesProvider.GetSources()
                .SelectMany(source => source.FindFiles(new(".*\\.md$")))
                .ToDictionary(
                    sf => sf.Location.Split(NavNode.Separator),
                    sf => sf
                )
            ;

        await SetContentFlag(tree, contentFiles);
    }

    private static async Task SetContentFlag(Dictionary<string, NavNode> tree, Dictionary<string[], ISourceFile> contentFiles)
    {
        foreach (var node in tree.Values)
        {
            if (node.Children == null)
            {
                node.HasContent = true; // nodes without children are always content, otherwise they shouldn't appear in the tree

                continue;
            }

            await SetContentFlag(node.Children!, contentFiles);

            var contentFile = contentFiles.SingleOrDefault(cf => cf.Key.SequenceEqual(node.LocationParts));
            node.HasContent = contentFile.Value != null;
        }
    }
}