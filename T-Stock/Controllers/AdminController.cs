using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace T_Stock.Controllers
{
    public class AdminController : Controller
    {
        [Authorize(Roles="Admin")]
        public IActionResult Report()
        {
            return View();
        }
    }
}
