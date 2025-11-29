using Microsoft.AspNetCore.Mvc;

namespace T_Stock.Controllers
{
    public class NotificationController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_NotiPartial");
            }
            return View("Notification");
        }
    }
}
