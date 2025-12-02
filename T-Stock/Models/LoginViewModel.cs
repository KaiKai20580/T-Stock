namespace T_Stock.Models
{
    public class LoginViewModel
    {
        public List<User> Users { get; set; } = new List<User>();

        public string Username { get; set; }
        public string Password { get; set; }

    }
}
