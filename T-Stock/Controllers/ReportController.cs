using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using T_Stock.Models;

namespace T_Stock.Controllers
{
    public class ReportController : Controller
    {
        private readonly IMongoCollection<Supplier> _suppliers;
        private readonly IMongoCollection<SupplierProduct> _supplierProducts;
        private readonly IMongoCollection<Product> _products;
        private readonly IMongoCollection<PurchaseOrder> _purchaseOrder;
        private readonly IMongoCollection<PurchaseOrderItem> _purchaseOrderItem;

        public ReportController(IMongoDatabase db)
        {
            _suppliers = db.GetCollection<Supplier>("Supplier");
            _supplierProducts = db.GetCollection<SupplierProduct>("SupplierProduct");
            _products = db.GetCollection<Product>("Product");
            _purchaseOrder = db.GetCollection<PurchaseOrder>("PurchaseOrder");
            _purchaseOrderItem = db.GetCollection<PurchaseOrderItem>("PurchaseOrderItem");
        }

        public IActionResult Index()
        {
            return View();
        }

        // Filter UI 
        public IActionResult FilterUI(string reportType)
        {
            if (string.Equals(reportType, "Exception", StringComparison.OrdinalIgnoreCase))
            {
                var categories = _products.AsQueryable()
                    .Select(p => p.Category)
                    .Distinct()
                    .Where(c => c != null && c != "")
                    .ToList();

                categories.Sort();
                ViewBag.Categories = categories;
            }

            return reportType switch
            {
                "Exception" => PartialView("Filters/_FilterException"),
                "Detail" => PartialView("Filters/_FilterDetail"),
                "Summary" => PartialView("Filters/_FilterSummary"),
                _ => Content("Unknown Filter")
            };
        }

