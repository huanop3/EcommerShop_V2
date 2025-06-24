using System.Text;
using System.Text.Json;
using MainEcommerceService.Models.ViewModel;
using Blazored.LocalStorage;

namespace BlazorWebApp.Services
{
    public class ShipperProfileService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        public ShipperProfileVM ShipperProfile { get; set; }

        public ShipperProfileService(HttpClient httpClient, ILocalStorageService localStorage)
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

        #region CRUD Operations

        public async Task<List<ShipperProfileVM>?> GetAllShipperProfilesAsync()
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/ShipperProfile/GetAllShipperProfiles");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<ShipperProfileVM>>>();
                if (result?.Success == true && result.Data != null)
                {
                    return result.Data.Where(s => s.IsDeleted == false).ToList();
                }
            }
            return null;
        }

        public async Task<ShipperProfileVM?> GetShipperProfileByIdAsync(int shipperId)
        {
            var response = await _httpClient.GetAsync($"main/api/ShipperProfile/GetShipperProfileById/{shipperId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<ShipperProfileVM>>();
                if (result?.Success == true && result.Data != null && result.Data.IsDeleted == false)
                {
                    return result.Data;
                }
            }
            return null;
        }

        public async Task<ShipperProfileVM?> GetShipperProfileByUserIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"main/api/ShipperProfile/GetShipperProfileByUserId/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<ShipperProfileVM>>();
                if (result?.Success == true && result.Data != null && result.Data.IsDeleted == false)
                {
                    return result.Data;
                }
            }
            return null;
        }

        public async Task<bool> CreateShipperProfileAsync(ShipperProfileVM shipperProfile)
        {
            var json = JsonSerializer.Serialize(shipperProfile);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"main/api/ShipperProfile/CreateShipperProfile", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateShipperProfileAsync(ShipperProfileVM shipperProfile)
        {
            await SetAuthorizationHeader();
            var json = JsonSerializer.Serialize(shipperProfile);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"main/api/ShipperProfile/UpdateShipperProfile", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteShipperProfileAsync(int shipperId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"main/api/ShipperProfile/DeleteShipperProfile/{shipperId}");
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Active/Inactive Operations

        public async Task<bool> ActivateShipperProfileAsync(int shipperId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsync($"main/api/ShipperProfile/ActivateShipperProfile/{shipperId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeactivateShipperProfileAsync(int shipperId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsync($"main/api/ShipperProfile/DeactivateShipperProfile/{shipperId}", null);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Query Operations

        public async Task<List<ShipperProfileVM>?> GetActiveShipperProfilesAsync()
        {
            var response = await _httpClient.GetAsync($"main/api/ShipperProfile/GetActiveShipperProfiles");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<ShipperProfileVM>>>();
                if (result?.Success == true && result.Data != null)
                {
                    return result.Data.ToList();
                }
            }
            return null;
        }

        public async Task<List<ShipperProfileVM>?> GetInactiveShipperProfilesAsync()
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/ShipperProfile/GetInactiveShipperProfiles");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<ShipperProfileVM>>>();
                if (result?.Success == true && result.Data != null)
                {
                    return result.Data.ToList();
                }
            }
            return null;
        }

        public async Task<bool> CheckUserHasShipperProfileAsync(int userId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/ShipperProfile/CheckUserHasShipperProfile/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<bool>>();
                return result?.Success == true && result.Data;
            }
            return false;
        }

        #endregion
    }
}