using Blazored.LocalStorage;
using MainEcommerceService.Models.ViewModel;

namespace BlazorWebApp.Services
{
    public class CategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        public IEnumerable<CategoryVM> categoryVM { get; set; }

        public CategoryService(HttpClient httpClient, ILocalStorageService localStorage)
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

        public async Task<IEnumerable<CategoryVM>> GetAllCategoriesAsync()
        {
            var response = await _httpClient.GetAsync($"product/api/Category/GetAllCategories");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<CategoryVM>>>();
            if (result != null)
            {
                categoryVM = result.Data.Where(c => c.IsDeleted != true);
            }
            return categoryVM;
        }

        public async Task<CategoryVM> GetCategoryByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"product/api/Category/GetCategoryById/{id}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<CategoryVM>>();
            if (result != null)
            {
                return result.Data;
            }
            return null;
        }

        public async Task<bool> CreateCategoryAsync(CategoryVM category)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync($"product/api/Category/CreateCategory", category);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            if (result != null)
            {
                return result.Success;
            }
            return false;
        }

        public async Task<bool> UpdateCategoryAsync(int id, CategoryVM category)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync($"product/api/Category/UpdateCategory/{id}", category);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            if (result != null)
            {
                return result.Success;
            }
            return false;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"product/api/Category/DeleteCategory/{id}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            if (result != null)
            {
                return result.Success;
            }
            return false;
        }
    }
}
