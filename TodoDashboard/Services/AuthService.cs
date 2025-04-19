using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace TodoDashboard.Services
{
    public class AuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(GetToken());

        // SetToken
        public void SetToken(string token)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // ใช้เฉพาะกับ HTTPS
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            };
            _httpContextAccessor.HttpContext!.Response.Cookies.Append("UserToken", token, options);
        }

        // GetToken
        public string? GetToken()
        {
            return _httpContextAccessor.HttpContext!.Request.Cookies["UserToken"];
        }

        public HttpClient CreateAuthorizedClient(HttpClient baseClient)
        {
            var client = baseClient;
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }
    }
}