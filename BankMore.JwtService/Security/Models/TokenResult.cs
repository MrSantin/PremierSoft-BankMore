using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.JwtService.Security.Models;

public sealed class TokenResult
{
    public string AccessToken { get; init; } = default!;
    public string RefreshToken { get; init; } = default!;
    public DateTime AccessTokenExpiresAt { get; init; }
}

