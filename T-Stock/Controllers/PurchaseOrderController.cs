using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using T_Stock.Helpers;
using T_Stock.Models;
using System.Web;

namespace T_Stock.Controllers
{
    public class PurchaseOrderController : Controller
    {
        private readonly IMongoCollection<Supplier> _suppliers;
        private readonly IMongoCollection<SupplierProduct> _supplierProducts;
        private readonly IMongoCollection<Product> _products;
        private readonly IMongoCollection<PurchaseOrder> _purchaseOrder;
        private readonly IMongoCollection<PurchaseOrderItem> _purchaseOrderItems;
        private readonly IMongoCollection<StockTransaction> _stockTransaction;
        private readonly IMongoCollection<StockTransactionItem> _stockTransactionItems;
        private readonly IMongoClient _client;
        private readonly IMongoCollection<User> _user;
        private readonly MongoPagingService _paging;

        public PurchaseOrderController(IMongoDatabase db, MongoPagingService paging, IMongoClient client)
        {
            _suppliers = db.GetCollection<Supplier>("Supplier");
            _supplierProducts = db.GetCollection<SupplierProduct>("SupplierProduct");
            _products = db.GetCollection<Product>("Product");
            _purchaseOrder = db.GetCollection<PurchaseOrder>("PurchaseOrder");
            _purchaseOrderItems = db.GetCollection<PurchaseOrderItem>("PurchaseOrderItem");
            _stockTransaction = db.GetCollection<StockTransaction>("StockTransaction");
            _stockTransactionItems = db.GetCollection<StockTransactionItem>("StockTransactionItem");
            _user = db.GetCollection<User>("User");
            _client = client;
            _paging = paging;
        }

        public async Task<IActionResult> Index(PagingQuery q)
        {
            // 1) Base filter (search)
            var filter = POFilterBuilder.Build(q);

            var userRole = Request.Cookies["Role"];
            var userEmail = Request.Cookies["User"]; // Or User.FindFirst(ClaimTypes.Email)?.Value;
            // If the user is a Supplier, restrict the query
            if (userRole == "Supplier" && !string.IsNullOrEmpty(userEmail))
            {
                // Find the User or Supplier record to get the ID
                // Assuming your '_user' collection links Email -> SupplierId
                var currentUser = await _suppliers.Find(s => s.Email == userEmail).FirstOrDefaultAsync();

                if (currentUser != null && !string.IsNullOrEmpty(currentUser.SupplierId))
                {
                    // Append the SupplierId filter using AND (&)
                    // This ensures they ONLY see their own rows, even if they try to search for others
                    filter &= Builders<PurchaseOrder>.Filter.Eq(p => p.SupplierID, currentUser.SupplierId);
                }
                else
                {
                    // Edge Case: User is a "Supplier" but has no mapped SupplierId in DB.
                    // Force the query to return nothing for security.
                    filter &= Builders<PurchaseOrder>.Filter.Eq(p => p.PO_ID, "RESTRICTED_ACCESS");
                }

            }
            // 2) Date filter
            if (!string.IsNullOrWhiteSpace(q.DateType) && q.DateType != "none")
            {
                var dateBuilder = Builders<PurchaseOrder>.Filter;
                //Make sure to include the whole day for DateTo
                DateTime? finalEndDate = q.DateTo.HasValue ? q.DateTo.Value.Date.AddDays(1).AddTicks(-1) : null;
                if (q.DateType.ToLower() == "created")
                {
                    // 1. Start Date Check
                    if (q.DateFrom.HasValue)
                    {
                        filter &= dateBuilder.Gte(p => p.CreatedDate, q.DateFrom.Value);
                    }

                    // 2. End Date Check
                    if (finalEndDate.HasValue)
                    {
                        filter &= dateBuilder.Lte(p => p.CreatedDate, finalEndDate.Value);
                    }
                }
                else if (q.DateType.ToLower() == "lastupdate")
                {
                    // 1. Start Date Check
                    if (q.DateFrom.HasValue)
                    {
                        filter &= dateBuilder.Gte(p => p.LastUpdated, q.DateFrom.Value);
                    }

                    // 2. End Date Check
                    if (finalEndDate.HasValue)
                    {
                        filter &= dateBuilder.Lte(p => p.LastUpdated, finalEndDate.Value);
                    }

                }

            }
                // 3) Sorting
                var sortBuilder = Builders<PurchaseOrder>.Sort;
                SortDefinition<PurchaseOrder> sortDef = q.Sort switch
                {
                    "poId" => q.Desc ? sortBuilder.Descending(p => p.PO_ID) : sortBuilder.Ascending(p => p.PO_ID),
                    "supplierId" => q.Desc ? sortBuilder.Descending(p => p.SupplierID) : sortBuilder.Ascending(p => p.SupplierID),
                    "status" => q.Desc ? sortBuilder.Descending(p => p.Status) : sortBuilder.Ascending(p => p.Status),
                    "created" => q.Desc ? sortBuilder.Descending(p => p.CreatedDate) : sortBuilder.Ascending(p => p.CreatedDate),
                    "lastUpdate" => q.Desc ? sortBuilder.Descending(p => p.LastUpdated) : sortBuilder.Ascending(p => p.LastUpdated),
                    _ => sortBuilder.Ascending(p => p.PO_ID)
                };

                // 4) Paging via MongoPagingService
                var result = await _paging.PagedAsync(_purchaseOrder, q, filter, sortDef);

                // 5) Return partial for AJAX or full view
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("_POTable", result);
            ViewBag.Products = await _products.Find(_ => true).ToListAsync();

            return View(result);
            }

