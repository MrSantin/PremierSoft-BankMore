using System.Security;
using System.Security.Claims;

namespace BankMore.JwtService.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetContaId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(sub))
            throw new SecurityException("Token inválido: Claim 'sub' não encontrada.");

        if (!Guid.TryParse(sub, out var accountId))
            throw new SecurityException("Token inválido: 'sub' não é um Guid válido.");

        return accountId;
    }
}
