
using BankMore.JwtService.Security.Models;
using System.Security.Claims;

namespace BankMore.JwtService.Security;

public interface ITokenService 
{
    TokenResult GenerateToken(Guid accountId);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
