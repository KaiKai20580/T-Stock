using MongoDB.Bson;
using MongoDB.Driver;
using T_Stock.Models;

namespace T_Stock.Helpers
{
    public static class POFilterBuilder
    {
        public static FilterDefinition<PurchaseOrder> Build(PagingQuery q)
        {
            var f = Builders<PurchaseOrder>.Filter;
            var filter = f.Empty;

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                filter &= f.Or(
                    f.Regex(p => p.PO_ID, new BsonRegularExpression(q.Search, "i")),
                    f.Regex(p => p.SupplierID, new BsonRegularExpression(q.Search, "i")),
                    f.Regex(p => p.UserID, new BsonRegularExpression(q.Search, "i")),
                    f.Regex(p => p.Status, new BsonRegularExpression(q.Search, "i"))
                );
            }

            return filter;
        }
    }
}
