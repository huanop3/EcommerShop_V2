using Blazored.LocalStorage;
using MainEcommerceService.Models.ViewModel;

namespace BlazorWebApp.Services
{
    public class DashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public DashboardService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        private async Task SetAuthorizationHeader()
        {
            var token = await _localStorage.GetItemAsStringAsync("token");
            if (string.IsNullOrEmpty(token))
            {
                token = await _localStorage.GetItemAsStringAsync("refreshToken");
            }

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<AdminDashboardVM> GetAdminDashboardAsync()
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("main/api/Dashboard/admin");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<AdminDashboardVM>>();
            return result?.Data ?? new AdminDashboardVM();
        }

        public async Task<SellerDashboardVM> GetSellerDashboardAsync(int sellerId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Dashboard/seller/{sellerId}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<SellerDashboardVM>>();
            return result?.Data ?? new SellerDashboardVM();
        }

        public async Task<SellerDashboardVM> GetSellerDashboardByIdAsync(int sellerId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Dashboard/seller/{sellerId}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<SellerDashboardVM>>();
            return result?.Data ?? new SellerDashboardVM();
        }

        public async Task<DashboardStatsVM> GetDashboardStatsAsync()
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("main/api/Dashboard/stats");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<DashboardStatsVM>>();
            return result?.Data ?? new DashboardStatsVM();
        }

        public async Task<object> GetSystemHealthAsync()
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("main/api/Dashboard/health");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<object>>();
            return result?.Data ?? new object();
        }
    }
}