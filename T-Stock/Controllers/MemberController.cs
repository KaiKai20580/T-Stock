using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using T_Stock.Models;

namespace T_Stock.Controllers
{
    public class MemberController : Controller
    {
        private readonly IMongoCollection<User> _users;

        public MemberController()
        {
            var client = new MongoClient("mongodb+srv://t-stock-123:oczgaj7c8lnRa5fr@t-stock.dr8vmsk.mongodb.net/?appName=T-Stock");
            var db = client.GetDatabase("Inventory");
            _users = db.GetCollection<User>("User");
        }

        // 1. DISPLAY ALL MEMBERS
        public IActionResult Index(string searchTerm, string roleFilter)
        {
            var filter = Builders<User>.Filter.Empty;

            // Role filter
            if (!string.IsNullOrEmpty(roleFilter))
            {
                filter &= Builders<User>.Filter.Regex(
                    u => u.Role,
                    new MongoDB.Bson.BsonRegularExpression($"^{roleFilter}$", "i")
                );
            }

            // Search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var regexPattern = $"^{Regex.Escape(searchTerm)}[^@]*@";
                filter &= Builders<User>.Filter.Regex(
                    u => u.Email,
                    new MongoDB.Bson.BsonRegularExpression(regexPattern, "i")
                );
            }

            var members = _users.Find(filter).ToList() ?? new List<User>(); // ✅ never null

            ViewBag.RoleFilter = roleFilter;
            ViewBag.SearchTerm = searchTerm;

            return View(members);
        }




        // 2. DELETE MEMBER
        [HttpPost]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            _users.DeleteOne(u => u.Id == id);

            TempData["SuccessMessage"] = "Member deleted successfully!";
            return RedirectToAction("Index");
        }

        // 3. EDIT MEMBER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User user)
        {
            var existingUser = _users.Find(u => u.Id == user.Id).FirstOrDefault();
            if (existingUser == null) return NotFound();

            // Check for duplicate email
            if (!string.IsNullOrEmpty(user.Email))
            {
                var duplicate = _users.Find(u => u.Email == user.Email && u.Id != user.Id).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["ErrorMessage"] = "Cannot save changes: This email already exists!";
                    return RedirectToAction(nameof(Index));
                }
            }

            var originalEmail = existingUser.Email;

            // Update editable fields
            if (!string.IsNullOrEmpty(user.Email))
                existingUser.Email = user.Email;

            if (!string.IsNullOrEmpty(user.Role))
                existingUser.Role = user.Role;

            _users.ReplaceOne(u => u.Id == user.Id, existingUser);

            // Update cookies if current user updated own data
            var currentEmail = Request.Cookies["User"];
            if (currentEmail == originalEmail)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddHours(3),
                };

                Response.Cookies.Append("User", existingUser.Email, cookieOptions);
                Response.Cookies.Append("Role", existingUser.Role ?? "No Role", cookieOptions);
            }

            TempData["SuccessMessage"] = "Member updated successfully!";
            return RedirectToAction(nameof(Index));
        }


    }
}