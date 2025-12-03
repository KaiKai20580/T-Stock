namespace T_Stock.Models
{
    public class LoginViewModel
    {
        public List<User> Users { get; set; } = new List<User>();

        public string Email { get; set; }
        public string Password { get; set; }

    }
}
