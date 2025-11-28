using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;

namespace T_Stock.Models
{
    // Database connection class
    public class DB
    {
        private readonly IMongoDatabase _db;

        // Constructor accepts IMongoDatabase (injected via DI)
        public DB(IMongoDatabase db)
        {
            _db = db;
        }

        // Expose the Inventory collection
        public IMongoCollection<Inventory> InventoryCollection =>
            _db.GetCollection<Inventory>("InventoryManagement");
    }

    // Inventory model
    public class Inventory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("ItemName")]
        [Required]
        public string? ItemName { get; set; }

        [BsonElement("Category")]
        [Required]
        public string? Category { get; set; }

        [BsonElement("Quantity")]
        [Required]
        public int Quantity { get; set; }

        [BsonElement("Price")]
        [Required]
        public decimal Price { get; set; }
    }

}
