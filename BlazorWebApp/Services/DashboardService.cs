using Blazored.LocalStorage;
using MainEcommerceService.Models.ViewModel;
using System.Net.Http.Json;

namespace BlazorWebApp.Services
{
    public class DashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public DashboardService(
            HttpClient httpClient,
            ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        private async Task SetAuthorizationHeader()
        {
            var token = await _localStorage.GetItemAsStringAsync("token");
            if (string.IsNullOrEmpty(token))
            {
                // Nếu không có token, thử refresh
                token = await _localStorage.GetItemAsStringAsync("refreshToken");
            }

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        /// <summary>
        /// Lấy dashboard data cho Admin - CHỈ 1 API CALL
        /// </summary>
        public async Task<AdminDashboardVM> GetAdminDashboardAsync()
        {

                await SetAuthorizationHeader();

                var response = await _httpClient.GetAsync("https://localhost:7260/api/Dashboard/GetAdminDashboardComplete");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<AdminDashboardVM>>();
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }


            return null;
        }

        /// <summary>
        /// Lấy dashboard data cho Seller - CHỈ 1 API CALL
        /// </summary>
        public async Task<SellerDashboardVM> GetSellerDashboardAsync(int sellerId)
        {

                await SetAuthorizationHeader();

                var response = await _httpClient.GetAsync($"https://localhost:7260/api/Dashboard/GetSellerDashboardComplete/{sellerId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<SellerDashboardVM>>();
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }
            return null;
        }

        /// <summary>
        /// Lấy thống kê tổng quan - API nhẹ
        /// </summary>
        public async Task<DashboardStatsVM> GetDashboardStatsAsync(string userRole, int? sellerId = null)
        {

                await SetAuthorizationHeader();

                var url = $"https://localhost:7260/api/Dashboard/GetDashboardStats?userRole={userRole}&sellerId={sellerId}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<DashboardStatsVM>>();
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }


            return null;
        }
    }
}
