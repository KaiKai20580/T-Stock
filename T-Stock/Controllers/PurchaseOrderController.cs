using Microsoft.AspNetCore.Mvc;

namespace T_Stock.Controllers
{
    public class PurchaseOrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
