using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.JwtService.Security.Models;

public sealed class RefreshToken
{
    public string Token { get; init; } = default!;
    public Guid UserId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool Revoked { get; set; }
}
