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
            var client = new MongoClient(
                "mongodb+srv://t-stock-123:oczgaj7c8lnRa5fr@t-stock.dr8vmsk.mongodb.net/?appName=T-Stock"
            );
            var db = client.GetDatabase("Inventory");
            _users = db.GetCollection<User>("User");
        }

        // =====================
        // LIST MEMBERS
        // =====================
        public IActionResult Index(string searchTerm, string roleFilter)
        {
            var filter = Builders<User>.Filter.Empty;

            if (!string.IsNullOrEmpty(roleFilter))
            {
                filter &= Builders<User>.Filter.Regex(
                    u => u.Role,
                    new MongoDB.Bson.BsonRegularExpression($"^{roleFilter}$", "i")
                );
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                filter &= Builders<User>.Filter.Regex(
                    u => u.Email,
                    new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
                );
            }

            var members = _users.Find(filter).ToList();

            ViewBag.RoleFilter = roleFilter;
            ViewBag.SearchTerm = searchTerm;

            return View(members);
        }

        // =====================
        // ADD MEMBER (ADMIN)
        // =====================
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(string Email, string Role)
        {
            // Basic required check
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Role))
            {
                TempData["ErrorMessage"] = "Email and Role are required";
                return RedirectToAction("Index");
            }

            Email = Email.Trim().ToLower();

            var emailParts = Email.Split('@');
            if (emailParts.Length != 2)
            {
                TempData["ErrorMessage"] = "Invalid email format";
                return RedirectToAction("Index");
            }

            if (!emailParts[0].Any(char.IsLetterOrDigit))
            {
                TempData["ErrorMessage"] =
                    "Email must contain at least one letter or number before @";
                return RedirectToAction("Index");
            }

            var existingUser = _users.Find(u => u.Email == Email).FirstOrDefault();
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "This email is already registered!";
                return RedirectToAction("Index");
            }

            var newUser = new User
            {
                UserId = GenerateNextUserId(),
                Email = Email,
                Role = Role,
                Password = null
            };

            _users.InsertOne(newUser);

            TempData["SuccessMessage"] =
                "User created. User must set password on first login.";

            return RedirectToAction("Index");
        }



        // =====================
        // DELETE MEMBER
        // =====================
        [HttpPost]
        public IActionResult Delete(string id)
        {
            _users.DeleteOne(u => u.Id == id);
            TempData["SuccessMessage"] = "Member deleted successfully!";
            return RedirectToAction("Index");
        }

        // EDIT MEMBER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User user)
        {
            var existingUser = _users.Find(u => u.Id == user.Id).FirstOrDefault();
            if (existingUser == null) return NotFound();

            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Role))
            {
                TempData["ErrorMessage"] = "Email and Role cannot be empty.";
                return RedirectToAction("Index");
            }

            string normalizedEmail = user.Email.Trim().ToLower();

            var emailParts = normalizedEmail.Split('@');

            if (emailParts.Length != 2 || !emailParts[0].Any(char.IsLetterOrDigit))
            {
                TempData["ErrorMessage"] = "Invalid email format. The email must contain at least one letter or number before the '@'.";
                return RedirectToAction("Index");
            }

            var emailExists = _users.Find(u => u.Email == normalizedEmail && u.Id != user.Id).Any();

            if (emailExists)
            {
                TempData["ErrorMessage"] = $"Email '{normalizedEmail}' is already in use by another member.";
                return RedirectToAction("Index");
            }

            // --- 5. Database Operation ---
            try
            {
                existingUser.Email = normalizedEmail; 
                existingUser.Role = user.Role;

                _users.ReplaceOne(u => u.Id == user.Id, existingUser);

                TempData["SuccessMessage"] = "Member updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected database error occurred while saving the changes.";
                return RedirectToAction("Index");
            }
        }

        // =====================
        // HELPER: AUTO USERID
        // =====================
        private string GenerateNextUserId()
        {
            var lastUser = _users.Find(u => u.UserId != null)
                                 .SortByDescending(u => u.UserId)
                                 .Limit(1)
                                 .FirstOrDefault();

            if (lastUser == null)
                return "U001";

            int lastNumber = int.Parse(lastUser.UserId.Substring(1));
            return $"U{(lastNumber + 1):D3}";
        }
    }
}
