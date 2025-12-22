using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using T_Stock.Models;
using System;
using System.Linq;

namespace T_Stock.Controllers
{
    public class InventoryController : Controller
    {
        private readonly DB _db;

        public InventoryController(DB db)
        {
            _db = db;
        }

        public IActionResult Index(int page = 1, int pageSize = 10, string sortBy = "ProductName", string sortDir = "asc", string highlightId = null)
        {
            // 1. Base Query
            var query = _db.ProductCollection.AsQueryable();

            // 2. Sorting (Initial Server-Side Sort - Optional but good for first paint)
            switch (sortBy)
            {
                case "ProductId":
                    query = (MongoDB.Driver.Linq.IMongoQueryable<Product>)(sortDir == "asc" ? query.OrderBy(p => p.ProductId) : query.OrderByDescending(p => p.ProductId));
                    break;
                case "Category":
                    query = (MongoDB.Driver.Linq.IMongoQueryable<Product>)(sortDir == "asc" ? query.OrderBy(p => p.Category) : query.OrderByDescending(p => p.Category));
                    break;
                case "Quantity":
                    query = (MongoDB.Driver.Linq.IMongoQueryable<Product>)(sortDir == "asc" ? query.OrderBy(p => p.Quantity) : query.OrderByDescending(p => p.Quantity));
                    break;
                case "Price":
                    query = (MongoDB.Driver.Linq.IMongoQueryable<Product>)(sortDir == "asc" ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price));
                    break;
                default: // ProductName
                    query = (MongoDB.Driver.Linq.IMongoQueryable<Product>)(sortDir == "asc" ? query.OrderBy(p => p.ProductName) : query.OrderByDescending(p => p.ProductName));
                    break;
            }

            // 3. Pagination Logic (For initial render)
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // 4. Fetch Data
            // A. Paged data for initial HTML render
            var pagedProducts = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // B. ALL data for Client-Side JavaScript Logic
            // This is crucial: Your JS needs the full list to handle sorting/paging without reloading.
            var allProducts = query.ToList();

            var model = new InventoryTableViewModel
            {
                Products = pagedProducts,
                AllProducts = allProducts,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                SortBy = sortBy,
                SortDir = sortDir
            };

            // Pass highlight ID for View logic
            ViewBag.HighlightProductId = highlightId;

            // 5. AJAX Handling (Optional, if you used AJAX calls)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_InventoryTable", model);
            }

            return View(model);
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Create()
        {
            var vm = new ProductListVM();
            vm.Items.Add(new Product());

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Create", vm);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductListVM model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("Create", model);

                return View(model);
            }

            // 1. Get the last ProductID
            var lastProduct = _db.ProductCollection.Find(_ => true)
                                                   .SortByDescending(p => p.ProductId)
                                                   .Limit(1)
                                                   .FirstOrDefault();

            int nextNumber = 1;
            if (lastProduct != null)
            {
                var numericPart = lastProduct.ProductId.Substring(1); // remove "P"
                if (int.TryParse(numericPart, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            // 2. Assign new ProductId
            foreach (var item in model.Items)
            {
                item.ProductId = $"P{nextNumber:D4}";
                nextNumber++;
            }

            // 3. Insert
            _db.ProductCollection.InsertMany(model.Items);

            // 4. Return
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true });

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return BadRequest();

            var collection = _db.ProductCollection;
            var filter = Builders<Product>.Filter.Eq(p => p.ProductId, productId);
            var result = collection.DeleteOne(filter);

            if (result.DeletedCount > 0)
            {
                TempData["Message"] = "Product deleted successfully.";
            }
            else
            {
                TempData["Message"] = "Product not found.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var product = _db.ProductCollection.Find(p => p.ProductId == id).FirstOrDefault();
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                var existingProduct = _db.ProductCollection.Find(p => p.ProductId == product.ProductId).FirstOrDefault();

                if (existingProduct == null)
                {
                    ModelState.AddModelError("", "Product not found.");
                    return View(product);
                }

                product.Id = existingProduct.Id; // Preserve MongoDB _id
                var filter = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
                var updateResult = _db.ProductCollection.ReplaceOne(filter, product);

                if (updateResult.MatchedCount > 0)
                {
                    // --- Added this line to trigger the CSS popup ---
                    TempData["SuccessMessage"] = "Product updated successfully!";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Update failed.");
                }
            }
            return View(product);
        }
    }
}