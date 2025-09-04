using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CRMWebApp.Services
{
    public interface IUserContext
    {
        string? UserId { get; }
        bool IsAdmin { get; }
    }

    public sealed class HttpUserContext : IUserContext
    {
        private readonly IHttpContextAccessor _http;
        public HttpUserContext(IHttpContextAccessor http) => _http = http;

        public string? UserId =>
            _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public bool IsAdmin =>
            _http.HttpContext?.User?.IsInRole("Admin") ?? false;
    }
}