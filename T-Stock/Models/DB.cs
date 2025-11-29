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

        [Required(ErrorMessage = "Item Name is required.")]
        public string? ItemName { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public string? Category { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }
    }

    public class InventoryListVM
    {
        public List<Inventory> Items { get; set; } = new List<Inventory>();
    }
}
