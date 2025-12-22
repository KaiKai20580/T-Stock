using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using System.Text.RegularExpressions;
using T_Stock.Models;

namespace T_Stock.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DB _db;

    public HomeController(ILogger<HomeController> logger, DB db)
    {
        _logger = logger;
        _db = db;
    }

    public IActionResult Index()
    {
        // ==========================================
        // 1. Total Stock Quantity
        // ==========================================
        var totalQuantity = _db.ProductCollection.AsQueryable().Sum(p => p.Quantity);
        ViewBag.TotalStockQuantity = totalQuantity;

        // ==========================================
        // 2. Total Transactions
        // ==========================================
        var transactionCount = _db.StockTransaction.CountDocuments(FilterDefinition<StockTransaction>.Empty);
        ViewBag.TotalTransactions = transactionCount;

        // ==========================================
        // 3. "Out of Stock" (Quantity <= ReorderLevel)
        // ==========================================
        var outOfStockCount = _db.ProductCollection.AsQueryable()
                                         .Where(p => p.Quantity <= p.ReorderLevel)
                                         .Count();
        ViewBag.OutOfStockCount = outOfStockCount;

        // ==========================================
        // 4. Purchase Orders & Pending Count (With Role Security)
        // ==========================================
        long totalOrders = 0;
        long pendingOrders = 0; // <--- NEW VARIABLE

        // Get User Info from Cookies
        var userRole = Request.Cookies["Role"];
        var userEmail = Request.Cookies["User"];

        if (userRole == "Supplier" && !string.IsNullOrEmpty(userEmail))
        {
            // 4a. If Supplier: Look up their ID using SupplierCollection
            var currentSupplier = _db.SupplierCollection
                                     .Find(s => s.Email == userEmail)
                                     .FirstOrDefault();

            if (currentSupplier != null)
            {
                // Count TOTAL orders for this Supplier
                totalOrders = _db.PurchaseOrderCollection
                                 .CountDocuments(p => p.SupplierID == currentSupplier.SupplierId);

                // Count PENDING orders for this Supplier
                pendingOrders = _db.PurchaseOrderCollection
                                   .CountDocuments(p => p.SupplierID == currentSupplier.SupplierId && p.Status == "Pending");
            }
            else
            {
                totalOrders = 0;
                pendingOrders = 0;
            }
        }
        else
        {
            // 4b. Admin/Staff/Manager: Sees ALL orders
            totalOrders = _db.PurchaseOrderCollection
                             .CountDocuments(FilterDefinition<PurchaseOrder>.Empty);

            // Count ALL Pending orders
            pendingOrders = _db.PurchaseOrderCollection
                               .CountDocuments(p => p.Status == "Pending");
        }

        // Store in ViewBag for the View to render
        ViewBag.TotalOrders = totalOrders;
        ViewBag.PendingPOCount = pendingOrders; // <--- PASS TO VIEW


        // ==========================================
        // 5. AJAX Check (Return Partial if requested)
        // ==========================================
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_IndexPartial");
        }

        return View();
    }
    public IActionResult Search(string q)
    {
        var results = new List<ProductViewModel>();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var filter = Builders<Product>.Filter.Regex(
                "ProductName",
                new BsonRegularExpression("^" + Regex.Escape(q), "i")
            );

            var products = _db.ProductCollection
                .Find(filter)
                .SortBy(x => x.ProductName)
                .Limit(10)
                .ToList();

            results = products.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName
            }).ToList();
        }

        return PartialView("_SearchResults", results);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}