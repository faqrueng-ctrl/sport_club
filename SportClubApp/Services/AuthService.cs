using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using SportClubApp.Data;
using SportClubApp.Models;
using SportClubApp.Utils;

namespace SportClubApp.Services
{
    public sealed class AuthService
    {
        public void Register(string fullName, string email, string phone, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName)) throw new InvalidOperationException("Имя обязательно.");
            if (!ValidationHelper.IsValidEmail(email)) throw new InvalidOperationException("Некорректный email.");
            if (!ValidationHelper.IsValidPhone(phone)) throw new InvalidOperationException("Некорректный телефон.");
            if (!ValidationHelper.IsStrongPassword(password)) throw new InvalidOperationException("Пароль должен быть не короче 6 символов.");

            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO dbo.Users(FullName,Email,Phone,PasswordHash,Role)
VALUES(@name,@email,@phone,@hash,N'Пользователь');";
                cmd.Parameters.AddWithValue("@name", fullName.Trim());
                cmd.Parameters.AddWithValue("@email", email.Trim());
                cmd.Parameters.AddWithValue("@phone", phone.Trim());
                cmd.Parameters.AddWithValue("@hash", Hash(password));
                cmd.ExecuteNonQuery();
            }
        }

        public UserContext Login(string login, string password)
        {
            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT TOP(1) Id,FullName,Email,Phone,Role,PasswordHash
FROM dbo.Users WHERE Email=@login OR Phone=@login";
                cmd.Parameters.AddWithValue("@login", login.Trim());

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("Пользователь не найден.");
                    var hash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                    if (!string.Equals(hash, Hash(password), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Неверный пароль.");
                    }

                    return new UserContext
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("Id")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Phone = reader.GetString(reader.GetOrdinal("Phone")),
                        Role = reader.GetString(reader.GetOrdinal("Role"))
                    };
                }
            }
        }

        public void UpdateProfile(UserContext user, string fullName, string email, string phone)
        {
            if (!ValidationHelper.IsValidEmail(email)) throw new InvalidOperationException("Некорректный email.");
            if (!ValidationHelper.IsValidPhone(phone)) throw new InvalidOperationException("Некорректный телефон.");

            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE dbo.Users SET FullName=@name, Email=@email, Phone=@phone WHERE Id=@id";
                cmd.Parameters.AddWithValue("@name", fullName.Trim());
                cmd.Parameters.AddWithValue("@email", email.Trim());
                cmd.Parameters.AddWithValue("@phone", phone.Trim());
                cmd.Parameters.AddWithValue("@id", user.UserId);
                cmd.ExecuteNonQuery();
            }
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }
    }
}
