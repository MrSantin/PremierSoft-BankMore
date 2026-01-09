
using BankMore.JwtService.Security.Models;

namespace BankMore.JwtService.Security;

public interface IRefreshTokenStore 
{
    Task SaveAsync(RefreshToken refreshToken, CancellationToken ct);
    Task<RefreshToken?> GetAsync(string refreshToken, CancellationToken ct);
    Task RevokeAsync(string refreshToken, CancellationToken ct);
}
