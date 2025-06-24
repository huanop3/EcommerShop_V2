using Blazored.LocalStorage;
using MainEcommerceService.Models.ViewModel;

namespace BlazorWebApp.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        public IEnumerable<UserVM> userVM { get; set; }

        public UserService(HttpClient httpClient, ILocalStorageService localStorage)
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
        public async Task<IEnumerable<UserVM>> GetAllUserAsync()
        {
            await SetAuthorizationHeader();

            var response = await _httpClient.GetAsync("main/api/User/GetAllUser");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<UserVM>>>();
            userVM = result?.Data?.Where(user => user.IsDeleted == false) ?? Enumerable.Empty<UserVM>();
            return userVM;
        }

        public async Task<IEnumerable<UserVM>> GetUsersByPageAsync(int pageIndex, int pageSize)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/User/GetUsersByPage?pageIndex={pageIndex}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<UserVM>>>();
            userVM = result?.Data?.Where(user => user.IsActive == true) ?? Enumerable.Empty<UserVM>();
            return userVM;
        }

        public async Task<IEnumerable<RoleVM>> GetAllRoleAsync()
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("main/api/User/GetAllRole");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<RoleVM>>>();
            return result?.Data ?? Enumerable.Empty<RoleVM>();
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(UserListVM user)
        {
            if (user == null) return (false, "User data is null");

            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync("main/api/User/UpdateUser", user);

            if (!response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
                return (false, result?.Message);
            }

            return (true, "User updated successfully");
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"main/api/User/DeleteUser?id={id}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        public async Task<ProfileVM> GetProfileAsync(int id)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/User/GetUserProfile?userId={id}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<ProfileVM>>();

            if (result?.Success == true && result.Data != null)
                return result.Data;

            return null;
        }

        public async Task<bool> UpdateProfileAsync(ProfileVM profile)
        {
            if (profile == null) return false;
            await SetAuthorizationHeader();

            var response = await _httpClient.PutAsJsonAsync("main/api/User/UpdateUserProfile", profile);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }
    }
}