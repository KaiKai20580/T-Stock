using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using T_Stock.Models;

namespace T_Stock.Controllers
{
    public class InventoryController : Controller
    {
        private readonly DB _db;

        public InventoryController(DB db)
        {
            _db = db;
        }

        //test
        // Fetch all inventory items
        public IActionResult Index()
        {
            var items = _db.InventoryCollection.Find(_ => true).ToList();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Create(Inventory model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _db.InventoryCollection.InsertOne(model);
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                // Optionally log the exception
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
    }
}
