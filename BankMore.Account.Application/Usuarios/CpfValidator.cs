using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Application.Usuarios;

public static class CpfValidator
{
    public static bool Validate(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        // Remove caracteres não numéricos
        var cpfLimpo = new string(cpf.Where(char.IsDigit).ToArray());

        if (cpfLimpo.Length != 11)
            return false;

        // Bloqueia sequências inválidas (000..., 111..., etc.)
        if (cpfLimpo.All(c => c == cpfLimpo[0]))
            return false;

        return HasValidDigits(cpfLimpo);
    }

    private static bool HasValidDigits(string cpf)
    {
        ReadOnlySpan<char> span = cpf.AsSpan();

        int sum1 = 0;
        for (int i = 0; i < 9; i++)
            sum1 += (span[i] - '0') * (10 - i);

        int remainder1 = sum1 % 11;
        int digit1 = remainder1 < 2 ? 0 : 11 - remainder1;

        if (span[9] - '0' != digit1)
            return false;

        int sum2 = 0;
        for (int i = 0; i < 10; i++)
            sum2 += (span[i] - '0') * (11 - i);

        int remainder2 = sum2 % 11;
        int digit2 = remainder2 < 2 ? 0 : 11 - remainder2;

        return (span[10] - '0') == digit2;
    }
}

