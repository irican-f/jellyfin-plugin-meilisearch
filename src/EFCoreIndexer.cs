using System.Collections.Immutable;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Meilisearch;

public class EfCoreIndexer(
    IJellyfinDatabaseProvider dbProvider,
    MeilisearchClientHolder clientHolder,
    ILogger<DbIndexer> logger
) : Indexer(clientHolder, logger)
{
    protected override Task<ImmutableList<MeilisearchItem>> GetItems()
    {
        var context = dbProvider.DbContextFactory!.CreateDbContext();
        Status["Database"] = context.Database.GetDbConnection().ConnectionString;

        // Get all CollectionFolder (library) IDs
        var libraryIds = context.BaseItems
            .Where(b => b.Type != null && b.Type.Contains("CollectionFolder"))
            .Select(b => b.Id)
            .ToHashSet();

        // Build a map of item -> library ID using ancestors
        var itemToLibrary = context.AncestorIds
            .Where(a => libraryIds.Contains(a.ParentItemId))
            .ToDictionary(a => a.ItemId, a => a.ParentItemId.ToString("N"));

        var items = context.BaseItems.ToImmutableList();
        return Task.FromResult(items.Select(item => ToMeilisearchItem(item, itemToLibrary)).ToImmutableList());
    }

    private static MeilisearchItem ToMeilisearchItem(BaseItemEntity item, Dictionary<Guid, string> itemToLibrary)
    {
        itemToLibrary.TryGetValue(item.Id, out var libraryId);
        
        return new MeilisearchItem(
            Guid: item.Id.ToString(),
            Type: item.Type,
            ParentId: item.ParentId.ToString(),
            LibraryId: libraryId,
            Name: item.Name,
            Overview: item.Overview,
            OriginalTitle: item.OriginalTitle,
            SeriesName: item.SeriesName,
            Studios: item.Studios?.Split('|'),
            Genres: item.Genres?.Split('|'),
            Tags: item.Tags?.Split('|'),
            CommunityRating: item.CommunityRating,
            ProductionYear: item.ProductionYear,
            Path: item.Path?[0] == '%' ? null : item.Path,
            Artists: item.Artists?.Split('|'),
            AlbumArtists: item.AlbumArtists?.Split('|'),
            CriticRating: item.CriticRating,
            IsFolder: item.IsFolder,
            Tagline: item.Tagline
        );
    }
}