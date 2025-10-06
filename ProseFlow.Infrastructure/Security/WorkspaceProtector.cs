using System.Security.Cryptography;
using ProseFlow.Application.Interfaces;

namespace ProseFlow.Infrastructure.Security;

/// <summary>
/// Provides a secure way to encrypt and decrypt sensitive data for a shared workspace
/// using a password-derived symmetric key.
/// </summary>
public class WorkspaceProtector : IWorkspaceProtector
{
    private byte[]? _key;
    private static readonly byte[] Salt = "ProseFlowSharedSalt-v1"u8.ToArray();
    
    /// <summary>
    /// Initializes the protector with a password for the current scope.
    /// This method must be called before calling Protect or Unprotect.
    /// </summary>
    /// <param name="password">The shared workspace password.</param>
    public void Initialize(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentNullException(nameof(password));
        }

        using var deriveBytes = new Rfc2898DeriveBytes(password, Salt, 10000, HashAlgorithmName.SHA256);
        _key = deriveBytes.GetBytes(32); // 256-bit key for AES
    }

    /// <summary>
    /// Encrypts a plaintext string.
    /// </summary>
    /// <param name="plainText">The data to protect.</param>
    /// <returns>A protected, Base64-encoded string containing the IV and ciphertext.</returns>
    public string Protect(string plainText)
    {
        if (_key is null) throw new InvalidOperationException("Key has not been initialized. Call Initialize first.");
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var ms = new MemoryStream();
        
        // Prepend IV to the stream
        ms.Write(iv, 0, iv.Length);
        
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            using var sw = new StreamWriter(cs);
            sw.Write(plainText);
        }
        
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypts a protected string back to its original form.
    /// </summary>
    /// <param name="protectedData">The protected data, including the IV.</param>
    /// <returns>The original plaintext string.</returns>
    public string Unprotect(string protectedData)
    {
        if (_key is null) throw new InvalidOperationException("Key has not been initialized. Call Initialize first.");
        
        var fullCipher = Convert.FromBase64String(protectedData);

        using var aes = Aes.Create();
        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        // Extract IV from the beginning of the data
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
        
        aes.Key = _key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}