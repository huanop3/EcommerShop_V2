using System.Text;
using System.Text.Json;
using MainEcommerceService.Models.ViewModel;
using Blazored.LocalStorage;
using MainEcommerceService.Models.dbMainEcommer;

namespace BlazorWebApp.Services
{
    public class SellerProfileService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        public SellerProfileVM SellerProfile { get; set; }

        public SellerProfileService(HttpClient httpClient, ILocalStorageService localStorage)
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
        #region CRUD Operations

        /// <summary>
        /// Lấy tất cả seller profiles - Admin only
        /// </summary>
        public async Task<List<SellerProfileVM>?> GetAllSellerProfilesAsync()
        {
            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"main/api/SellerProfile/GetAllSellerProfiles");

                if (response.IsSuccessStatusCode)
                {
                    // Sử dụng cùng pattern như các method khác
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<SellerProfileVM>>>();
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data.Where(s => s.IsDeleted == false).ToList();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all seller profiles: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy seller profile theo ID
        /// </summary>
        public async Task<SellerProfileVM?> GetSellerProfileByIdAsync(int sellerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"main/api/SellerProfile/GetSellerProfileById/{sellerId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<SellerProfileVM>>();
                    if (result?.Success == true && result.Data != null && result.Data.IsDeleted == false)
                    {
                        return result.Data;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting seller profile by ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy seller profile theo User ID
        /// </summary>
        public async Task<SellerProfileVM?> GetSellerProfileByUserIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"main/api/SellerProfile/GetSellerProfileByUserId/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<SellerProfileVM>>();
                    if (result?.Success == true && result.Data != null && result.Data.IsDeleted == false)
                    {
                        return result.Data;
                    }
                    return null;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting seller profile by user ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tạo seller profile mới
        /// </summary>
        public async Task<bool> CreateSellerProfileAsync(SellerProfileVM sellerProfile)
        {
            try
            {
                var json = JsonSerializer.Serialize(sellerProfile);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"main/api/SellerProfile/CreateSellerProfile", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating seller profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cập nhật seller profile
        /// </summary>
        public async Task<bool> UpdateSellerProfileAsync(SellerProfileVM sellerProfile)
        {
            await SetAuthorizationHeader();
            try
            {
                var json = JsonSerializer.Serialize(sellerProfile);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"main/api/SellerProfile/UpdateSellerProfile", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating seller profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa seller profile - Admin only
        /// </summary>
        public async Task<bool> DeleteSellerProfileAsync(int sellerId)
        {
            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.DeleteAsync($"main/api/SellerProfile/DeleteSellerProfile/{sellerId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting seller profile: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Verification Operations

        /// <summary>
        /// Xác minh seller profile - Admin only
        /// </summary>
        public async Task<bool> VerifySellerProfileAsync(int sellerId)
        {
            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.PutAsync($"main/api/SellerProfile/VerifySellerProfile/{sellerId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying seller profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Hủy xác minh seller profile - Admin only
        /// </summary>
        public async Task<bool> UnverifySellerProfileAsync(int sellerId)
        {
            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.PutAsync($"main/api/SellerProfile/UnverifySellerProfile/{sellerId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unverifying seller profile: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Lấy danh sách seller profiles đã được xác minh
        /// </summary>
        public async Task<List<SellerProfileVM>?> GetVerifiedSellerProfilesAsync()
        {
            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"main/api/SellerProfile/GetVerifiedSellerProfiles");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<SellerProfileVM>>>();
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data.ToList();
                    }
                    return null;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting verified seller profiles: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy danh sách seller profiles chờ xác minh - Admin only
        /// </summary>
        public async Task<List<SellerProfileVM>?> GetPendingVerificationProfilesAsync()
        {
            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"main/api/SellerProfile/GetPendingVerificationProfiles");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<SellerProfileVM>>>();
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data.ToList();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting pending verification profiles: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra user có seller profile không
        /// </summary>
        public async Task<bool> CheckUserHasSellerProfileAsync(int userId)
        {
            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"main/api/SellerProfile/CheckUserHasSellerProfile/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<bool>>();
                    return result?.Success == true && result.Data;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking user seller profile: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}