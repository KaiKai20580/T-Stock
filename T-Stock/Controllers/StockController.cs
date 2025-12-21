using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;
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

        // --- INDEX: Lists Transaction Headers + Data for Lookups ---
        public async Task<IActionResult> Index()
        {
            // 1. Check for Receipt (New Transaction Created)
            if (TempData["CreatedTransactionID"] != null)
            {
                string newTxId = TempData["CreatedTransactionID"].ToString();
                ViewBag.NewTxId = newTxId;
                // You can load specific details for a receipt modal here if needed
            }

            // 2. Load History (Headers)
            var transactions = await _db.StockTransaction
                                    .Find(_ => true)
                                    .SortByDescending(tx => tx.Date)
                                    .ToListAsync();

            // 3. Load Related Data (Items & Products) for Lookups
            // In a large system, you would paginate 'transactions' first, then only load IDs found in those transactions.
            var txIds = transactions.Select(t => t.TransactionID).ToList();

            var relatedItems = await _db.StockTransactionItemCollection
                                    .Find(i => txIds.Contains(i.TransactionID))
                                    .ToListAsync();

            var allProducts = await _db.ProductCollection
                                    .Find(_ => true)
                                    .ToListAsync();

            // 4. Construct VM
            var model = new StockTransactionListVM
            {
                Items = transactions,           // The Headers
                TransactionItems = relatedItems,// The Details (Rows)
                Products = allProducts          // The Master Data (for Name lookups)
            };

            return View(model);
        }

        // --- STOCK TABLE PARTIAL (Refreshed via AJAX) ---
        // Updated to return Transactions, not Products, matching your _StockTable view
        public async Task<IActionResult> StockTable(string search, string type, string dateFrom, string dateTo)
        {
            var builder = Builders<StockTransaction>.Filter;
            var filter = builder.Empty;

            // 1. Apply Search (Transaction ID or Reason)
            if (!string.IsNullOrEmpty(search))
            {
                var regex = new BsonRegularExpression(search, "i");
                filter &= builder.Or(
                    builder.Regex(x => x.TransactionID, regex),
                    builder.Regex(x => x.Reason, regex)
                );
            }

            // 2. Apply Type Filter (IN/OUT)
            if (!string.IsNullOrEmpty(type) && type != "all")
            {
                filter &= builder.Eq(x => x.transactionType, type);
            }

            // 3. Apply Date Filter
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime dtFrom))
            {
                filter &= builder.Gte(x => x.Date, dtFrom);
            }
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime dtTo))
            {
                // Add 1 day to include the end date fully
                filter &= builder.Lt(x => x.Date, dtTo.AddDays(1));
            }

            // 4. Fetch Results
            var transactions = await _db.StockTransaction
                                    .Find(filter)
                                    .SortByDescending(tx => tx.Date)
                                    .ToListAsync();

            // 5. Fetch Related Data for these specific transactions
            var txIds = transactions.Select(t => t.TransactionID).ToList();
            var relatedItems = await _db.StockTransactionItemCollection
                                    .Find(i => txIds.Contains(i.TransactionID))
                                    .ToListAsync();

            // Optimization: In real apps, cache products or specific lookup. Here we load all.
            var allProducts = await _db.ProductCollection
                                    .Find(_ => true)
                                    .ToListAsync();

            var model = new StockTransactionListVM
            {
                Items = transactions,
                TransactionItems = relatedItems,
                Products = allProducts
            };

            return PartialView("_StockTable", model);
        }

        // --- GET SINGLE TRANSACTION DETAIL (For Modal/Expansion) ---
        public async Task<IActionResult> GetTransactions(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return BadRequest();

            // 1. Fetch Items for this Transaction
            var items = await _db.StockTransactionItemCollection
                            .Find(t => t.TransactionID == transactionId)
                            .ToListAsync();

            // 2. Fetch Products (Required for the partial to lookup names)
            var allProducts = await _db.ProductCollection
                            .Find(_ => true)
                            .ToListAsync();

            // 3. Construct VM
            var model = new StockTransactionListVM
            {
                TransactionItems = items,
                Products = allProducts
            };

            // 4. Pass the ID so the Partial knows what to filter (if logic requires it)
            ViewData["TransactionID"] = transactionId;

            return PartialView("_StockTransactionDetail", model);
        }

        // --- CREATE (GET) ---
        [HttpGet]
        public async Task<IActionResult> CreateTransaction()
        {
            var vm = new StockTransactionListVM
            {
                Products = await _db.ProductCollection.Find(_ => true).ToListAsync(),
                Items = new List<StockTransaction> { new StockTransaction() },
                TransactionItems = new List<StockTransactionItem> { new StockTransactionItem { QtyChange = 1 } }
            };

            return View("_Create", vm);
        }

        // --- CREATE (POST) ---
        [HttpPost]
        public async Task<IActionResult> CreateTransaction(StockTransactionListVM model)
        {
            // The Header info is expected in model.Items[0]
            var headerInput = model.Items?.FirstOrDefault();

            // 1. Basic Validation
            if (headerInput == null || model.TransactionItems == null || !model.TransactionItems.Any())
            {
                ModelState.AddModelError("", "Please provide transaction details.");
                model.Products = await _db.ProductCollection.Find(_ => true).ToListAsync();
                return View("_Create", model);
            }

            // 2. Clear Validation for Generated Fields
            ModelState.Remove("Items[0].TransactionID");
            ModelState.Remove("Items[0].UserID");
            ModelState.Remove("Items[0].Date");
            ModelState.Remove("Items[0].Id");

            for (int i = 0; i < model.TransactionItems.Count; i++)
            {
                ModelState.Remove($"TransactionItems[{i}].TransactionID");
                ModelState.Remove($"TransactionItems[{i}].Id");
                ModelState.Remove($"TransactionItems[{i}].ProductID");
            }

            // 3. Setup IDs
            // Generate Transaction ID (T0001, etc.)
            var lastTx = await _db.StockTransaction.Find(_ => true).SortByDescending(t => t.TransactionID).FirstOrDefaultAsync();
            string newBatchId = "T0001";
            if (lastTx != null && !string.IsNullOrEmpty(lastTx.TransactionID))
            {
                string numericPart = lastTx.TransactionID.Substring(1);
                if (int.TryParse(numericPart, out int currentNum))
                {
                    newBatchId = $"T{currentNum + 1:D4}";
                }
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "Unknown";
            DateTime currentDate = DateTime.Now;

            // 4. Process Logic
            headerInput.TransactionID = newBatchId;
            headerInput.UserID = currentUserId;
            headerInput.Date = currentDate;

            var validItemsToInsert = new List<StockTransactionItem>();

            foreach (var item in model.TransactionItems)
            {
                if (string.IsNullOrEmpty(item.ProductID)) continue;

                item.TransactionID = newBatchId;

                var product = await _db.ProductCollection.Find(p => p.ProductId == item.ProductID).FirstOrDefaultAsync();

                if (product != null)
                {
                    int changeAmount = item.QtyChange;

                    // Handle 'OUT' logic (Negative Stock)
                    if (headerInput.transactionType == "OUT")
                    {
                        if (product.Quantity < changeAmount)
                        {
                            ModelState.AddModelError("", $"Insufficient stock for {product.ProductName}. Current: {product.Quantity}");
                            model.Products = await _db.ProductCollection.Find(_ => true).ToListAsync();
                            return View("_Create", model);
                        }
                        changeAmount = -changeAmount;
                    }

                    // Update Inventory
                    var updateDef = Builders<Product>.Update.Inc(p => p.Quantity, changeAmount);
                    await _db.ProductCollection.UpdateOneAsync(p => p.ProductId == item.ProductID, updateDef);

                    validItemsToInsert.Add(item);
                }
            }

            // 5. Save
            if (validItemsToInsert.Count > 0)
            {
                await _db.StockTransaction.InsertOneAsync(headerInput);
                await _db.StockTransactionItemCollection.InsertManyAsync(validItemsToInsert);

                TempData["CreatedTransactionID"] = newBatchId;
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "No valid items to process.");
            model.Products = await _db.ProductCollection.Find(_ => true).ToListAsync();
            return View("_Create", model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            // Note: This logic only deletes the header. In a real app, you should also 
            // 1. Delete the associated Items
            // 2. Reverse the Stock effect (if needed)

            await _db.StockTransaction.DeleteOneAsync(x => x.Id == id);
            return RedirectToAction("Index");
        }
    }
}