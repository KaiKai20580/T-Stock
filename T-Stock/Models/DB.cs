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

        // =========================
        // 方案B：通用方法
        // =========================
        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _db.GetCollection<T>(collectionName);
        }
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
    

    // ViewModel for passing to Views
    public class InventoryListVM
    {
        public List<Inventory> Items { get; set; } = new List<Inventory>();
    }
    public class Supplier
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }               // MongoDB 的 _id（可以保留）

        public string? SupplierID { get; set; }       // 你数据库实际的字段
        public string? Company { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class PurchaseOrder
    {
        [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }              
    public string PO_ID { get; set; } = null!;     
    public string SupplierID { get; set; } = null!;
    public string? UserID { get; set; }
    public string Status { get; set; } = null!;    // Pending, Approved, Completed...
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastUpdated {  get; set; }
    public string? Remarks { get; set; }
}
    public class PurchaseOrderItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }              // test1 
        public string PO_ID { get; set; } = null!;     
        public string ProductID { get; set; } = null!;
        public int QuantityOrdered { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }       // QuantityOrdered × UnitPrice
    }

    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ? Id { get; set; }
        public string? ProductID { get; set; }
        public string? ProductName { get; set; }
        public string? Category { get; set; }
        public int Quantity { get; set; }
        public int ReorderLevel { get; set; }
    }

    
}
