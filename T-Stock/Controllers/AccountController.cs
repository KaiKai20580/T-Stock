using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using T_Stock.Models;
using System;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace T_Stock.Controllers
{
    public class AccountController : Controller
    {
        private readonly IMongoCollection<User> _users;

        public AccountController()
        {
            var client = new MongoClient(
                "mongodb+srv://t-stock-123:oczgaj7c8lnRa5fr@t-stock.dr8vmsk.mongodb.net/?appName=T-Stock"
            );
            var db = client.GetDatabase("Inventory");
            _users = db.GetCollection<User>("User");
        }

        // ===================== LOGIN =====================
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

            var user = _users.Find(u => u.Email == model.Email).FirstOrDefault();

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid Email or Password.");
                return View(model);
            }

            if (string.IsNullOrEmpty(user.Password))
            {
                return RedirectToAction("SetPassword", new { id = user.Id });
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid password!");
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

            Response.Cookies.Append("UID", user.UserId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddHours(3)
            });

            if (user.Role == "Admin" || user.Role == "Staff")
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Index", "PurchaseOrder");
            }
        }

        // ===================== FORGOT PASSWORD =====================
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
                TempData["Message"] = "Email not found!";
                TempData["MessageType"] = "danger";
                return RedirectToAction("ForgotPassword");
            }

            string token = Guid.NewGuid().ToString();

            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
            _users.ReplaceOne(u => u.Id == user.Id, user);

            TempData["ResetLink"] = Url.Action(
                "ResetPassword",
                "Account",
                new { token },
                Request.Scheme
            );

            return RedirectToAction("ForgotPassword");
        }

        // ===================== RESET PASSWORD =====================
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var user = _users.Find(u => u.ResetToken == token).FirstOrDefault();

            if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
                return BadRequest("Invalid or expired token.");

            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            var user = _users.Find(u => u.ResetToken == token).FirstOrDefault();

            if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
                return BadRequest("Invalid or expired token.");

            if (!IsValidPassword(newPassword))
            {
                TempData["Message"] = "Password must be at least 8 characters and contain letters and numbers.";
                TempData["MessageType"] = "danger";
                return RedirectToAction("ResetPassword", new { token });
            }

            if (newPassword != confirmPassword)
            {
                TempData["Message"] = "Passwords do not match!";
                TempData["MessageType"] = "danger";
                return RedirectToAction("ResetPassword", new { token });
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            _users.ReplaceOne(u => u.Id == user.Id, user);

            TempData["Message"] = "Password updated successfully!";
            TempData["MessageType"] = "success";

            return RedirectToAction("Login");
        }

        // ===================== SET PASSWORD =====================
        [HttpGet]
        public IActionResult SetPassword(string id)
        {
            var user = _users.Find(u => u.Id == id).FirstOrDefault();
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public IActionResult SetPassword(string id, string newPassword, string confirmPassword)
        {
            var user = _users.Find(u => u.Id == id).FirstOrDefault();
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(user.Password))
                return RedirectToAction("Login");

            if (!IsValidPassword(newPassword))
            {
                TempData["Message"] = "Password must be at least 8 characters and contain at least one number!";
                TempData["MessageType"] = "danger";
                return RedirectToAction("SetPassword", new { id = id });
            }


            if (newPassword != confirmPassword)
            {
                TempData["Message"] = "Passwords do not match!";
                TempData["MessageType"] = "danger";
                return RedirectToAction("SetPassword", new { id });
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _users.ReplaceOne(u => u.Id == user.Id, user);

            TempData["Message"] = "Password set successfully!";
            TempData["MessageType"] = "success";

            return RedirectToAction("Login");
        }

        // ===================== PASSWORD RULE =====================
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            // Check if it contains at least one digit
            foreach (char c in password)
            {
                if (char.IsDigit(c))
                    return true;
            }

            return false;
        }

        // ===================== CHECK EMAIL (AJAX) =====================
        [HttpPost]
        public IActionResult CheckEmail([FromBody] EmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Json(new { status = "error", message = "Email is required." });

            // ✔ Only check for '@'
            if (!request.Email.Contains("@"))
                return Json(new { status = "error", message = "Email must contain '@'." });

            var user = _users.Find(u => u.Email == request.Email).FirstOrDefault();
            if (user == null)
                return Json(new { status = "error", message = "Email not found." });

            if (string.IsNullOrEmpty(user.Password))
            {
                return Json(new
                {
                    status = "setpassword",
                    redirectUrl = Url.Action("SetPassword", new { id = user.Id })
                });
            }

            return Json(new { status = "ok" });
        }


        public class EmailRequest
        {
            public string Email { get; set; }
        }


        // ===================== LOGOUT =====================
        public IActionResult Logout()
        {
            Response.Cookies.Delete("User");
            Response.Cookies.Delete("Role");
            Response.Cookies.Delete("UID");
            return RedirectToAction("Login");
        }
    }
}
