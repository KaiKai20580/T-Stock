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

        public IActionResult Index()
        {
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("Index", _db.InventoryCollection.Find(_ => true).ToList());
            }

            var items = _db.InventoryCollection.Find(_ => true).ToList();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new InventoryListVM();
            vm.Items.Add(new Inventory());

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("Create", vm);
            }

            return View(vm);
        }

        [HttpPost]
        public IActionResult Create(InventoryListVM model)
        {
            if (!ModelState.IsValid)
            {
                if (model.Items == null || model.Items.Count == 0)
                    model.Items?.Add(new Inventory());

                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return PartialView("Create", model);

                return View(model);
            }

            try
            {
                _db.InventoryCollection.InsertMany(model.Items);

                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return Json(new { success = true });

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;

                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return PartialView("Create", model);

                return View(model);
            }
        }


    }
}
