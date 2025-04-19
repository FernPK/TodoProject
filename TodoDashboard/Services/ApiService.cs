using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TodoDashboard.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigation;
        private readonly IJSRuntime _js;

        public ApiService(HttpClient httpClient, NavigationManager navigation, IJSRuntime js)
        {
            _httpClient = httpClient;
            _navigation = navigation;
            _js = js;
        }

        private void AttachAuthorizationHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            try
            {
                var token = await _js.InvokeAsync<string>("localStorage.getItem", "userToken");
                if (string.IsNullOrEmpty(token))
                {
                    _navigation.NavigateTo("/login");
                    return default;
                }

                AttachAuthorizationHeader(token);
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _navigation.NavigateTo("/login");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ API GET error: {ex.Message}");
            }
            return default;
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", "userToken");
            if (string.IsNullOrEmpty(token))
            {
                _navigation.NavigateTo("/login");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            AttachAuthorizationHeader(token);
            var response = await _httpClient.DeleteAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _navigation.NavigateTo("/login");
            }
            return response;
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string url, T content)
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", "userToken");
            if (string.IsNullOrEmpty(token))
            {
                _navigation.NavigateTo("/login");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            AttachAuthorizationHeader(token);
            var response = await _httpClient.PostAsJsonAsync(url, content);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _navigation.NavigateTo("/login");
            }
            return response;
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string url, T content)
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", "userToken");
            if (string.IsNullOrEmpty(token))
            {
                _navigation.NavigateTo("/login");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            AttachAuthorizationHeader(token);
            var response = await _httpClient.PutAsJsonAsync(url, content);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _navigation.NavigateTo("/login");
            }
            return response;
        }
    }
}