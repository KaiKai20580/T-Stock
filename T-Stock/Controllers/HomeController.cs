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
        // 1. Total Stock Quantity
        var totalQuantity = _db.ProductCollection.AsQueryable().Sum(p => p.Quantity);
        ViewBag.TotalStockQuantity = totalQuantity;

        // 2. Total Transactions
        var transactionCount = _db.StockTransactionCollection.CountDocuments(FilterDefinition<StockTransaction>.Empty);
        ViewBag.TotalTransactions = transactionCount;

        // 3. NEW LOGIC: "Out of Stock" (Quantity <= ReorderLevel)
        // We use AsQueryable() because it handles comparing two fields (Quantity vs ReorderLevel) efficiently.
        var outOfStockCount = _db.ProductCollection.AsQueryable()
                                 .Where(p => p.Quantity <= p.ReorderLevel)
                                 .Count();

        ViewBag.OutOfStockCount = outOfStockCount;

        // 4. AJAX Check
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_IndexPartial");
        }

        return View();
    }

    // HomeController
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
