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
                return View(model);

            var user = _users.Find(x => x.Email == model.Email && x.Password == model.Password)
                             .FirstOrDefault();

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid Email or Password.");
                return View(model);
            }

            Response.Cookies.Append("User", user.Email, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddHours(3) });
            Response.Cookies.Append("Role", user.Role, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddHours(3) });

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var user = _users.Find(u => u.Email == email).FirstOrDefault();

            if (user == null)
            {
                TempData["ResetLink"] = "Email not found!";
                return RedirectToAction("ForgotPassword");
            }

            string token = Guid.NewGuid().ToString();

            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
            _users.ReplaceOne(u => u.Id == user.Id, user);

            TempData["ResetLink"] = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);

            return RedirectToAction("ForgotPassword");
        }


        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Invalid token.");

            var user = _users.Find(u => u.ResetToken == token).FirstOrDefault();

            if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
                return BadRequest("Token has expired or is invalid.");

            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            var user = _users.Find(u => u.ResetToken == token).FirstOrDefault();

            if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
                return BadRequest("Token is invalid or expired.");

            if (newPassword != confirmPassword)
            {
                TempData["Message"] = "Passwords do not match!";
                TempData["MessageType"] = "danger";
                return RedirectToAction("ResetPassword", new { token = token });
            }

            if (newPassword.Length < 8)
            {
                TempData["Message"] = "Password must be at least 8 characters!";
                TempData["MessageType"] = "danger";
                return RedirectToAction("ResetPassword", new { token = token });
            }

            // Update password
            user.Password = newPassword;

            // Clear token
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            _users.ReplaceOne(u => u.Id == user.Id, user);

            TempData["Message"] = "Password updated successfully!";
            TempData["MessageType"] = "success";
            return RedirectToAction("Login");
        }




        public IActionResult Logout()
        {
            Response.Cookies.Delete("User");
            Response.Cookies.Delete("Role");
            return RedirectToAction("Login");
        }
    }
}
