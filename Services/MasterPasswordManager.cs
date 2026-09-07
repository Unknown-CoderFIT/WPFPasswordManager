using System.Security.Cryptography;

namespace PasswordManager.Services;

public static class MasterPasswordManager
{
    private const int SaltSize = 32;
    private const int DefaultIterations = 600_000;

    // Получение ключа из мастер-пароля
    public static byte[] DeriveKey(string masterPassword, byte[] salt, int iterations = DefaultIterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            masterPassword,
            salt,
            iterations,
            HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }

    // Чтение параметров из master.dat
    public static (byte[] salt, int iterations) ReadMasterData(string masterPath)
    {
        byte[] fileBytes = System.IO.File.ReadAllBytes(masterPath);
        
        int iterations = BitConverter.ToInt32(fileBytes, 0);
        byte[] salt = new byte[SaltSize];
        Buffer.BlockCopy(fileBytes, 4, salt, 0, SaltSize);

        return (salt, iterations);
    }
}