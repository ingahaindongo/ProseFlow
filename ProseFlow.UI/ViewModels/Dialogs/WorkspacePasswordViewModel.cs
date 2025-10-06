using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.UI.Models;
using ShadUI;

namespace ProseFlow.UI.ViewModels.Dialogs;

/// <summary>
/// Represents the calculated strength of a password.
/// </summary>
public enum PasswordStrength
{
    None,
    VeryWeak,
    Weak,
    Medium,
    Strong,
    VeryStrong
}

public partial class WorkspacePasswordViewModel : ViewModelBase
{
    internal readonly TaskCompletionSource<WorkspacePasswordResult> CompletionSource = new();

    private const int MinimumPasswordLength = 8;

    [ObservableProperty]
    private WorkspacePasswordMode _mode;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private PasswordStrength _currentStrength = PasswordStrength.None;

    [ObservableProperty]
    private string _strengthText = string.Empty;

    // Recommendation state properties
    [ObservableProperty]
    private bool _hasMinimumLength;

    [ObservableProperty]
    private bool _hasUppercase;

    [ObservableProperty]
    private bool _hasLowercase;

    [ObservableProperty]
    private bool _hasNumber;

    [ObservableProperty]
    private bool _hasSymbol;

    public void Initialize(WorkspacePasswordMode mode)
    {
        Mode = mode;
        if (mode == WorkspacePasswordMode.Create)
        {
            Title = "Create Workspace Password";
            Description = "This password will be used to encrypt sensitive data like API keys in your shared folder. All team members will need this password to connect.";
        }
        else
        {
            Title = "Enter Workspace Password";
            Description = "Enter the password to access the shared workspace.";
        }
    }

    partial void OnPasswordChanged(string value)
    {
        // Update overall strength meter
        CurrentStrength = EvaluatePassword(value);
        StrengthText = CurrentStrength switch
        {
            PasswordStrength.VeryWeak => "Very Weak",
            PasswordStrength.Weak => "Weak",
            PasswordStrength.Medium => "Medium",
            PasswordStrength.Strong => "Strong",
            PasswordStrength.VeryStrong => "Very Strong",
            _ => string.Empty
        };

        // Update individual recommendation criteria
        if (string.IsNullOrEmpty(value))
        {
            HasMinimumLength = false;
            HasUppercase = false;
            HasLowercase = false;
            HasNumber = false;
            HasSymbol = false;
        }
        else
        {
            HasMinimumLength = value.Length >= MinimumPasswordLength;
            HasUppercase = value.Any(char.IsUpper);
            HasLowercase = value.Any(char.IsLower);
            HasNumber = value.Any(char.IsDigit);
            HasSymbol = value.Any(c => !char.IsLetterOrDigit(c));
        }
    }

    /// <summary>
    /// Evaluates the strength of a given password based on a set of criteria.
    /// </summary>
    /// <param name="password">The password to evaluate.</param>
    /// <returns>A PasswordStrength enum value representing the calculated strength.</returns>
    private static PasswordStrength EvaluatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return PasswordStrength.None;
        }

        var score = 0;

        // Length score
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;

        // Character type score
        if (Regex.IsMatch(password, "[a-z]")) score++; // Lowercase
        if (Regex.IsMatch(password, "[A-Z]")) score++; // Uppercase
        if (Regex.IsMatch(password, "[0-9]")) score++; // Numbers
        if (Regex.IsMatch(password, @"[\W_]")) score++; // Special characters

        return score switch
        {
            <= 1 => PasswordStrength.VeryWeak,
            2 => PasswordStrength.Weak,
            3 => PasswordStrength.Medium,
            4 => PasswordStrength.Strong,
            _ => PasswordStrength.VeryStrong // Score 5 or 6
        };
    }

    [RelayCommand]
    private void Submit(Window window)
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Password cannot be empty.";
            return;
        }

        if (Mode == WorkspacePasswordMode.Create && Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        CompletionSource.TrySetResult(new WorkspacePasswordResult(true, Password));
        window.Close();
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        window.Close(); // Closing will trigger the OnClosing event in code-behind
    }
}