using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver; // Required for MongoDB methods
using T_Stock.Models;
using System.Linq;

namespace T_Stock.Controllers
{
    public class NotificationController : Controller
    {
        private readonly DB _db;

        public NotificationController(DB context)
        {
            _db = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // FIX: Use ProductCollection and MongoDB 'Find' syntax
            // _ => true means "get all documents"
            var products = _db.ProductCollection.Find(_ => true).ToList();

            // Handle AJAX requests (e.g., if loading into a popup/modal)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_NotiPartial", products);
            }

            // Normal Page Load
            return View("Notification", products);
        }

        [HttpGet]
        public IActionResult GetAlertCount()
        {
            // 1. Fetch all products
            var products = _db.ProductCollection.Find(_ => true).ToList();

            // 2. Count Out of Stock (Quantity == 0)
            var outOfStock = products.Count(p => p.Quantity == 0);

            // 3. Count Low Stock (Quantity <= ReorderLevel && Quantity > 0)
            var lowStock = products.Count(p => p.Quantity > 0 && p.Quantity <= p.ReorderLevel);

            var total = outOfStock + lowStock;

            // Return the count as JSON
            return Json(new { count = total });
        }
    }
}