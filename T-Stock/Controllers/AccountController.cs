using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using T_Stock.Models;

namespace T_Stock.Controllers
{
    public class AccountController : Controller
    {
        private readonly IMongoCollection<User> _users;

        public AccountController()
        {
            var client = new MongoClient("mongodb+srv://t-stock-123:oczgaj7c8lnRa5fr@t-stock.dr8vmsk.mongodb.net/?appName=T-Stock");
            var db = client.GetDatabase("Inventory");
            _users = db.GetCollection<User>("User");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _users.Find(x => x.Email == model.Email && x.Password == model.Password)
                             .FirstOrDefault();

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid Email or Password.");

                return View(model);
            }

            Response.Cookies.Append("User", user.Email, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddHours(3)
            });

            Response.Cookies.Append("Role", user.Role, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddHours(3)
            });

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("User");
            Response.Cookies.Delete("Role");
            return RedirectToAction("Login");
        }
    }
}
