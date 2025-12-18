using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text.Json; // Required for serializing data for the redirect
using T_Stock.Models;

namespace T_Stock.Controllers
{
    public class StockController : Controller
    {
        private readonly DB _db;

        public StockController(DB db)
        {
            _db = db;
        }

        // --- UPDATED INDEX METHOD ---
        // Main page - list all stock transactions
        public IActionResult Index()
        {
            // 1. Check if we have a "Receipt" from a recently created transaction
            //    (This comes from the CreateTransaction method via TempData)
            if (TempData["CreatedTransactions"] != null)
            {
                var json = TempData["CreatedTransactions"].ToString();
                if (!string.IsNullOrEmpty(json))
                {
                    var newItems = JsonSerializer.Deserialize<List<StockTransaction>>(json);
                    ViewBag.NewTransactionReceipt = newItems; // Pass this to the View
                }
            }

            // 2. Standard Load
            var transactions = _db.StockTransactionCollection
                                  .Find(_ => true)
                                  .SortByDescending(tx => tx.Date)
                                  .ToList();

            var model = new StockTransactionListVM
            {
                Items = transactions
            };

            return View(model);
        }

        // Partial: Get transactions filtered by TransactionID or UserID
        // In StockController.cs

        public IActionResult GetTransactions(string? transactionId = null, string? userId = null)
        {
            var filter = Builders<StockTransaction>.Filter.Empty;

            if (!string.IsNullOrEmpty(transactionId))
                filter = Builders<StockTransaction>.Filter.Eq(tx => tx.TransactionID, transactionId);

            if (!string.IsNullOrEmpty(userId))
                filter = Builders<StockTransaction>.Filter.Eq(tx => tx.UserID, userId);

            var transactions = _db.StockTransactionCollection
                                  .Find(filter)
                                  .SortByDescending(tx => tx.Date)
                                  .ToList();

            var model = new StockTransactionListVM
            {
                Items = transactions
            };

            // --- FIX IS HERE ---
            // Pass 'model.Items' (the List) instead of 'model' (the VM)
            return PartialView("_StockTransactionDetail", model.Items);
        }

        // Partial: Stock table for products with filtering
        public IActionResult StockTable(string? search, string? category, string? stockLevel)
        {
            var filter = Builders<Product>.Filter.Empty;

            // Search filter
            if (!string.IsNullOrEmpty(search))
                filter &= Builders<Product>.Filter.Regex(i => i.ProductName, new BsonRegularExpression(search, "i"));

            // Category filter
            if (!string.IsNullOrEmpty(category) && category != "all")
                filter &= Builders<Product>.Filter.Eq(i => i.Category, category);

            // Stock level filter
            if (!string.IsNullOrEmpty(stockLevel))
            {
                if (stockLevel == "low")
                    filter &= Builders<Product>.Filter.Lte(i => i.Quantity, 5);
                else if (stockLevel == "out")
                    filter &= Builders<Product>.Filter.Eq(i => i.Quantity, 0);
            }

            var products = _db.ProductCollection
                              .Find(filter)
                              .SortBy(i => i.ProductName)
                              .ToList();

            return PartialView("_StockTable", products);
        }

        // Delete a stock transaction
        [HttpPost]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            _db.StockTransactionCollection.DeleteOne(x => x.Id == id);

            // Return updated list in StockTransactionListVM
            var transactions = _db.StockTransactionCollection
                                  .Find(FilterDefinition<StockTransaction>.Empty)
                                  .SortByDescending(tx => tx.Date)
                                  .ToList();

            var model = new StockTransactionListVM
            {
                Items = transactions
            };

            return PartialView("_Create", model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateTransaction()
        {
            var products = await _db.ProductCollection.Find(_ => true).ToListAsync();

            var viewModel = new StockTransactionListVM
            {
                Items = new List<StockTransaction>(),
                Products = products
            };

            // Check if the request is an AJAX call (sent by jQuery/JavaScript)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // AJAX: Return only the HTML fragment (no layout)
                return PartialView("_Create", viewModel);
            }

            // Normal Request (Browser URL): Return the full page (with layout)
            return View("_Create", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction(StockTransactionListVM model)
        {
            // 1. Validation Clean-up
            if (model.Items != null)
            {
                for (int i = 0; i < model.Items.Count; i++)
                {
                    ModelState.Remove($"Items[{i}].TransactionID");
                    ModelState.Remove($"Items[{i}].UserID");
                    ModelState.Remove($"Items[{i}].Date");
                    ModelState.Remove($"Items[{i}].Id");
                    ModelState.Remove($"Items[{i}].ProductId");
                }
            }

            // 2. Check Validation
            if (!ModelState.IsValid)
            {
                model.Products = await _db.ProductCollection.Find(_ => true).ToListAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_Create", model);
                }
                return View(model);
            }

            // =========================================================================
            // 3. Setup Variables
            // =========================================================================

            // --- A. Generate Transaction ID (e.g., T0001) ---
            var lastTx = await _db.StockTransactionCollection
                .Find(_ => true)
                .SortByDescending(t => t.TransactionID)
                .FirstOrDefaultAsync();

            string batchId = "T0001";

            if (lastTx != null && !string.IsNullOrEmpty(lastTx.TransactionID))
            {
                string numericPart = lastTx.TransactionID.Substring(1);
                if (int.TryParse(numericPart, out int currentNum))
                {
                    batchId = $"T{currentNum + 1:D4}";
                }
            }

            // --- B. DETECT CURRENT LOGGED-IN USER ID ---
            // We check the standard NameIdentifier claim (User ID) first, then fallback to Name (Username)
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                currentUserId = User.Identity?.Name;
            }

            // Fallback if user is somehow not logged in (optional safety net)
            if (string.IsNullOrEmpty(currentUserId))
            {
                currentUserId = "UnknownUser";
            }

            DateTime currentDate = DateTime.Now;

            // =========================================================================

            if (model.Items != null)
            {
                foreach (var item in model.Items)
                {
                    // C. Fill Transaction Details
                    item.TransactionID = batchId;
                    item.UserID = currentUserId; // <--- Uses the detected ID
                    item.Date = currentDate;

                    if (!string.IsNullOrEmpty(item.ProductName))
                    {
                        var filter = Builders<Product>.Filter.Eq(p => p.ProductName, item.ProductName);
                        var existingProduct = await _db.ProductCollection.Find(filter).FirstOrDefaultAsync();

                        if (existingProduct != null)
                        {
                            // Update Stock
                            item.ProductId = existingProduct.ProductId;

                            var update = item.TransactionType == "IN"
                                ? Builders<Product>.Update.Inc(p => p.Quantity, item.Quantity)
                                : Builders<Product>.Update.Inc(p => p.Quantity, -item.Quantity);

                            await _db.ProductCollection.UpdateOneAsync(filter, update);
                        }
                        else
                        {
                            // Error: Product not found
                            ModelState.AddModelError("", $"Product '{item.ProductName}' does not exist in the database.");
                            model.Products = await _db.ProductCollection.Find(_ => true).ToListAsync();
                            return View(model);
                        }
                    }
                }

                // D. Save Transactions to MongoDB
                if (model.Items.Count > 0)
                {
                    await _db.StockTransactionCollection.InsertManyAsync(model.Items);
                }
            }

            // 4. Success Response
            TempData["CreatedTransactions"] = System.Text.Json.JsonSerializer.Serialize(model.Items);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Stock") });
            }

            return RedirectToAction("Index", "Stock");
        }
    }
}