        [HttpGet]
        public async Task<IActionResult> GetAddPOForm()
        {
            // Fetch Products ONLY for the initial Dropdown List (to show Product Names)
            var products = await _products.Find(_ => true).ToListAsync();

            // Fetch data for the "Supplier Logic"
            var suppliers = await _suppliers.Find(_ => true).ToListAsync();
            var supplierProducts = await _supplierProducts.Find(_ => true).ToListAsync();

            // Join SupplierProduct -> Supplier to get the "Company Name"
            var mapData = from sp in supplierProducts
                          join s in suppliers on sp.SupplierId equals s.SupplierId
                          select new
                          {
                              pId = sp.ProductId,     
                              sId = sp.SupplierId,    
                              sName = s.Company,      
                              price = sp.SupplierPrice 
                          };

            // Pass data to View
            ViewBag.ProductSupplierMap = System.Text.Json.JsonSerializer.Serialize(mapData);
            ViewBag.Products = products; // Passed strictly for the <select> options
            var model = new PurchaseOrderViewModel();

            return PartialView("_AddPOForm", model);
        }

        [HttpPost]
        public async Task<IActionResult> AddPO(PurchaseOrderViewModel model)
        {

            ModelState.Remove("PO_ID"); 
            ModelState.Remove("Status");

            // Basic Validation
            if (model.POProductItems == null || !model.POProductItems.Any(p => p.Quantity > 0))
            {
                ModelState.AddModelError("", "Please add at least one valid product.");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Invalid field entered, failed to create purchase order.");
                return PartialView("_AddPOForm", model);
            }

            // Identify the Current
            var currentUserId = Request.Cookies["UID"];

            var lastPO = await _purchaseOrder
            .Find(_ => true) // Find ALL
            .SortByDescending(p => p.PO_ID) // Sort Z -> A (e.g., PR0002 before PR0001)
            .Limit(1)
            .FirstOrDefaultAsync();

            int nextSeq = 1; // Default if DB is empty

            if (lastPO != null && !string.IsNullOrEmpty(lastPO.PO_ID))
            {
                // Remove "PR" prefix and parse the number
                // Example: "PR0005" -> "0005" -> 5
                string numericPart = lastPO.PO_ID.Substring(2);
                if (int.TryParse(numericPart, out int lastNumber))
                {
                    nextSeq = lastNumber + 1;
                }
            }

            var newOrders = new List<PurchaseOrder>();
            var newOrderItems = new List<PurchaseOrderItem>();

            // Loop through each group and create a PO
            var supplierGroups = model.POProductItems
            .Where(p => !string.IsNullOrEmpty(p.ProductId) && p.Quantity > 0)
            .GroupBy(x => x.SupplierID)
            .ToList();

            foreach (var group in supplierGroups)
            {
                string currentPoId = $"PR{nextSeq:D4}";
                nextSeq++;

                // Create Header
                var order = new PurchaseOrder
                {
                    PO_ID = currentPoId,
                    SupplierID = group.Key,        
                    UserID = currentUserId,        
                    Status = "Pending",            
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    Remarks = model.Remarks        
                };
                newOrders.Add(order);

                // Create Items
                foreach (var item in group)
                {
                    newOrderItems.Add(new PurchaseOrderItem
                    {
                        PO_ID = currentPoId,
                        ProductId = item.ProductId,
                        QuantityOrdered = item.Quantity, 
                        UnitPrice = item.UnitPrice,
                        TotalPrice = (item.UnitPrice) * item.Quantity
                    });
                }
            }

            // Save to MongoDB
            if (newOrders.Any()) await _purchaseOrder.InsertManyAsync(newOrders);
            if (newOrderItems.Any()) await _purchaseOrderItems.InsertManyAsync(newOrderItems);

            return Json(new
            {
                success = true,
                message = $"Generated {newOrders.Count} Purchase Order(s) successfully!"
            });
        }

