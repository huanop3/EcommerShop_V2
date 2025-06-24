using Blazored.LocalStorage;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorWebApp.Services
{
    public class ProductImageService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private const string BaseUrl = "product/api/ProductImage";

        public ProductImageService(HttpClient httpClient, ILocalStorageService localStorage)
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

        /// <summary>
        /// Upload ảnh cho sản phẩm
        /// </summary>
        public async Task<ProductImageViewModel?> UploadImageAsync(int productId, IBrowserFile imageFile, bool isPrimary = false, string folder = "products")
        {
            await SetAuthorizationHeader();

            try
            {
                using var formData = new MultipartFormDataContent();
                
                var fileContent = new StreamContent(imageFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
                formData.Add(fileContent, "ImageFile", imageFile.Name);
                formData.Add(new StringContent(productId.ToString()), "ProductId");
                formData.Add(new StringContent(isPrimary.ToString()), "IsPrimary");
                formData.Add(new StringContent(folder), "Folder");

                var response = await _httpClient.PostAsync($"{BaseUrl}/UploadImage", formData);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<HTTPResponseClient<ProductImageViewModel>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        Console.WriteLine($"✅ Image uploaded successfully: {result.Data.ImageUrl}");
                        return result.Data;
                    }
                }

                Console.WriteLine($"❌ Error uploading image: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error uploading image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Upload nhiều ảnh cho sản phẩm
        /// </summary>
        public async Task<List<ProductImageViewModel>> UploadMultipleImagesAsync(int productId, List<IBrowserFile> imageFiles, int? primaryImageIndex = null, string folder = "products")
        {
            await SetAuthorizationHeader();

            try
            {
                using var formData = new MultipartFormDataContent();
                
                foreach (var file in imageFiles)
                {
                    var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    formData.Add(fileContent, "ImageFiles", file.Name);
                }
                
                formData.Add(new StringContent(productId.ToString()), "ProductId");
                if (primaryImageIndex.HasValue)
                {
                    formData.Add(new StringContent(primaryImageIndex.Value.ToString()), "PrimaryImageIndex");
                }
                formData.Add(new StringContent(folder), "Folder");

                var response = await _httpClient.PostAsync($"{BaseUrl}/UploadMultipleImages", formData);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<HTTPResponseClient<List<ProductImageViewModel>>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        Console.WriteLine($"✅ {result.Data.Count} images uploaded successfully");
                        return result.Data;
                    }
                }

                Console.WriteLine($"❌ Error uploading multiple images: {response.StatusCode}");
                return new List<ProductImageViewModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error uploading multiple images: {ex.Message}");
                return new List<ProductImageViewModel>();
            }
        }

        /// <summary>
        /// Lấy tất cả ảnh của sản phẩm
        /// </summary>
        public async Task<ProductImageListViewModel?> GetProductImagesAsync(int productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/GetProductImages/{productId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<HTTPResponseClient<ProductImageListViewModel>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting product images: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy ảnh theo ID
        /// </summary>
        public async Task<ProductImageViewModel?> GetImageByIdAsync(int imageId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/GetImageById/{imageId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<HTTPResponseClient<ProductImageViewModel>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting image by ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy ảnh chính của sản phẩm
        /// </summary>
        public async Task<ProductImageViewModel?> GetPrimaryImageAsync(int productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/GetPrimaryImage/{productId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<HTTPResponseClient<ProductImageViewModel>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting primary image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Đặt ảnh chính cho sản phẩm
        /// </summary>
        public async Task<bool> SetPrimaryImageAsync(int productId, int imageId)
        {
            await SetAuthorizationHeader();

            try
            {
                var response = await _httpClient.PatchAsync($"{BaseUrl}/SetPrimaryImage/{productId}/{imageId}", null);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<HTTPResponseClient<string>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result?.Success == true)
                    {
                        Console.WriteLine($"✅ Primary image set successfully: ProductId={productId}, ImageId={imageId}");
                        return true;
                    }
                }

                Console.WriteLine($"❌ Error setting primary image: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error setting primary image: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa ảnh sản phẩm
        /// </summary>
        public async Task<bool> DeleteImageAsync(int imageId)
        {
            await SetAuthorizationHeader();

            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/DeleteImage/{imageId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<HTTPResponseClient<string>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result?.Success == true)
                    {
                        Console.WriteLine($"✅ Image deleted successfully: {imageId}");
                        return true;
                    }
                }

                Console.WriteLine($"❌ Error deleting image: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deleting image: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa tất cả ảnh của sản phẩm
        /// </summary>
        public async Task<bool> DeleteAllProductImagesAsync(int productId)
        {
            await SetAuthorizationHeader();

            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/DeleteAllProductImages/{productId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<HTTPResponseClient<string>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result?.Success == true)
                    {
                        Console.WriteLine($"✅ All images deleted successfully for product: {productId}");
                        return true;
                    }
                }

                Console.WriteLine($"❌ Error deleting all product images: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deleting all product images: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate file trước khi upload
        /// </summary>
        public bool ValidateImageFile(IBrowserFile file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Size == 0)
            {
                errorMessage = "Không có file được chọn";
                return false;
            }

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                errorMessage = "Định dạng file không hợp lệ. Chỉ chấp nhận JPEG, PNG, GIF, WebP";
                return false;
            }

            if (file.Size > 10 * 1024 * 1024)
            {
                errorMessage = "Kích thước file quá lớn. Tối đa 10MB";
                return false;
            }

            return true;
        }
    }

    // Models đơn giản
    public class ProductImageViewModel
    {
        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string S3Key { get; set; } = string.Empty;
    }

    public class ProductImageListViewModel
    {
        public List<ProductImageViewModel> Images { get; set; } = new();
        public int TotalCount { get; set; }
        public int ProductId { get; set; }
        public ProductImageViewModel? PrimaryImage { get; set; }
    }
}