using MongoDB.Bson;
using MongoDB.Driver;
using T_Stock.Models;

namespace T_Stock.Helpers
{
    public static class SupplierFilterBuilder
    {
        public static FilterDefinition<Supplier> Build(PagingQuery q)
        {
            var f = Builders<Supplier>.Filter;
            var filter = f.Empty;

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                filter &= f.Or(
                    f.Regex(s => s.SupplierId, new BsonRegularExpression(q.Search, "i")),
                    f.Regex(s => s.Company, new BsonRegularExpression(q.Search, "i")),
                    f.Regex(s => s.ContactPerson, new BsonRegularExpression(q.Search, "i")),
                    f.Regex(s => s.PhoneNumber, new BsonRegularExpression(q.Search, "i"))
                );
            }

            return filter;
        }
    }
}
