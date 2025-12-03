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
            return View(new User());
        }

        [HttpPost]
        public IActionResult Login(User model)
        {
            var user = _users.Find(x => x.Email == model.Email && x.Password == model.Password).FirstOrDefault();

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View(model);
            }

            HttpContext.Session.SetString("User", user.Email);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
