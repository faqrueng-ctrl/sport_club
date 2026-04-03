namespace SportClubApp.Models
{
    public sealed class UserContext
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public bool IsManagerOrAdmin => Role == "Менеджер" || Role == "Администратор";
        public bool IsAdmin => Role == "Администратор";
    }
}
