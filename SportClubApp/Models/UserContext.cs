namespace SportClubApp.Models
{
    public sealed class UserContext
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int RoleId { get; set; }
        public string Role => RoleCodes.ToName(RoleId);
        public bool IsManagerOrAdmin => RoleId == RoleCodes.Manager || RoleId == RoleCodes.Administrator;
        public bool IsAdmin => RoleId == RoleCodes.Administrator;
    }
}
