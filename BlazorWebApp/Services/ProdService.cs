using Blazored.LocalStorage;
using MainEcommerceService.Models.ViewModel;
using ProductService.Models.ViewModel;

namespace BlazorWebApp.Services
{
    public class ProdService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        public IEnumerable<PrdVMWithImages> Products { get; set; }

        public ProdService(HttpClient httpClient, ILocalStorageService localStorage)
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
                // Remove quotes if they exist
                token = token.Trim('"');
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<IEnumerable<PrdVMWithImages>> GetAllProductsAsync()
        {
            var response = await _httpClient.GetAsync("product/api/Product/GetAllProducts");
            
            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<PrdVMWithImages>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<PrdVMWithImages>>>();
            
            if (result?.Success == true && result.Data != null)
            {
                Products = result.Data.Where(product => product.IsDeleted == false).ToList();
                return Products;
            }
            
            return Enumerable.Empty<PrdVMWithImages>();
        }
        public async Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> GetAllProductByPageAsync(int pageIndex, int pageSize)
        {
            var response = await _httpClient.GetAsync($"product/api/Product/GetPagedProducts?pageIndex={pageIndex}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
            {
                return new HTTPResponseClient<IEnumerable<PrdVMWithImages>> { Success = false, Message = "Failed to retrieve products." };
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<PrdVMWithImages>>>();
            if (result?.Success == true && result.Data != null)
            {
                var filteredProducts = result.Data.Where(product => product.IsDeleted == false).ToList();
                return new HTTPResponseClient<IEnumerable<PrdVMWithImages>> { Success = true, Data = filteredProducts };
            }
            return new HTTPResponseClient<IEnumerable<PrdVMWithImages>> { Success = false, Message = "No products found." };
        }
        public async Task<PrdVMWithImages> GetProductByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"product/api/Product/GetProductById/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<PrdVMWithImages>>();
            
            if (result?.Success == true && result.Data != null)
                return result.Data;
                
            return null;
        }

        public async Task<IEnumerable<PrdVMWithImages>> GetProductsByCategoryAsync(int categoryId)
        {
            var response = await _httpClient.GetAsync($"product/api/Product/GetProductsByCategory/{categoryId}");
            
            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<PrdVMWithImages>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<PrdVMWithImages>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data.Where(product => product.IsDeleted == false).ToList();
            }

            return Enumerable.Empty<PrdVMWithImages>();
        }

        public async Task<IEnumerable<PrdVMWithImages>> GetProductsBySellerAsync(int sellerId)
        {
            var response = await _httpClient.GetAsync($"product/api/Product/GetProductsBySeller/{sellerId}");
            
            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<PrdVMWithImages>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<PrdVMWithImages>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data.Where(product => product.IsDeleted == false).ToList();
            }

            return Enumerable.Empty<PrdVMWithImages>();
        }

        public async Task<IEnumerable<PrdVMWithImages>> SearchProductsAsync(string searchTerm)
        {
            var response = await _httpClient.GetAsync($"product/api/Product/SearchProducts?searchTerm={Uri.EscapeDataString(searchTerm)}");
            
            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<PrdVMWithImages>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<PrdVMWithImages>>>();
            if (result?.Success == true && result.Data != null)
            {
                return result.Data.Where(product => product.IsDeleted == false && product.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Enumerable.Empty<PrdVMWithImages>();
        }

        public async Task<bool> CreateProductAsync(ProductVM product, int userId)
        {
            if (product == null) return false;
            
            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync($"product/api/Product/CreateProduct?userId={userId}", product);
            
            if (!response.IsSuccessStatusCode) return false;
            
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        public async Task<bool> UpdateProductAsync(int id, ProductVM product)
        {
            if (product == null) return false;
            
            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync($"product/api/Product/UpdateProduct/{id}", product);
            
            if (!response.IsSuccessStatusCode) return false;
            
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"product/api/Product/DeleteProduct/{id}");
            
            if (!response.IsSuccessStatusCode) return false;
            
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        public async Task<bool> UpdateProductQuantityAsync(int id, int quantity)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PatchAsync($"product/api/Product/UpdateProductQuantity/{id}?quantity={quantity}", null);
            
            if (!response.IsSuccessStatusCode) return false;
            
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }
    }
}