        [HttpGet]
        public IActionResult Preview(String reportType, DateTime? startDate, DateTime? endDate, string category, string sortBy)
        {
            ViewBag.ReportType = reportType;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.SelectedCategory = category;
            ViewBag.SortBy = sortBy;

            string userEmail = Request.Cookies["User"];
            string userRole = Request.Cookies["Role"] ?? "User";
            string displayName = userEmail.Contains("@") ? userEmail.Split('@')[0] : userEmail;


            List<string> allowedPOIds = new List<string>();
            bool hasDateFilter = startDate.HasValue || endDate.HasValue;

            if (hasDateFilter)
            {
                var poFilter = Builders<PurchaseOrder>.Filter.Empty;

                if (startDate.HasValue)
                    poFilter &= Builders<PurchaseOrder>.Filter.Gte(x => x.LastUpdated, startDate.Value.Date);

                if (endDate.HasValue)
                    poFilter &= Builders<PurchaseOrder>.Filter.Lte(x => x.LastUpdated, endDate.Value.Date.AddDays(1).AddTicks(-1));

                allowedPOIds = _purchaseOrder
                                  .Find(poFilter)
                                  .Project(po => po.PO_ID)
                                  .ToList();

                if (!allowedPOIds.Any() && reportType == "Detail")
                {
                    return PartialView("_DetailReport", new List<Supplier>());
                }
            }
            ViewBag.AllowedPOIds = allowedPOIds;

            // Summary eport  // Exception Report 
            // calc date and price 
            var lastPurchaseDict = new Dictionary<string, DateTime>();
            var lastPriceDict = new Dictionary<string, decimal>();

            if (reportType == "Summary" || reportType == "Exception")
            {

                var poQueryFilter = allowedPOIds.Any()
                    ? Builders<PurchaseOrder>.Filter.In(p => p.PO_ID, allowedPOIds)
                    : Builders<PurchaseOrder>.Filter.Empty;

                var validPOs = _purchaseOrder.Find(poQueryFilter).ToList();


                var validPOIds = validPOs.Select(p => p.PO_ID).ToList();
                var allItems = _purchaseOrderItem.Find(i => validPOIds.Contains(i.PO_ID)).ToList();

                foreach (var po in validPOs)
                {
                    if (po.LastUpdated == DateTime.MinValue) continue;


                    var items = allItems.Where(i => i.PO_ID == po.PO_ID);

                    foreach (var item in items)
                    {
                        if (string.IsNullOrEmpty(item.ProductId)) continue;

                        if (!lastPurchaseDict.ContainsKey(item.ProductId) ||
                            po.LastUpdated > lastPurchaseDict[item.ProductId])
                        {
                            lastPurchaseDict[item.ProductId] = po.LastUpdated;
                            lastPriceDict[item.ProductId] = item.UnitPrice;
                        }
                    }
                }

                // --------- Summary Report ---------
                if (reportType == "Summary")
                {
                    var today = DateTime.Today;
                    var allProduct = _products.Find(_ => true).ToList();

                    var top5 = allProduct
                        .Where(p => p.Quantity > 0)
                        .OrderByDescending(p => p.Quantity)
                        .Take(5)
                        .Select((p, index) =>
                        {
                            decimal price = lastPriceDict.ContainsKey(p.ProductId) ? lastPriceDict[p.ProductId] : 0;
                            return new
                            {
                                No = index + 1,
                                p.ProductId,
                                p.ProductName,
                                p.Quantity,
                                p.Category,
                                Price = price,
                                TotalValue = p.Quantity * price,
                                Status = p.Quantity <= p.ReorderLevel ? "Low Stock" :
                                         p.Quantity > p.ReorderLevel * 2 ? "Over Stock" : "Normal"
                            };
                        }).ToList();



                    ViewBag.ReportMonth = startDate.HasValue ? startDate.Value.ToString("MMMM yyyy") : "All Time";
                    ViewBag.GeneratedDate = today.ToString("dd MMMM yyyy");
                    ViewBag.GeneratedBy = $"{displayName} ({userRole})"; ViewBag.ActiveProducts = allProduct.Count(p => p.Quantity > 0);
                    ViewBag.TotalStockQty = allProduct.Sum(p => p.Quantity).ToString("N0");
                    ViewBag.OutOfStockCount = allProduct.Count(p => p.Quantity <= 0);
                    ViewBag.Top10Items = top5;
                    ViewBag.Top10TotalQty = top5.Sum(x => x.Quantity);
                    ViewBag.Top10TotalValue = top5.Sum(x => x.TotalValue);

                    return PartialView("_SummaryReport");
                }

                // --------- Exception Report ---------
                if (string.Equals(reportType, "Exception", StringComparison.OrdinalIgnoreCase))
                {
                    var today = DateTime.Today;
                    int deadStockDays = 7; //control dead stock day

                    // Category Filter
                    var builder = Builders<Product>.Filter;
                    var filter = builder.Empty;

                    if (!string.IsNullOrEmpty(category) && category != "All")
                    {
                        filter &= builder.Eq(p => p.Category, category);
                    }

                    var filteredProductList = _products.Find(filter).ToList();


                    //negative stock
                    var negativeStock = filteredProductList
                        .Where(p => p.Quantity < 0)
                        .Select(p => new object[] { p.ProductId, p.ProductName, p.Quantity, p.Category ?? "-" })
                        .ToList();

                    // low stock 
                    var lowStock = filteredProductList
                        .Where(p => p.Quantity <= p.ReorderLevel && p.Quantity > 0)
                        .Select(p => new object[] { p.ProductId, p.ProductName, p.Quantity, p.ReorderLevel, p.ReorderLevel - p.Quantity })
                        .ToList();

                    // Deadstock
                    var deadStock = filteredProductList
                    .Where(p => p.Quantity > 0 && p.Quantity > p.ReorderLevel)
                    .Select(p => {
                        bool hasRecord = lastPurchaseDict.ContainsKey(p.ProductId);
                        var lastDate = hasRecord ? lastPurchaseDict[p.ProductId] : (DateTime?)null;
                        int days = lastDate.HasValue ? (today - lastDate.Value.Date).Days : 999;

                        if (days < deadStockDays) return null;

                        decimal unitPrice = lastPriceDict.ContainsKey(p.ProductId) ? lastPriceDict[p.ProductId] : 0;

                        // [0]ID, [1]ProductName, [2]Qty, [3]DateStr, [4]DaysStr, [5]Category, [6]Value
                        return new object[] {
                                p.ProductId,
                                p.ProductName ?? "Unknown",
                                p.Quantity,
                                lastDate.HasValue ? lastDate.Value.ToString("dd-MMM-yyyy") : "Never",
                                days >= 999 ? ">365" : days.ToString(),
                                p.Category ?? "-",
                                p.Quantity * unitPrice
                        };
                    }).Where(x => x != null).ToList();

                    // Sort Filter
                    Comparison<object[]> sortDelegate = (x, y) => 0;

                    if (sortBy == "Name")
                        sortDelegate = (x, y) => ((string)x[1]).CompareTo((string)y[1]); // sort by A-Z
                    else if (sortBy == "Quantity")
                        sortDelegate = (x, y) => ((int)x[2]).CompareTo((int)y[2]); // sort by quantity calc
                    else if (sortBy == "Category")
                    {
                        sortDelegate = (x, y) => ((string)x[1]).CompareTo((string)y[1]);
                    }

                    if (sortBy != null)
                    {
                        negativeStock.Sort(sortDelegate);
                        lowStock.Sort(sortDelegate);
                        deadStock.Sort(sortDelegate);
                    }

                    ViewBag.Negative = negativeStock;
                    ViewBag.Low = lowStock;
                    ViewBag.Dead = deadStock;

                    return PartialView("_ExceptionReport");
                }
            }

            // ================== C. Detail Report ==================
            else if (reportType == "Detail")
            {
                var poQuery = _purchaseOrder;
                var itemQuery = _purchaseOrderItem;
                var poList = new List<PurchaseOrder>();
                var itemList = new List<PurchaseOrderItem>();

                if (allowedPOIds.Any())
                {
                    poList = poQuery.Find(po => allowedPOIds.Contains(po.PO_ID)).ToList();
                    itemList = itemQuery.Find(item => allowedPOIds.Contains(item.PO_ID)).ToList();
                }
                else if (!hasDateFilter) // if no select date (show all info)
                {
                    poList = poQuery.Find(_ => true).ToList();
                    itemList = itemQuery.Find(_ => true).ToList();
                }

                // ready for Supplier data
                var poCounts = poList.GroupBy(x => x.SupplierID).ToDictionary(g => g.Key, g => g.Count());
                var poSupplierDict = poList.ToDictionary(x => x.PO_ID, x => x.SupplierID ?? "UNKNOWN");

                var supplierTotalAmount = itemList
                    .GroupBy(item => poSupplierDict.GetValueOrDefault(item.PO_ID, "UNKNOWN"))
                    .ToDictionary(g => g.Key, g => g.Sum(item => item.TotalPrice));

                var lastPODate = poList
                    .GroupBy(po => po.SupplierID)
                    .ToDictionary(g => g.Key ?? "UNKNOWN", g => g.Max(po => po.LastUpdated));

                var activeSupplierIds = poList
                    .Where(po => po.SupplierID != null)
                    .Select(po => po.SupplierID!)
                    .Distinct()
                    .ToList();

                var activeSuppliers = activeSupplierIds.Any()
                    ? _suppliers.Find(s => activeSupplierIds.Contains(s.SupplierId)).ToList()
                    : new List<Supplier>();

                ViewBag.POCounts = poCounts;
                ViewBag.SupplierTotalAmount = supplierTotalAmount;
                ViewBag.LastPODate = lastPODate;

                return PartialView("_DetailReport", activeSuppliers);
            }

            // Fallback
            return Content("No report Found");
        }
    }
}