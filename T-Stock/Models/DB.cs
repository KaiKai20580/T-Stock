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

        public IMongoCollection<Supplier> SupplierCollection =>
            _db.GetCollection<Supplier>("Supplier");

        public IMongoCollection<Product> ProductCollection =>
            _db.GetCollection<Product>("Product");

        public IMongoCollection<SupplierProduct> SupplierProductCollection =>
            _db.GetCollection<SupplierProduct>("SupplierProduct");
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

    public class Supplier
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required]
        public string Id { get; set; }

        [BsonElement("SupplierID")]
        [Required]
        public string SupplierId { get; set; }

        [BsonElement("Company")]
        [Required(ErrorMessage="Company name is required")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-',&().]+$", ErrorMessage = "Company name can only contain letters, numbers, spaces, hyphens (-), commas (,), apostrophes ('), ampersands (&), and parentheses ().")]
        public string Company { get; set; }

        [BsonElement("ContactPerson")]
        [Required(ErrorMessage = "Contact person is required")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name can only contain letters and spaces.")]
        public string ContactPerson { get; set; }

        [BsonElement("Email")]
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress]
        public string Email { get; set; }

        [BsonElement("PhoneNumber")]
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[0]{1}[0-9]{9-10}+$", ErrorMessage = "Phone number must only consist of 10-11 numbers")]
        public string PhoneNumber { get; set; }

        [BsonElement("Address")]
        [Required(ErrorMessage = "Company address is required")]
        [RegularExpression(@"^[a-zA-Z0-9\-',]+$", ErrorMessage = "Address can only contain letters, numbers, hyphen(-), apostrophes('), commas(,) and spaces.")]
        public string Address { get; set; }

        [BsonElement("LastUpdated")]
        [Required]
        public DateTime LastUpdated { get; set; }
    }

    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("ProductID")]
        [Required]
        public string ProductId { get; set; }

        [BsonElement("ProductName")]
        [Required]
        public string ProductName { get; set; }

        [BsonElement("Category")]
        [Required]
        public string Category { get; set; }

        [BsonElement("Quantity")]
        [Required]
        public int Quantity { get; set; }

        [BsonElement("ReorderLevel")]
        [Required]
        public int ReorderLevel { get; set; }
    }

    public class SupplierProduct
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("SupplierID")]
        [Required]
        public string SupplierID { get; set; }

        [BsonElement("ProductID")]
        [Required]
        public string ProductID { get; set; }

        [BsonElement("SupplierPrice")]
        [Required]
        public double SupplierPrice { get; set; }
    }


    public class InventoryListVM
    {
        public List<Inventory> Items { get; set; } = new List<Inventory>();
    }

}
