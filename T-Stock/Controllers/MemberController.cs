using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
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
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role == null || role.ToLower() != "admin")
            {
                return RedirectToAction("Login", "Account"); 
            }

            var members = _users.Find(_ => true).ToList();
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


        // 3. EDIT MEMBER (POST) - Saves the Data from the Modal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User user)
        {
            var existingUser = _users.Find(u => u.Id == user.Id).FirstOrDefault();
            if (existingUser == null) return NotFound();

            // Save original email for session check
            var originalEmail = existingUser.Email;

            // Update user fields
            if (!string.IsNullOrEmpty(user.Email)) existingUser.Email = user.Email;
            if (!string.IsNullOrEmpty(user.Role)) existingUser.Role = user.Role;

            _users.ReplaceOne(u => u.Id == user.Id, existingUser);

            // Update session if editing current logged-in user
            var currentEmail = HttpContext.Session.GetString("User");
            if (currentEmail == originalEmail)
            {
                HttpContext.Session.SetString("User", existingUser.Email);
                HttpContext.Session.SetString("Role", existingUser.Role ?? "No Role");
            }

            TempData["SuccessMessage"] = "Member updated successfully!";

            return RedirectToAction(nameof(Index));
        }


    }
}