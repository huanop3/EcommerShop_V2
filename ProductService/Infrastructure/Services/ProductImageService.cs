using Microsoft.EntityFrameworkCore;
using ProductService.ViewModels;
using ProductService.Models.dbProduct;
using ProductService.Infrastructure.Services;

public interface IProductImageService
{
    Task<HTTPResponseClient<ProductImageViewModel>> UploadImageAsync(CreateProductImageViewModel model);
    Task<HTTPResponseClient<List<ProductImageViewModel>>> UploadMultipleImagesAsync(UploadProductImagesViewModel model);
    Task<HTTPResponseClient<ProductImageViewModel>> UpdateImageAsync(UpdateProductImageViewModel model);
    Task<HTTPResponseClient<string>> DeleteImageAsync(int imageId);
    Task<HTTPResponseClient<string>> DeleteAllProductImagesAsync(int productId);
    Task<HTTPResponseClient<ProductImageListViewModel>> GetProductImagesAsync(int productId);
    Task<HTTPResponseClient<ProductImageViewModel>> GetImageByIdAsync(int imageId);
    Task<HTTPResponseClient<string>> SetPrimaryImageAsync(int productId, int imageId);
    Task<HTTPResponseClient<ProductImageViewModel>> GetPrimaryImageAsync(int productId);
}

