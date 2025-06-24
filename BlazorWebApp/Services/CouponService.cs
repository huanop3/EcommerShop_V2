using Blazored.LocalStorage;
using MainEcommerceService.Models.ViewModel;

namespace BlazorWebApp.Services
{
    public class CouponService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        public IEnumerable<CouponVM> couponVM { get; set; }

        public CouponService(HttpClient httpClient, ILocalStorageService localStorage)
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
        public async Task<IEnumerable<CouponVM>> GetAllCouponsAsync()
        {
            var response = await _httpClient.GetAsync($"main/api/Coupon/GetAllCoupons");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<CouponVM>>>();
            if (result != null)
            {
                couponVM = result.Data.Where(coupon => coupon.IsDeleted == false).ToList();
            }
            return couponVM;
        }

        public async Task<CouponVM> GetCouponByIdAsync(int couponId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Coupon/GetCouponById/{couponId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<CouponVM>>();
            if (result != null)
            {
                return result.Data;
            }
            return null;
        }

        public async Task<CouponVM> GetCouponByCodeAsync(string couponCode)
        {
            var response = await _httpClient.GetAsync($"main/api/Coupon/GetCouponByCode/{couponCode}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<CouponVM>>();
            if (result != null)
            {
                return result.Data;
            }
            return null;
        }

        public async Task<bool> CreateCouponAsync(CouponVM coupon)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync($"main/api/Coupon/CreateCoupon", coupon);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            if (result != null)
            {
                return result.Success;
            }
            return false;
        }

        public async Task<bool> UpdateCouponAsync(CouponVM coupon)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync($"main/api/Coupon/UpdateCoupon", coupon);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            if (result != null)
            {
                return result.Success;
            }
            return false;
        }

        public async Task<bool> DeleteCouponAsync(int couponId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"main/api/Coupon/DeleteCoupon/{couponId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            if (result != null)
            {
                return result.Success;
            }
            return false;
        }

        public async Task<bool> ActivateCouponAsync(int couponId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsync($"main/api/Coupon/ActivateCoupon/{couponId}", null);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            if (result != null)
            {
                return result.Success;
            }
            return false;
        }

        public async Task<bool> DeactivateCouponAsync(int couponId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsync($"main/api/Coupon/DeactivateCoupon/{couponId}", null);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            if (result != null)
            {
                return result.Success;
            }
            return false;
        }

        public async Task<IEnumerable<CouponVM>> GetActiveCouponsAsync()
        {
            var response = await _httpClient.GetAsync($"main/api/Coupon/GetActiveCoupons");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<CouponVM>>>();
            if (result != null)
            {
                return result.Data;
            }
            return null;
        }

        public async Task<CouponVM> ValidateCouponAsync(string couponCode)
        {
            var response = await _httpClient.GetAsync($"main/api/Coupon/ValidateCoupon/{couponCode}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<CouponVM>>();
            if (result != null)
            {
                return result.Data;
            }
            return null;
        }
        public async Task<bool> UpdateCouponUsageCount(int couponId)
        {
            var response = await _httpClient.PutAsJsonAsync($"main/api/Coupon/UpdateCouponUsageCount/{couponId}", couponId);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<bool>>();
            if (result != null)
            {
                return result.Success;
            }
            return false;
        }
    }
}