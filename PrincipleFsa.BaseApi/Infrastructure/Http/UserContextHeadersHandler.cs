using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace PrincipleFsa.BaseApi.Infrastructure.Http;

public sealed class UserContextHeadersHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextHeadersHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", httpContext.TraceIdentifier);

            var user = httpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var subject =
                    user.FindFirstValue("sub") ??
                    user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    user.FindFirstValue("oid");

                if (!string.IsNullOrWhiteSpace(subject))
                {
                    request.Headers.TryAddWithoutValidation("X-User-Subject", subject);
                }
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}

