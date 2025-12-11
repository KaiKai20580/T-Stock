using Microsoft.AspNetCore.Mvc; /*Test 1 */
using MongoDB.Driver;
using System.Linq;
using T_Stock.Models;

namespace T_Stock.Controllers
{


    public class ReportController : Controller
    {
        private readonly DB _db;

        public ReportController(DB db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }
        //Test 2025-12-10 13:27
        public IActionResult FilterUI(string reportType)
        {
            return reportType switch
            {
                "Exception" => PartialView("Filters/_FilterException"),
                "Detail" => PartialView("Filters/_FilterDetail"),
                "Supplier" => PartialView("Filters/_FilterSupplier"),
                "Summary" => PartialView("Filters/_FilterSummary")
            };
        }

        [HttpGet]
        public IActionResult Preview(String reportType, DateTime? startDate, DateTime? endDate)
        {


            ViewBag.ReportType = reportType;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            List<string> allowedPOIds = new List<string>();
            if (startDate.HasValue || endDate.HasValue)
            {
                var poFilter = Builders<PurchaseOrder>.Filter.Empty;

                if (startDate.HasValue)
                    poFilter &= Builders<PurchaseOrder>.Filter.Gte(x => x.LastUpdated, startDate.Value.Date);

                if (endDate.HasValue)
                    poFilter &= Builders<PurchaseOrder>.Filter.Lte(x => x.LastUpdated, endDate.Value.Date.AddDays(1).AddTicks(-1));

                allowedPOIds = _db.GetCollection<PurchaseOrder>("PurchaseOrder")
                                  .Find(poFilter)
                                  .Project(po => po.PO_ID)
                                  .ToList();

                if (!allowedPOIds.Any())
                {
                    if (reportType == "Detail" || reportType == "Supplier")
                        return PartialView("_DetailReport", new List<Supplier>());
                    if (reportType == "Summary")
                        return PartialView("_SummaryReport");
                    if (reportType == "Exception")
                        return PartialView("_ExceptionReport");
                    return Content("-");
                }
            }

            ViewBag.AllowedPOIds = allowedPOIds;

            if (reportType == "Summary")
            {
                var today = DateTime.Today;
                var reportMonth = startDate.HasValue ? startDate.Value.ToString("MMMM yyyy") : "All Time";
                var allProduct = _db.GetCollection<Product>("Product")
                    .Find(_=> true)
                    .ToList();

                var activeProducts = allProduct.Count(p => p.Quantity > 0);
                var totalStockQty = allProduct.Sum(p => p.Quantity);

              //  var lowStockCount = allProduct.Count(p => p.Quantity > 0 && p.Quantity <= p.ReorderLevel); 等后续看要不要
                var outOfStockCount = allProduct.Count(p => p.Quantity <= 0);
                var overStockCount = allProduct.Count(p => p.Quantity > p.ReorderLevel * 2);
                    
                var top5 = allProduct
                    .Where(p => p.Quantity > 0)
                    .OrderByDescending(p => p.Quantity)
                    .Take(10)
                    .Select((p,index) => new
                    {
                        No = index+1, 
                        p.ProductID,
                        p.ProductName,
                        p.Quantity,
                        p.Category,
                        Status = p.Quantity <= p.ReorderLevel ? "Low Stock" :
                    p.Quantity > p.ReorderLevel * 2 ? "Over Stock" : "Normal"
                    })
        .ToList();

                ViewBag.ReportMonth = reportMonth;
                ViewBag.GeneratedDate = today.ToString("dd MMMM yyyy");
                ViewBag.GeneratedBy = "Koay Kah Wooi (Admin)"; //hardcode

                ViewBag.ActiveProducts = activeProducts;
                ViewBag.TotalStockQty = totalStockQty.ToString("N0");
              // ViewBag.LowStockCount = lowStockCount; 等后续看要不要
                ViewBag.OutOfStockCount = outOfStockCount;
                ViewBag.Top10Items = top5;
                ViewBag.Top10TotalQty = top5.Sum(x => x.Quantity);
                return PartialView("_SummaryReport");
            }

            else if (reportType == "Exception")
            {
                var today = DateTime.Today;
                int deadStockDays = 30;
                var cutoffDate = today.AddDays(-deadStockDays);

                var negativeStock = _db.GetCollection<Product>("Product")
                    .Find(p => p.Quantity < 0)
                    .ToList()
                    .Select(p => new object[]
                    {
                        p.ProductID,
                        p.ProductName,
                        p.Quantity,
                        p.Category
                    }).ToList();


                var lowStock = _db.GetCollection<Product>("Product")
                    .Find(p => p.Quantity <= p.ReorderLevel && p.Quantity > 0)
                    .ToList()
                    .Select(p => new object[]
                    {
                        p.ProductID,
                        p.ProductName,
                        p.Quantity,
                        p.ReorderLevel,
                        p.ReorderLevel - p.Quantity
                    }).ToList();

                var lastPurchaseDict = new Dictionary<string, DateTime>();

                var allPOs = _db.GetCollection<PurchaseOrder>("PurchaseOrder")
                    .Find(_ => true)
                    .ToList();

                foreach (var po in allPOs)
                {
                    if (!po.LastUpdated.HasValue) continue;

                    var items = _db.GetCollection<PurchaseOrderItem>("PurchaseOrderItem")
                        .Find(i => i.PO_ID == po.PO_ID)
                        .ToList();

                    foreach (var item in items)
                    {
                        if (string.IsNullOrEmpty(item.ProductID)) continue;

                        if (!lastPurchaseDict.ContainsKey(item.ProductID) ||
                            po.LastUpdated.Value > lastPurchaseDict[item.ProductID])
                        {
                            lastPurchaseDict[item.ProductID] = po.LastUpdated.Value;
                        }
                    }
                }

                var deadStock = _db.GetCollection<Product>("Product")
                    .Find(p => p.Quantity > 0)
                    .ToList()
                    .Select(p =>
                    {
                        bool hasRecord = lastPurchaseDict.ContainsKey(p.ProductID);
                        var lastDate = hasRecord ? lastPurchaseDict[p.ProductID] : (DateTime?)null;
                        int days = lastDate.HasValue ? (today - lastDate.Value.Date).Days : 999;

                        return new object[]
                        {
                p.ProductID,
                p.ProductName ?? "Unknown",
                p.Quantity,
                lastDate.HasValue ? lastDate.Value.ToString("dd-MMM-yyyy") : "Never",
                days >= 999 ? ">365" : days.ToString(),
                p.Category ?? "-"
                        };
                    })
                    .Where(x => x != null)
                    .ToList();


                ViewBag.Negative = negativeStock;
                ViewBag.Low = lowStock;
                ViewBag.Dead = deadStock;

                return PartialView("_ExceptionReport");
            }

            else if (reportType == "Detail")
            {
                var Detailr = _db.GetCollection<Supplier>("Supplier")
                                 .Find(_ => true)
                                 .ToList();

                var poQuery = _db.GetCollection<PurchaseOrder>("PurchaseOrder").Find(_ => true);
                var itemQuery = _db.GetCollection<PurchaseOrderItem>("PurchaseOrderItem").Find(_ => true);

                if (allowedPOIds.Any())
                {
                    poQuery = _db.GetCollection<PurchaseOrder>("PurchaseOrder")
                                 .Find(po => allowedPOIds.Contains(po.PO_ID));

                    itemQuery = _db.GetCollection<PurchaseOrderItem>("PurchaseOrderItem")
                                    .Find(item => allowedPOIds.Contains(item.PO_ID));
                }

                var poList = poQuery.ToList();
                var itemList = itemQuery.ToList();



                var poCounts = poList
                               .GroupBy(x => x.SupplierID)
                               .ToDictionary(g => g.Key, g => g.Count());


                var poSupplierDict = poList
                                    .ToDictionary(x => x.PO_ID, x => x.SupplierID ?? "UNKNOWN");

                var supplierTotalAmount = itemList
                                .GroupBy(item => poSupplierDict.GetValueOrDefault(item.PO_ID, "UNKNOWN"))
                                .ToDictionary(g => g.Key, g => g.Sum(item => item.TotalPrice));

                var lastPODate = poList
                                 .GroupBy(po => po.SupplierID)
                                 .ToDictionary(
                                     g => g.Key ?? "UNKNOWN",
                                     g => g.Max(po => po.LastUpdated)
                                 );

                var activeSupplierIds = poList
        .Where(po => po.SupplierID != null)
        .Select(po => po.SupplierID!)
        .Distinct()
        .ToList();

                var activeSuppliers = activeSupplierIds.Any()
                    ? _db.GetCollection<Supplier>("Supplier")
                         .Find(s => activeSupplierIds.Contains(s.SupplierID))
                         .ToList()
                    : new List<Supplier>();



                ViewBag.POCounts = poCounts;
                ViewBag.SupplierTotalAmount = supplierTotalAmount;
                ViewBag.LastPODate = lastPODate;

                return PartialView("_DetailReport", activeSuppliers);
            }

            else if (reportType == "Supplier")
            {
                var suppliers = _db.GetCollection<Supplier>("Supplier")
                                   .Find(_ => true)
                                   .ToList();
                return PartialView("_SupplierReport", suppliers);
            }
            return Content("No report Found");
        }

    }

}
