using Microsoft.AspNetCore.DataProtection;

namespace ProseFlow.Infrastructure.Security;

/// <summary>
/// Provides a secure way to encrypt and decrypt sensitive data, such as API keys.
/// </summary>
public class ApiKeyProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("ProseFlow.ApiKey.v1");
    
    /// <summary>
    /// Encrypts a plaintext string.
    /// </summary>
    /// <param name="plainText">The data to protect.</param>
    /// <returns>A protected, Base64-encoded string.</returns>
    public string Protect(string plainText)
    {
        return _protector.Protect(plainText);
    }

    /// <summary>
    /// Decrypts a protected string back to its original form.
    /// </summary>
    /// <param name="protectedData">The data to unprotect.</param>
    /// <returns>The original plaintext string.</returns>
    public string Unprotect(string protectedData)
    {
        return _protector.Unprotect(protectedData);
    }
}