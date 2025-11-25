using Microsoft.AspNetCore.Mvc;

namespace T_Stock.Controllers
{
    public class Supplier : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
