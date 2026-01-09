using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.JwtService.Security.Models;

public sealed class AppicationTokenResult
{
    public string Token { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}

