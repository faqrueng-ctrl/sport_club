namespace SportClubApp.Models
{
    public static class RoleCodes
    {
        public const int User = 1;
        public const int Manager = 2;
        public const int Administrator = 3;

        public static string ToName(int roleId)
        {
            switch (roleId)
            {
                case User: return "Пользователь";
                case Manager: return "Менеджер";
                case Administrator: return "Администратор";
                default: return "Гость";
            }
        }
    }
}
