
namespace BankMore.Account.Application.Shared;

public static class PasswordValidator
{
    private const int MinLength = 8;
    //Presumi ser necessário uma política de senha forte, devido à natureza do projeto
    public static bool SenhaValida(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return false;

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        var hasSpecial = false;

        foreach (var c in password)
        {
            switch (c)
            {
                case char ch when char.IsWhiteSpace(ch):
                    return false; 
                case char ch when char.IsUpper(ch):
                    hasUpper = true;
                    break;
                case char ch when char.IsLower(ch):
                    hasLower = true;
                    break;
                case char ch when char.IsDigit(ch):
                    hasDigit = true;
                    break;
                case char ch when char.IsPunctuation(ch) || char.IsSymbol(ch):
                    hasSpecial = true;
                    break;
            }

            if (hasUpper && hasLower && hasDigit && hasSpecial)
                return true;
        }

        return false;
    }
}
