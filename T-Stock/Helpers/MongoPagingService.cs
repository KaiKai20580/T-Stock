using MongoDB.Driver;
using T_Stock.Models;

namespace T_Stock.Helpers
{
    public class MongoPagingService
    {

        public async Task<IPagedResult<T>> PagedAsync<T>(
            IMongoCollection<T> collection,
            PagingQuery query,
            FilterDefinition<T> filter,
            SortDefinition<T> sort)
        {
            var total = await collection.CountDocumentsAsync(filter);

            var items = await collection.Find(filter)
                .Sort(sort)
                .Skip((query.Page - 1) * query.PageSize)
                .Limit(query.PageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = total
            };
        }

    }

}
