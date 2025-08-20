using System.Security.Cryptography;
using System.Text;

namespace LaOriginalBackend.Services
{
    public static class PasswordHelper
    {
        // Hashear contraseña
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        // Verificar contraseña ingresada contra la guardada en BD
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == storedHash;
        }
    }
}
