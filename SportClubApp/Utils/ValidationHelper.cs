using System.Text.RegularExpressions;

namespace SportClubApp.Utils
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        public static bool IsValidPhone(string phone) =>
            !string.IsNullOrWhiteSpace(phone) && Regex.IsMatch(phone, @"^\+?[0-9\-\(\)\s]{7,20}$");

        public static bool IsStrongPassword(string password) =>
            !string.IsNullOrWhiteSpace(password) && password.Length >= 6;
    }
}
