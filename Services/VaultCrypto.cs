using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services;

public static class VaultCrypto
{
    private const int KeySize = 32;       // 256 бит
    private const int NonceSize = 12;     // 96 бит для GCM
    private const int TagSize = 16;       // 128 бит

    // Шифрование данных
    public static byte[] Encrypt(string plaintext, byte[] key)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        byte[] result = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);

        return result;
    }

    // Расшифровка данных
    public static string Decrypt(byte[] encryptedData, byte[] key)
    {
        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];
        byte[] ciphertext = new byte[encryptedData.Length - NonceSize - TagSize];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, NonceSize, ciphertext, 0, ciphertext.Length);
        Buffer.BlockCopy(encryptedData, NonceSize + ciphertext.Length, tag, 0, TagSize);

        byte[] plainBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}