public class ProductImageService : IProductImageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IS3Service _s3Service;
    private readonly RedisHelper _cacheService;

    public ProductImageService(
        IUnitOfWork unitOfWork,
        IS3Service s3Service,
        RedisHelper cacheService,
        ILogger<ProductImageService> logger)
    {
        _unitOfWork = unitOfWork;
        _s3Service = s3Service;
        _cacheService = cacheService;
    }

    public async Task<HTTPResponseClient<ProductImageViewModel>> UploadImageAsync(CreateProductImageViewModel model)
    {
        var response = new HTTPResponseClient<ProductImageViewModel>();
        try
        {
            await _unitOfWork.BeginTransaction();

            // Validate file
            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Không có file được tải lên";
                return response;
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(model.ImageFile.ContentType.ToLower()))
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Định dạng file không hợp lệ. Chỉ chấp nhận JPEG, PNG, GIF, WebP";
                return response;
            }

            // Validate file size (10MB max)
            if (model.ImageFile.Length > 10 * 1024 * 1024)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Kích thước file quá lớn. Tối đa 10MB";
                return response;
            }

            // If this is set as primary, remove primary flag from other images
            if (model.IsPrimary)
            {
                var existingPrimary = await _unitOfWork._productImageRepository.Query()
                    .Where(pi => pi.ProductId == model.ProductId && pi.IsPrimary == true && pi.IsDeleted != true)
                    .ToListAsync();

                foreach (var img in existingPrimary)
                {
                    img.IsPrimary = false;
                    img.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork._productImageRepository.Update(img);
                }
            }

            // Create S3 key
            var fileExtension = Path.GetExtension(model.ImageFile.FileName);
            var s3Key = $"{model.Folder}/product-{model.ProductId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{fileExtension}";

            // Upload to S3
            var imageUrl = await _s3Service.UploadImageAsync(model.ImageFile, s3Key);

            // Save to database
            var productImage = new ProductImage
            {
                ProductId = model.ProductId,
                ImageUrl = imageUrl,
                IsPrimary = model.IsPrimary,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork._productImageRepository.AddAsync(productImage);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear cache
            await _cacheService.DeleteByPatternAsync($"ProductImages_*");

            var resultViewModel = MapToViewModel(productImage, s3Key, model.ImageFile.FileName);

            response.Data = resultViewModel;
            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Upload ảnh thành công";
            response.DateTime = DateTime.Now;

        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi upload ảnh: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<List<ProductImageViewModel>>> UploadMultipleImagesAsync(UploadProductImagesViewModel model)
    {
        var response = new HTTPResponseClient<List<ProductImageViewModel>>();
        try
        {
            if (model.ImageFiles == null || !model.ImageFiles.Any())
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Không có file được tải lên";
                return response;
            }

            if (model.ImageFiles.Count > 10)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Tối đa 10 file được phép upload";
                return response;
            }

            var results = new List<ProductImageViewModel>();

            // If primary image index is specified, remove primary flag from existing images
            if (model.PrimaryImageIndex.HasValue)
            {
                var existingPrimary = await _unitOfWork._productImageRepository.Query()
                    .Where(pi => pi.ProductId == model.ProductId && pi.IsPrimary == true && pi.IsDeleted != true)
                    .ToListAsync();

                foreach (var img in existingPrimary)
                {
                    img.IsPrimary = false;
                    img.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork._productImageRepository.Update(img);
                }
            }

            for (int i = 0; i < model.ImageFiles.Count; i++)
            {
                var file = model.ImageFiles[i];
                bool isPrimary = model.PrimaryImageIndex == i;

                var uploadModel = new CreateProductImageViewModel
                {
                    ProductId = model.ProductId,
                    ImageFile = file,
                    IsPrimary = isPrimary,
                    Folder = model.Folder
                };

                var result = await UploadImageAsync(uploadModel);
                if (result.Success && result.Data != null)
                {
                    results.Add(result.Data);
                }
            }

            response.Data = results;
            response.Success = true;
            response.StatusCode = 201;
            response.Message = $"Upload thành công {results.Count}/{model.ImageFiles.Count} ảnh";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi upload nhiều ảnh: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<ProductImageViewModel>> UpdateImageAsync(UpdateProductImageViewModel model)
    {
        var response = new HTTPResponseClient<ProductImageViewModel>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var existingImage = await _unitOfWork._productImageRepository.Query()
                .FirstOrDefaultAsync(pi => pi.ImageId == model.ImageId && pi.IsDeleted != true);

            if (existingImage == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy ảnh";
                return response;
            }

            // If setting as primary, remove primary flag from other images
            if (model.IsPrimary == true)
            {
                var otherPrimary = await _unitOfWork._productImageRepository.Query()
                    .Where(pi => pi.ProductId == existingImage.ProductId && 
                               pi.ImageId != model.ImageId && 
                               pi.IsPrimary == true && 
                               pi.IsDeleted != true)
                    .ToListAsync();

                foreach (var img in otherPrimary)
                {
                    img.IsPrimary = false;
                    img.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork._productImageRepository.Update(img);
                }

                existingImage.IsPrimary = true;
            }
            else if (model.IsPrimary == false)
            {
                existingImage.IsPrimary = false;
            }

            // If new image file provided, upload new one and delete old
            if (model.NewImageFile != null)
            {
                // Extract S3 key from existing URL and delete
                var oldS3Key = ExtractS3KeyFromUrl(existingImage.ImageUrl);
                if (!string.IsNullOrEmpty(oldS3Key))
                {
                    await _s3Service.DeleteImageAsync(oldS3Key);
                }

                // Upload new image
                var fileExtension = Path.GetExtension(model.NewImageFile.FileName);
                var newS3Key = $"{model.Folder}/product-{existingImage.ProductId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{fileExtension}";
                var newImageUrl = await _s3Service.UploadImageAsync(model.NewImageFile, newS3Key);

                existingImage.ImageUrl = newImageUrl;
            }

            existingImage.UpdatedAt = DateTime.UtcNow;
            _unitOfWork._productImageRepository.Update(existingImage);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear cache
            await _cacheService.DeleteByPatternAsync($"ProductImages_*");

            response.Data = MapToViewModel(existingImage);
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật ảnh thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật ảnh: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> DeleteImageAsync(int imageId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var image = await _unitOfWork._productImageRepository.Query()
                .FirstOrDefaultAsync(pi => pi.ImageId == imageId && pi.IsDeleted != true);

            if (image == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy ảnh";
                return response;
            }

            // Extract S3 key from URL and delete from S3
            var s3Key = ExtractS3KeyFromUrl(image.ImageUrl);
            if (!string.IsNullOrEmpty(s3Key))
            {
                await _s3Service.DeleteImageAsync(s3Key);
            }

            // Soft delete from database
            image.IsDeleted = true;
            image.UpdatedAt = DateTime.UtcNow;
            _unitOfWork._productImageRepository.Update(image);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear cache
            await _cacheService.DeleteByPatternAsync($"ProductImages_*");

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa ảnh thành công";
            response.Data = "Xóa thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa ảnh: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> DeleteAllProductImagesAsync(int productId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var images = await _unitOfWork._productImageRepository.Query()
                .Where(pi => pi.ProductId == productId && pi.IsDeleted != true)
                .ToListAsync();

            // Delete from S3
            foreach (var image in images)
            {
                var s3Key = ExtractS3KeyFromUrl(image.ImageUrl);
                if (!string.IsNullOrEmpty(s3Key))
                {
                    await _s3Service.DeleteImageAsync(s3Key);
                }

                // Soft delete from database
                image.IsDeleted = true;
                image.UpdatedAt = DateTime.UtcNow;
                _unitOfWork._productImageRepository.Update(image);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear cache
            await _cacheService.DeleteByPatternAsync($"ProductImages_*");

            response.Success = true;
            response.StatusCode = 200;
            response.Message = $"Xóa thành công {images.Count} ảnh";
            response.Data = "Xóa thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa tất cả ảnh: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<ProductImageListViewModel>> GetProductImagesAsync(int productId)
    {
        var response = new HTTPResponseClient<ProductImageListViewModel>();
        try
        {
            string cacheKey = $"ProductImages_{productId}";
            var cachedResult = await _cacheService.GetAsync<ProductImageListViewModel>(cacheKey);
            if (cachedResult != null)
            {
                response.Data = cachedResult;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách ảnh từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var images = await _unitOfWork._productImageRepository.Query()
                .Where(pi => pi.ProductId == productId && pi.IsDeleted != true)
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.CreatedAt)
                .ToListAsync();

            var primaryImage = images.FirstOrDefault(i => i.IsPrimary == true);
            var result = new ProductImageListViewModel
            {
                ProductId = productId,
                Images = images.Select(i => MapToViewModel(i)).ToList(),
                TotalCount = images.Count,
                PrimaryImage = primaryImage != null ? MapToViewModel(primaryImage) : null
            };

            // Cache for 15 minutes
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromDays(1));

            response.Data = result;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách ảnh thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách ảnh: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<ProductImageViewModel>> GetImageByIdAsync(int imageId)
    {
        var response = new HTTPResponseClient<ProductImageViewModel>();
        try
        {
            var image = await _unitOfWork._productImageRepository.Query()
                .FirstOrDefaultAsync(pi => pi.ImageId == imageId && pi.IsDeleted != true);

            if (image == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy ảnh";
                return response;
            }

            response.Data = MapToViewModel(image);
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin ảnh thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin ảnh: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> SetPrimaryImageAsync(int productId, int imageId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            // Remove primary from all images of this product
            var allImages = await _unitOfWork._productImageRepository.Query()
                .Where(pi => pi.ProductId == productId && pi.IsDeleted != true)
                .ToListAsync();

            foreach (var img in allImages)
            {
                img.IsPrimary = img.ImageId == imageId;
                img.UpdatedAt = DateTime.UtcNow;
                _unitOfWork._productImageRepository.Update(img);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear cache
            await _cacheService.DeleteByPatternAsync($"ProductImages_*");

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Đặt ảnh chính thành công";
            response.Data = "Thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi đặt ảnh chính: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<ProductImageViewModel>> GetPrimaryImageAsync(int productId)
    {
        var response = new HTTPResponseClient<ProductImageViewModel>();
        try
        {
            var image = await _unitOfWork._productImageRepository.Query()
                .FirstOrDefaultAsync(pi => pi.ProductId == productId && pi.IsPrimary == true && pi.IsDeleted != true);

            if (image == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy ảnh chính";
                return response;
            }

            response.Data = MapToViewModel(image);
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy ảnh chính thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy ảnh chính: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    private ProductImageViewModel MapToViewModel(ProductImage image, string s3Key = "", string fileName = "")
    {
        return new ProductImageViewModel
        {
            ImageId = image.ImageId,
            ProductId = image.ProductId,
            ImageUrl = image.ImageUrl,
            IsPrimary = image.IsPrimary ?? false,
            CreatedAt = image.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = image.UpdatedAt ?? DateTime.MinValue,
            S3Key = s3Key,
            FileName = fileName
        };
    }

    private string ExtractS3KeyFromUrl(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl)) return "";
            var uri = new Uri(imageUrl);
            return uri.AbsolutePath.TrimStart('/');
        }
        catch
        {
            return "";
        }
    }
}