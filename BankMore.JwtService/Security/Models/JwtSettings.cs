using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.JwtService.Security.Models;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = "BankMore";
    public string Audience { get; init; } = "BankMore";
    public string Secret { get; init; } = default!;
    public int AccessTokenMinutes { get; init; } = 10;
    public int RefreshTokenDays { get; init; } = 7;
}
