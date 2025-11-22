using Microsoft.AspNetCore.Mvc;

namespace T_Stock.Controllers
{
    public class Notification : Controller
    {
        public IActionResult Index()
        {
            return View("Notification");
        }
    }
}
