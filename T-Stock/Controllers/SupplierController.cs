using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using T_Stock.Helpers;
using T_Stock.Models;


namespace T_Stock.Controllers
{
    public class SupplierController : Controller
    {
        private readonly IMongoCollection<Supplier> _suppliers;
        private readonly IMongoCollection<SupplierProduct> _supplierProducts;
        private readonly IMongoCollection<Product> _products;
        private readonly MongoPagingService _paging;

        public SupplierController(IMongoDatabase db, MongoPagingService paging)
        {
            _suppliers = db.GetCollection<Supplier>("Supplier");
            _supplierProducts = db.GetCollection<SupplierProduct>("SupplierProduct");
            _products = db.GetCollection<Product>("Product");
            _paging = paging;
        }

        public async Task<IActionResult> Index(PagingQuery q)
        {
            // 1) Base filter (search)
            var filter = SupplierFilterBuilder.Build(q);

            // 2) Product filter (option A)
            if (!string.IsNullOrWhiteSpace(q.Product) && q.Product != "none")
            {
                var prodFilter = Builders<SupplierProduct>.Filter.Eq(sp => sp.ProductId, q.Product);
                var supplierIds = await _supplierProducts
                    .Find(prodFilter)
                    .Project(sp => sp.SupplierId)
                    .ToListAsync();

                supplierIds = supplierIds.Distinct().ToList();

                if (!supplierIds.Any())
                {
                    var empty = new PagedResult<Supplier>
                    {
                        Items = new List<Supplier>(),
                        Page = q.Page,
                        PageSize = q.PageSize,
                        TotalItems = 0
                    };

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return PartialView("_SupplierTable", empty);

                    ViewBag.Products = await _products.Find(_ => true).ToListAsync();
                    return View(empty);
                }

                filter &= Builders<Supplier>.Filter.In(s => s.SupplierId, supplierIds);
            }

            // 3) Sorting
            var sortBuilder = Builders<Supplier>.Sort;
            SortDefinition<Supplier> sortDef = q.Sort switch
            {
                "supId" => q.Desc ? sortBuilder.Descending(s => s.SupplierId) : sortBuilder.Ascending(s => s.SupplierId),
                "company" => q.Desc ? sortBuilder.Descending(s => s.Company) : sortBuilder.Ascending(s => s.Company),
                "lastUpdate" => q.Desc ? sortBuilder.Descending(s => s.LastUpdated) : sortBuilder.Ascending(s => s.LastUpdated),
                _ => sortBuilder.Ascending(s => s.SupplierId)
            };

            // 4) Paging via MongoPagingService
            var result = await _paging.PagedAsync(_suppliers, q, filter, sortDef);

            // 5) Return partial for AJAX or full view
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_SupplierTable", result);

            ViewBag.Products = await _products.Find(_ => true).ToListAsync();
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAddSupplierForm()
        {
            var model = new SupplierViewModel();
            // Re-populate the products dropdown
            ViewBag.Products = await _products.Find(_ => true).ToListAsync();

            return PartialView("_AddSupplierForm", model);
        }

        [HttpPost]
        public async Task<IActionResult> AddSupplier(SupplierViewModel model)
        { 
            // Check for error in products
            if (model.ProductItems != null && model.ProductItems.Count > 0)
            {
                var invalidProducts = model.ProductItems
                    .Where(p => string.IsNullOrEmpty(p.ProductID) || p.SupplierPrice <= 0)
                    .Any();

                if (invalidProducts)
                {
                    ModelState.AddModelError("", "All added products must have a selected product and a valid price.");
                }
            }

            // CHECK EVERYTHING AT ONCE
            if (!ModelState.IsValid)
            {

                ViewBag.Products = await _products.Find(_ => true).ToListAsync();

                return PartialView("_AddSupplierForm", model);
            }

            // CLEANUP DATA (Only runs if everything is valid)
            if (model.ProductItems != null)
            {
                model.ProductItems = model.ProductItems
                    .Where(p => !string.IsNullOrEmpty(p.ProductID) && p.SupplierPrice > 0)
                    .Select(p =>
                    {
                        double price = p.SupplierPrice;
                        p.SupplierPrice = Math.Round(price, 2);
                        return p;
                    })
                    .ToList();
            }
            else
            {
                model.ProductItems = new List<SupplierProductItem>();
            }

            //Find last SupplierID
            var lastSupplier = _suppliers
            .Find(_ => true)
            .SortByDescending(s => s.SupplierId)
            .Limit(1)
            .FirstOrDefault();

            string newSupplierId;
            if (lastSupplier == null || string.IsNullOrEmpty(lastSupplier.SupplierId))
            {
                newSupplierId = "S0001";
            }
            else
            {
                // Extract numeric part and increment
                var numberPart = lastSupplier.SupplierId.Substring(1); // remove "S"
                var nextNumber = int.Parse(numberPart) + 1;
                newSupplierId = "S" + nextNumber.ToString("D4"); // pad with 0s, e.g., S0002
            }

            // Save to MongoDB
            var supplier = new Supplier
            {
                SupplierId = newSupplierId,
                Company = model.Company,
                ContactPerson = model.ContactPerson,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Address = model.Address,
                LastUpdated = DateTime.Now
            };

            _suppliers.InsertOne(supplier);

            if (model.ProductItems != null && model.ProductItems.Count > 0)
            {
                // Save product items
                foreach (var item in model.ProductItems)
                {
                    var supplierProduct = new SupplierProduct
                    {
                        SupplierId = supplier.SupplierId,
                        ProductId = item.ProductID,
                        SupplierPrice = item.SupplierPrice
                    };

                    _supplierProducts.InsertOne(supplierProduct);
                }
            }

            return Json(new { success = true });
        }

     
        [HttpGet]
        public async Task<IActionResult> EditSupplier(string id)
        {
            var supplier = await _suppliers.Find(s => s.SupplierId == id).FirstOrDefaultAsync();
            if (supplier == null) return NotFound();

            var products = await _supplierProducts.Find(sp => sp.SupplierId == id).ToListAsync();

            var model = new SupplierViewModel
            {
                SupplierId = supplier.SupplierId,
                Company = supplier.Company,
                ContactPerson = supplier.ContactPerson,
                PhoneNumber = supplier.PhoneNumber,
                Email = supplier.Email,
                Address = supplier.Address,
                ProductItems = products.Select(p => new SupplierProductItem
                {
                    ProductID = p.ProductId,
                    SupplierPrice = p.SupplierPrice
                }).ToList()
            };

            ViewBag.Products = await _products.Find(_ => true).ToListAsync();

            // Return the SEPARATE view
            return PartialView("_EditSupplierForm", model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSupplier(SupplierViewModel model)
        {
            
            ModelState.Remove("SupplierId");

            if (model.ProductItems != null && model.ProductItems.Count > 0)
            {
                var invalid = model.ProductItems.Any(p => string.IsNullOrEmpty(p.ProductID) || (p.SupplierPrice) <= 0);
                if (invalid) ModelState.AddModelError("", "Invalid products found.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _products.Find(_ => true).ToListAsync();
                return PartialView("_EditSupplierForm", model);
            }

            // Update Basic Info
            var updateDef = Builders<Supplier>.Update
                .Set(s => s.Company, model.Company)
                .Set(s => s.ContactPerson, model.ContactPerson)
                .Set(s => s.Email, model.Email)
                .Set(s => s.PhoneNumber, model.PhoneNumber)
                .Set(s => s.Address, model.Address)
                .Set(s => s.LastUpdated, DateTime.Now);

            await _suppliers.UpdateOneAsync(s => s.SupplierId == model.SupplierId, updateDef);

            // Update Products (Delete All & Re-insert)
            await _supplierProducts.DeleteManyAsync(sp => sp.SupplierId == model.SupplierId);

            if (model.ProductItems != null)
            {
                var newProducts = model.ProductItems
                    .Where(p => !string.IsNullOrEmpty(p.ProductID) && (p.SupplierPrice) > 0)
                    .Select(p => new SupplierProduct
                    {
                        SupplierId = model.SupplierId,
                        ProductId = p.ProductID,
                        SupplierPrice = (double)(p.SupplierPrice)
                    });

                if (newProducts.Any())
                {
                    await _supplierProducts.InsertManyAsync(newProducts);
                }
            }

            return Json(new { success = true });
        }

    }
}
