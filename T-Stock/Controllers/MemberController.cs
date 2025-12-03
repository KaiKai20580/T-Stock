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

            
            return RedirectToAction("Index");
        }


        // 3. EDIT MEMBER (POST) - Saves the Data from the Modal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User user)
        {
            // 1. Check if ID is valid
            if (user.Id == null)
            {
                return NotFound();
            }

            // 2. Fetch the EXISTING user from the database
            // This holds the old Email, Role, and Password
            var existingUser = _users.Find(u => u.Id == user.Id).FirstOrDefault();

            if (existingUser == null)
            {
                return NotFound();
            }

            // 3. Update Email (Only if the user typed something)
            if (!string.IsNullOrEmpty(user.Email))
            {
                existingUser.Email = user.Email;
            }

            // 4. Update Role 
            // Only update existingUser.Role if the new user.Role is NOT null and NOT empty.
            // If user.Role is null, we skip this line, keeping the old Role.
            if (!string.IsNullOrEmpty(user.Role))
            {
                existingUser.Role = user.Role;
            }

            // 5. Save back to database
            // This saves the new data mixed with the preserved old data (like Password)
            _users.ReplaceOne(u => u.Id == user.Id, existingUser);

            return RedirectToAction(nameof(Index));
        }
    }
}