        [HttpGet]
        public async Task<IActionResult> EditPO(string poId)
        {
            var po = await _purchaseOrder.Find(p => p.PO_ID == poId).FirstOrDefaultAsync();
            var items = await _purchaseOrderItems.Find(p => p.PO_ID == poId).ToListAsync();
            var userRole = Request.Cookies["Role"];
            ViewBag.Role = userRole;
            ViewBag.Status = po.Status;

            // Fetch suppliers so we can look up names
            var allSuppliers = await _suppliers.Find(_ => true).ToListAsync();
            var allProducts = await _products.Find(_ => true).ToListAsync();
            ViewBag.SupplierName = allSuppliers.FirstOrDefault(s => s.SupplierId == po.SupplierID)?.Company ?? "Unknown";

            var model = new PurchaseOrderViewModel
            {
                PO_ID = po.PO_ID,
                Status = po.Status,
                Remarks = po.Remarks,
                POProductItems = items.Select(i => new POItemViewModel
                {
                    ProductName = allProducts.FirstOrDefault(p => p.ProductId == i.ProductId)?.ProductName ?? "Unknown Product",
                    Quantity = i.QuantityOrdered,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };

            return PartialView("_EditPOForm", model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePO(PurchaseOrderViewModel model)
        {
            // Validate Remarks for Cancellation/Rejection
            if ((model.Status == "Cancelled" || model.Status == "Rejected") && (string.IsNullOrWhiteSpace(model.Remarks) || model.Remarks.Trim() == "NULL"))
            {
                // Fetch the original Header
                var originalPO = await _purchaseOrder.Find(p => p.PO_ID == model.PO_ID).FirstOrDefaultAsync();
                var items = await _purchaseOrderItems.Find(p => p.PO_ID == model.PO_ID).ToListAsync();

                if (originalPO != null)
                {
                    ViewBag.Status = originalPO.Status;

                    // Fetch suppliers so we can look up names
                    var allSuppliers = await _suppliers.Find(_ => true).ToListAsync();
                    var allProducts = await _products.Find(_ => true).ToListAsync();
                    ViewBag.SupplierName = allSuppliers.FirstOrDefault(s => s.SupplierId == originalPO.SupplierID)?.Company ?? "Unknown";

                    model = new PurchaseOrderViewModel
                    {
                        PO_ID = originalPO.PO_ID,
                        Status = originalPO.Status,
                        Remarks = originalPO.Remarks,
                        POProductItems = items.Select(i => new POItemViewModel
                        {
                            ProductName = allProducts.FirstOrDefault(p => p.ProductId == i.ProductId)?.ProductName ?? "Unknown Product",
                            Quantity = i.QuantityOrdered,
                            UnitPrice = i.UnitPrice,
                            TotalPrice = i.TotalPrice
                        }).ToList()
                    };
                }

                ModelState.AddModelError("Remarks", "Remarks are required when cancelling or rejecting an order.");

                // Return the Partial View with errors
                return PartialView("_EditPOForm", model);
            }

            // If Validation Fails, reload everything from the DB
            if (!ModelState.IsValid)
            {
                // Fetch the original Header
                var originalPO = await _purchaseOrder.Find(p => p.PO_ID == model.PO_ID).FirstOrDefaultAsync();
                var items = await _purchaseOrderItems.Find(p => p.PO_ID == model.PO_ID).ToListAsync();

                if (originalPO != null)
                {
                    ViewBag.Status = originalPO.Status;

                    // Fetch suppliers so we can look up names
                    var allSuppliers = await _suppliers.Find(_ => true).ToListAsync();
                    var allProducts = await _products.Find(_ => true).ToListAsync();
                    ViewBag.SupplierName = allSuppliers.FirstOrDefault(s => s.SupplierId == originalPO.SupplierID)?.Company ?? "Unknown";

                    model = new PurchaseOrderViewModel
                    {
                        PO_ID = originalPO.PO_ID,
                        Status = originalPO.Status,
                        Remarks = originalPO.Remarks,
                        POProductItems = items.Select(i => new POItemViewModel
                        {
                            ProductName = allProducts.FirstOrDefault(p => p.ProductId == i.ProductId)?.ProductName ?? "Unknown Product",
                            Quantity = i.QuantityOrdered,
                            UnitPrice = i.UnitPrice,
                            TotalPrice = i.TotalPrice
                        }).ToList()
                    };
                }

                // Return the Partial View with errors
                return PartialView("_EditPOForm", model);
            }

           var oriPO = await _purchaseOrder.Find(p => p.PO_ID == model.PO_ID).FirstOrDefaultAsync();

            bool isNewlyCompleted = (model.Status == "Completed" && oriPO.Status != "Completed");

            // If it was ALREADY Completed, and they are changing it to something else
            if (oriPO.Status == "Completed" && model.Status != "Completed")
            {
                return Json(new { success = false, message = "Error: Cannot revert an order once completed." });
            }

            try
            {
                // STOCK UPDATE LOGIC 
                if (isNewlyCompleted)
                {
                    var items = await _purchaseOrderItems.Find(p => p.PO_ID == model.PO_ID).ToListAsync();

                    // Generate ID
                    string newTransId = await GenerateNextTransactionId();

                    // Get User ID safely
                    string currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "U001";

                    // Create Header
                    var transHeader = new StockTransaction
                    {
                        TransactionID = newTransId,
                        UserID = currentUserId,
                        Date = DateTime.Now,
                        Reason = $"Purchase Order #{model.PO_ID} - Received",
                        transactionType = "IN"
                    };

                    // Prepare Items & Product Updates
                    var transItems = new List<StockTransactionItem>();
                    var productUpdates = new List<WriteModel<Product>>();

                    foreach (var item in items)
                    {
                        // Create Transaction Item
                        transItems.Add(new StockTransactionItem
                        {
                            TransactionID = newTransId,
                            ProductID = item.ProductId,
                            QtyChange = item.QuantityOrdered,
                            Remarks = "PO Received"
                        });

                        // Prepare Product Stock Update (The Cache)
                        var filter = Builders<Product>.Filter.Eq(p => p.ProductId, item.ProductId);
                        var update = Builders<Product>.Update.Inc(p => p.Quantity, item.QuantityOrdered);
                        productUpdates.Add(new UpdateOneModel<Product>(filter, update));
                    }

                    // WRITE TO DB 
                    await _stockTransaction.InsertOneAsync(transHeader);

                    if (transItems.Any())
                    {
                        await _stockTransactionItems.InsertManyAsync(transItems);
                        // Update the product quantities
                        await _products.BulkWriteAsync(productUpdates);
                    }
                }

                // UPDATE PO STATUS 
                var updatePO = Builders<PurchaseOrder>.Update
                    .Set(p => p.Status, model.Status)
                    .Set(p => p.Remarks, model.Remarks)
                    .Set(p => p.LastUpdated, DateTime.Now);

                await _purchaseOrder.UpdateOneAsync(p => p.PO_ID == model.PO_ID, updatePO);

                return Json(new { success = true, message = "Order updated successfully!" });
            }
            catch (Exception ex)
            {
                // Log the error
                return Json(new { success = false, message = "System Error: " + ex.Message });
            }
        }
private async Task<string> GenerateNextTransactionId()
        {
            // Find the latest transaction directly
            var lastTransaction = await _stockTransaction.Find(_ => true)
                .SortByDescending(t => t.TransactionID)
                .FirstOrDefaultAsync();

            if (lastTransaction == null)
            {
                return "T0001";
            }

            // Extract number and increment
            string numericPart = lastTransaction.TransactionID.Substring(1);
            if (int.TryParse(numericPart, out int currentId))
            {
                return "T" + (currentId + 1).ToString("D4");
            }

            return "T" + Guid.NewGuid().ToString().Substring(0, 4);
        }

        [HttpGet]
        public async Task<IActionResult> PrintPO(string id)
        {
            // Fetch the PO Header
            var po = await _purchaseOrder.Find(p => p.PO_ID == id).FirstOrDefaultAsync();
            if (po == null) return NotFound();

            // Fetch the PO Items
            var items = await _purchaseOrderItems.Find(p => p.PO_ID == id).ToListAsync();

            // Fetch Supplier Info (to display Name/Address instead of just ID)
            var supplier = await _suppliers.Find(s => s.SupplierId == po.SupplierID).FirstOrDefaultAsync();

            // Fetch Product Info (to get names for the items)
            //    Optimization: Only fetch products that are in this order
            var productIds = items.Select(i => i.ProductId).ToList();
            var products = await _products.Find(p => productIds.Contains(p.ProductId)).ToListAsync();

            // Construct the ViewModel
            var model = new PurchaseOrderViewModel
            {
                PO_ID = po.PO_ID,
                Status = po.Status,
                Remarks = po.Remarks,
                POProductItems = items.Select(i => new POItemViewModel
                {
                    ProductId = i.ProductId,
                    ProductName = products.FirstOrDefault(p => p.ProductId == i.ProductId)?.ProductName ?? "Unknown Item",
                    Quantity = i.QuantityOrdered,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };

            // Pass supplier details via ViewBag for simplicity
            ViewBag.SupplierName = supplier?.Company ?? "Unknown Supplier";
            ViewBag.SupplierAddress = supplier?.Address ?? "";
            ViewBag.DateCreated = po.CreatedDate.ToString("dd MMM yyyy");

            return View("PrintPO", model);
        }
    }
    }
    
