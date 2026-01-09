using Microsoft.AspNetCore.Http;

namespace BankMore.Transfer.Infrastructure.Http;

public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(authHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}