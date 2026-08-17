namespace FixMyTownApi.Services
{
    /// <summary>
    /// Small wrapper around BCrypt.Net-Next, the same hashing algorithm
    /// (bcrypt) used everywhere else, so existing password hashes in
    /// the database keep working no matter which language wrote them.
    /// </summary>
    public class PasswordService
    {
        public string Hash(string plainTextPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
        }

        public bool Verify(string plainTextPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, storedHash);
        }
    }
}
