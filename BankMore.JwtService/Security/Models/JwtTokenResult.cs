using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.JwtService.Security.Models;

public sealed class JwtTokenResult
{
    public string Token { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}

