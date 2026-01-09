using System.Text.Json;
using BankMore.JwtService.Security;
using BankMore.JwtService.Security.Models;
using StackExchange.Redis;

namespace BankMore.Account.Infrastructure.Security;

public sealed class RedisRefreshTokenStore : IRefreshTokenStore
{
    private readonly IDatabase _db;

    public RedisRefreshTokenStore(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    private static string Key(string token) => $"auth:refresh:{token}";

    public async Task SaveAsync(RefreshToken refreshToken, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(refreshToken);

        var ttl = refreshToken.ExpiresAt - DateTime.Now;
        if (ttl <= TimeSpan.Zero)
            ttl = TimeSpan.FromSeconds(1);

        await _db.StringSetAsync(Key(refreshToken.Token), json, ttl).ConfigureAwait(false);
    }

    public async Task<RefreshToken?> GetAsync(string refreshToken, CancellationToken ct)
    {
        var value = await _db.StringGetAsync(Key(refreshToken)).ConfigureAwait(false);

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<RefreshToken>((byte[])value!);
    }


    public async Task RevokeAsync(string refreshToken, CancellationToken ct)
    {
        await _db.KeyDeleteAsync(Key(refreshToken)).ConfigureAwait(false);
    }
}

