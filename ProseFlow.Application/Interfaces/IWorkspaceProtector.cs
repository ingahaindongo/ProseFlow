namespace ProseFlow.Application.Interfaces;

/// <summary>
/// Defines a contract for encrypting and decrypting sensitive data.
/// </summary>
public interface IWorkspaceProtector
{
    /// <summary>
    /// Initializes the protector with a password for the current scope.
    /// This method must be called before calling Protect or Unprotect.
    /// </summary>
    /// <param name="password">The shared workspace password.</param>
    void Initialize(string password);
    
    /// <summary>
    /// Encrypts a plaintext string.
    /// </summary>
    /// <param name="plainText">The data to protect.</param>
    /// <returns>A protected, Base64-encoded string containing the IV and ciphertext.</returns>
    string Protect(string plainText);

    /// <summary>
    /// Decrypts a protected string back to its original form.
    /// </summary>
    /// <param name="protectedData">The protected data, including the IV.</param>
    /// <returns>The original plaintext string.</returns>
    string Unprotect(string protectedData);
}