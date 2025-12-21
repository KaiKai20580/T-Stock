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

        public DB(IMongoDatabase db)
        {
            _db = db;
        }

        // Expose the Inventory collection
        public IMongoCollection<Supplier> SupplierCollection =>
            _db.GetCollection<Supplier>("Supplier");

        public IMongoCollection<Product> ProductCollection =>
            _db.GetCollection<Product>("Product");

        public IMongoCollection<SupplierProduct> SupplierProductCollection =>
            _db.GetCollection<SupplierProduct>("SupplierProduct");

        public IMongoCollection<PurchaseOrder> PurchaseOrderCollection =>
            _db.GetCollection<PurchaseOrder>("PurchaseOrder");

        public IMongoCollection<PurchaseOrderItem> PurchaseOrderItemCollection =>
            _db.GetCollection<PurchaseOrderItem>("PurchaseOrderItem");
        public IMongoCollection<StockTransaction> StockTransaction =>
            _db.GetCollection<StockTransaction>("StockTransaction");

        public IMongoCollection<StockTransactionItem> StockTransactionItemCollection =>
            _db.GetCollection<StockTransactionItem>("StockTransactionItem");

        public IMongoCollection<User> User =>
           _db.GetCollection<User>("User");
    }

    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }


        [BsonElement("userid")]
        public string UserId { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonElement("role")]
        public string Role { get; set; }

        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }

    public class StockTransactionListVM
    {
        public List<StockTransaction> Items { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<StockTransactionItem> TransactionItems { get; set; } = new();
    }

    public class Supplier
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required]
        public string? Id { get; set; }

        [BsonElement("SupplierID")]
        [Required]
        public string? SupplierId { get; set; }

        [BsonElement("Company")]
        [Required(ErrorMessage="Company name is required")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-',&().]+$", ErrorMessage = "Company name can only contain letters, numbers, spaces, hyphens (-), commas (,), apostrophes ('), ampersands (&), and parentheses ().")]
        public string? Company { get; set; }

        [BsonElement("ContactPerson")]
        [Required(ErrorMessage = "Contact person is required")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name can only contain letters and spaces.")]
        public string? ContactPerson { get; set; }

        [BsonElement("Email")]
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress]
        public string? Email { get; set; }

        [BsonElement("PhoneNumber")]
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[0]{1}[0-9]{9-10}+$", ErrorMessage = "Phone number must only consist of 10-11 numbers")]
        public string? PhoneNumber { get; set; }

        [BsonElement("Address")]
        [Required(ErrorMessage = "Company address is required")]
        [RegularExpression(@"^[a-zA-Z0-9\-',]+$", ErrorMessage = "Address can only contain letters, numbers, hyphen(-), apostrophes('), commas(,) and spaces.")]
        public string? Address { get; set; }

        [BsonElement("LastUpdated")]
        [Required]
        public DateTime LastUpdated { get; set; }
    }


    public class ProductViewModel
    {
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Page { get; set; }
    }

    public class InventoryTableViewModel
    {
        public List<Product> AllProducts { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public string SortBy { get; set; } = "ProductName";
        public string SortDir { get; set; } = "asc";
    }


    public class ProductListVM
    {
        public List<Product> Items { get; set; } = new List<Product>();
    }

    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("ProductID")]
        public string? ProductId { get; set; }

        [BsonElement("ProductName")]
        [Required(ErrorMessage = "Product Name cannot be empty.")]
        public string? ProductName { get; set; }

        [BsonElement("Category")]
        [Required(ErrorMessage = "Category cannot be empty.")]
        public string? Category { get; set; }

        [BsonElement("Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must at least 1.")]
        public int Quantity { get; set; }

        [BsonElement("ReorderLevel")]
        [Range(1, int.MaxValue, ErrorMessage = "Reorder Level must be at least 1.")]
        public int ReorderLevel { get; set; }

        [BsonElement("Price")]
        [BsonRepresentation(BsonType.Decimal128)]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must at least 0.01.")]
        public decimal Price { get; set; }

        [BsonIgnore]
        public int Page { get; set; }
    }

    public class SupplierProduct
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("SupplierID")]
        [Required]
        public string SupplierId { get; set; }

        [BsonElement("ProductID")]
        [Required]
        public string ProductId { get; set; }

        [BsonElement("SupplierPrice")]
        [Required]
        public double SupplierPrice { get; set; }
    }

    public class StockTransaction
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonElement("TransactionID")]
        [Required]
        public string TransactionID { get; set; }
        [BsonElement("UserID")]
        [Required]
        public string UserID { get; set; }
        [BsonElement("Date")]
        [Required]
        public DateTime Date { get; set; }
        [BsonElement("Reason")]
        [Required]
        public string Reason{ get; set; }
        [BsonElement("transactionType")]
        [Required]
        public string transactionType { get; set; }
    }

    public class StockTransactionItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonElement("TransactionID")]
        [Required]
        public string TransactionID { get; set; }
        [BsonElement("ProductID")]
        [Required]
        public string ProductID { get; set; }
        [BsonElement("QtyChange")]
        [Required]
        public int QtyChange { get; set; }
        [BsonElement("Remarks")]
        public string? Remarks { get; set; }
    }

    public class PurchaseOrder
    {
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

        [BsonElement("PO_ID")]
        public string PO_ID { get; set; } = null!;
        [BsonElement("SupplierID")]
        public string SupplierID { get; set; } = null!;
        [BsonElement("UserID")]
        public string? UserID { get; set; }
        [BsonElement("Status")]
        public string Status { get; set; } = null!;    
        [BsonElement("CreatedDate")]
        public DateTime CreatedDate { get; set; }
        [BsonElement("LastUpdated")]
        public DateTime LastUpdated {  get; set; }
    public string? Remarks { get; set; }
}
    public class PurchaseOrderItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }               
       
        [BsonElement("PO_ID")] 
        public string PO_ID { get; set; } = null!;     
        
        [BsonElement("ProductID")]
        public string ProductId { get; set; } = null!;
        [BsonElement("QuantityOrdered")]
        public int QuantityOrdered { get; set; }
        [BsonElement("UnitPrice")]
        public decimal UnitPrice { get; set; }
        [BsonElement("TotalPrice")]
        public decimal TotalPrice { get; set; }       
    }

}
