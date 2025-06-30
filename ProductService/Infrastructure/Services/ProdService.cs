using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProductService.Models.ViewModel;
using ProductService.Models.dbProduct;
using ProductService.Hubs;
using MainEcommerceService.Models.Kafka;

public interface IProdService
{
    Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> GetAllProductsAsync();
    Task<HTTPResponseClient<PrdVMWithImages>> GetProductByIdAsync(int id);
    Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> GetProductsByCategoryAsync(int categoryId);
    Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> GetProductsBySellerAsync(int sellerId);
    Task<HTTPResponseClient<string>> CreateProductAsync(ProductVM product, int userId);
    Task<HTTPResponseClient<string>> UpdateProductAsync(int id, ProductVM product);
    Task<HTTPResponseClient<string>> DeleteProductAsync(int id);
    Task DeleteProductsBySellerId(int sellerId);
    Task<HTTPResponseClient<string>> UpdateProductQuantityAsync(int id, int quantity);
    Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> SearchProductsAsync(string searchTerm);
    Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> GetAllProductByPageAsync(int pageIndex, int pageSize);
    Task<HTTPResponseClient<ProductUpdateMessage>> ProcessOrderItems(OrderCreatedMessage orderMessage);
    Task<HTTPResponseClient<string>> RestoreProductStockAsync(OrderCreatedMessage orderMessage);
}

public class ProdService : IProdService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ProdService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IKafkaProducerService kafkaProducerService,
        IHubContext<NotificationHub> hubContext,
        ILogger<ProdService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
        _kafkaProducerService = kafkaProducerService;
    }

    // Helper method để map Product sang ProductVM với hình ảnh
    private async Task<PrdVMWithImages> MapToProductVMAsync(Product product)
    {
        // Lấy thông tin category
        var category = await _unitOfWork._categoryRepository.GetByIdAsync(product.CategoryId);
        
        // Lấy tất cả ảnh của sản phẩm
        var productImages = await _unitOfWork._productImageRepository.Query()
            .Where(pi => pi.ProductId == product.ProductId && pi.IsDeleted != true)
            .OrderByDescending(pi => pi.IsPrimary)
            .ThenBy(pi => pi.CreatedAt)
            .ToListAsync();

        // Tìm ảnh chính
        var primaryImage = productImages.FirstOrDefault(pi => pi.IsPrimary == true);

        return new PrdVMWithImages
        {
            ProductId = product.ProductId,
            CategoryId = product.CategoryId,
            ProductName = product.ProductName,
            Description = product.Description,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            Quantity = product.Quantity,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            TotalSold = product.TotalSold,
            IsDeleted = product.IsDeleted,
            SellerId = product.SellerId,
            CategoryName = category?.CategoryName ?? "Unknown",
            ParentCategoryId = category?.ParentCategoryId ?? 0, // Lấy ID của danh mục cha, nếu có
            PrimaryImageUrl = primaryImage?.ImageUrl,
            TotalImages = productImages.Count,
            Images = productImages.Select(pi => new ProductImageInfo
            {
                ImageId = pi.ImageId,
                ImageUrl = pi.ImageUrl,
                IsPrimary = pi.IsPrimary ?? false,
                CreatedAt = pi.CreatedAt ?? DateTime.MinValue
            }).ToList()
        };
    }

    // Helper method để map nhiều Product sang ProductVM
    private async Task<List<PrdVMWithImages>> MapToProductVMListAsync(List<Product> products)
    {
        var result = new List<PrdVMWithImages>();

        // Lấy tất cả category IDs
        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
        var categories = await _unitOfWork._categoryRepository.Query()
            .Where(c => categoryIds.Contains(c.CategoryId))
            .ToDictionaryAsync(c => c.CategoryId, c => c.CategoryName);
        var ParentCategories = await _unitOfWork._categoryRepository.Query()
            .Where(c => categoryIds.Contains(c.CategoryId))
            .ToDictionaryAsync(c => c.CategoryId, c => c.ParentCategoryId);
        // Lấy tất cả product IDs
        var productIds = products.Select(p => p.ProductId).ToList();
        var allImages = await _unitOfWork._productImageRepository.Query()
            .Where(pi => productIds.Contains(pi.ProductId) && pi.IsDeleted != true)
            .OrderByDescending(pi => pi.IsPrimary)
            .ThenBy(pi => pi.CreatedAt)
            .ToListAsync();

        // Group images by ProductId
        var imagesByProduct = allImages.GroupBy(pi => pi.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var product in products)
        {
            var productImages = imagesByProduct.GetValueOrDefault(product.ProductId, new List<ProductImage>());
            var primaryImage = productImages.FirstOrDefault(pi => pi.IsPrimary == true);

            result.Add(new PrdVMWithImages
            {
                ProductId = product.ProductId,
                CategoryId = product.CategoryId,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Quantity = product.Quantity,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                TotalSold = product.TotalSold,
                IsDeleted = product.IsDeleted,
                SellerId = product.SellerId,
                CategoryName = categories.GetValueOrDefault(product.CategoryId, "Unknown"),
                ParentCategoryId = ParentCategories.GetValueOrDefault(product.CategoryId, 0) ?? 0, // Lấy ID của danh mục cha, nếu có
                PrimaryImageUrl = primaryImage?.ImageUrl,
                TotalImages = productImages.Count,
                Images = productImages.Select(pi => new ProductImageInfo
                {
                    ImageId = pi.ImageId,
                    ImageUrl = pi.ImageUrl,
                    IsPrimary = pi.IsPrimary ?? false,
                    CreatedAt = pi.CreatedAt ?? DateTime.MinValue
                }).ToList()
            });
        }

        return result;
    }

    public async Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> GetAllProductsAsync()
    {
        var response = new HTTPResponseClient<IEnumerable<PrdVMWithImages>>();
        try
        {
            const string cacheKey = "AllProductsWithImages";

            // Kiểm tra cache trước
            var cachedProducts = await _cacheService.GetAsync<IEnumerable<PrdVMWithImages>>(cacheKey);
            if (cachedProducts != null)
            {
                response.Data = cachedProducts;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách sản phẩm từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Nếu không có trong cache, lấy từ database
            var products = await _unitOfWork._prodRepository.Query()
                .Where(p => p.IsDeleted == false)
                .ToListAsync();

            if (products == null || !products.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm nào";
                return response;
            }

            var productVMs = await MapToProductVMListAsync(products);

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, productVMs, TimeSpan.FromDays(1));

            response.Data = productVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách sản phẩm thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách sản phẩm: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<PrdVMWithImages>> GetProductByIdAsync(int id)
    {
        var response = new HTTPResponseClient<PrdVMWithImages>();
        try
        {
            string cacheKey = $"ProductWithImages_{id}";
            var cachedProduct = await _cacheService.GetAsync<PrdVMWithImages>(cacheKey);
            if (cachedProduct != null)
            {
                response.Data = cachedProduct;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin sản phẩm từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var product = await _unitOfWork._prodRepository.Query()
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsDeleted == false);

            if (product == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm";
                return response;
            }

            var productVM = await MapToProductVMAsync(product);

            // Cache for 1 day
            await _cacheService.SetAsync(cacheKey, productVM, TimeSpan.FromDays(1));

            response.Data = productVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin sản phẩm thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin sản phẩm: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> GetProductsByCategoryAsync(int categoryId)
    {
        var response = new HTTPResponseClient<IEnumerable<PrdVMWithImages>>();
        try
        {
            string cacheKey = $"ProductsByCategoryWithImages_{categoryId}";

            // Kiểm tra cache trước
            var cachedProducts = await _cacheService.GetAsync<IEnumerable<PrdVMWithImages>>(cacheKey);
            if (cachedProducts != null)
            {
                response.Data = cachedProducts;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách sản phẩm theo danh mục từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Lấy danh sách categoryIds bao gồm category hiện tại và các category con
            var categoryIds = new List<int> { categoryId };

            // Kiểm tra xem category này có phải là category cha không
            var childCategories = await _unitOfWork._categoryRepository.Query()
                .Where(c => c.ParentCategoryId == categoryId && c.IsDeleted == false)
                .Select(c => c.CategoryId)
                .ToListAsync();

            if (childCategories.Any())
            {
                categoryIds.AddRange(childCategories);
            }

            // Lấy sản phẩm từ tất cả các category (cha + con)
            var products = await _unitOfWork._prodRepository.Query()
                .Where(p => categoryIds.Contains(p.CategoryId) && p.IsDeleted == false)
                .ToListAsync();

            if (products == null || !products.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm nào trong danh mục này";
                return response;
            }

            var productVMs = await MapToProductVMListAsync(products);

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, productVMs, TimeSpan.FromDays(1));

            response.Data = productVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = $"Lấy danh sách sản phẩm theo danh mục thành công (bao gồm {categoryIds.Count} danh mục)";
            response.DateTime = DateTime.Now;

        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách sản phẩm theo danh mục: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> GetProductsBySellerAsync(int sellerId)
    {
        var response = new HTTPResponseClient<IEnumerable<PrdVMWithImages>>();
        try
        {
            string cacheKey = $"ProductsBySellerWithImages_{sellerId}";

            // Kiểm tra cache trước
            var cachedProducts = await _cacheService.GetAsync<IEnumerable<PrdVMWithImages>>(cacheKey);
            if (cachedProducts != null)
            {
                response.Data = cachedProducts;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách sản phẩm theo người bán từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var products = await _unitOfWork._prodRepository.Query()
                .Where(p => p.SellerId == sellerId && p.IsDeleted == false)
                .ToListAsync();

            if (products == null || !products.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm nào của người bán này";
                return response;
            }

            var productVMs = await MapToProductVMListAsync(products);

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, productVMs, TimeSpan.FromDays(1));

            response.Data = productVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách sản phẩm theo người bán thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách sản phẩm theo người bán: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> GetAllProductByPageAsync(int pageIndex, int pageSize)
    {
        var response = new HTTPResponseClient<IEnumerable<PrdVMWithImages>>();
        try
        {
            string cacheKey = $"PagedProductsWithImages_{pageIndex}_{pageSize}";

            // Kiểm tra cache trước
            var cachedProducts = await _cacheService.GetAsync<IEnumerable<PrdVMWithImages>>(cacheKey);
            if (cachedProducts != null)
            {
                response.Data = cachedProducts;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách sản phẩm phân trang từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Nếu không có trong cache, lấy từ database với phân trang
            var skip = (pageIndex - 1) * pageSize;
            var products = await _unitOfWork._prodRepository.Query()
                .Where(p => p.IsDeleted == false)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            if (products == null || !products.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm nào";
                return response;
            }

            var productVMs = await MapToProductVMListAsync(products);

            // Lưu vào cache với thời gian ngắn hơn
            await _cacheService.SetAsync(cacheKey, productVMs, TimeSpan.FromDays(1));

            response.Data = productVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách sản phẩm theo phân trang thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách sản phẩm theo phân trang: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<PrdVMWithImages>>> SearchProductsAsync(string searchTerm)
    {
        var response = new HTTPResponseClient<IEnumerable<PrdVMWithImages>>();
        try
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Từ khóa tìm kiếm không được để trống";
                return response;
            }

            var products = await _unitOfWork._prodRepository.Query()
                .Where(p => p.IsDeleted == false &&
                           (p.ProductName.Contains(searchTerm) ||
                            p.Description.Contains(searchTerm)))
                .ToListAsync();

            if (products == null || !products.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm nào";
                return response;
            }

            var productVMs = await MapToProductVMListAsync(products);

            response.Data = productVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Tìm kiếm sản phẩm thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tìm kiếm sản phẩm: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    // Cập nhật cache keys để tránh xung đột
    private async Task ClearProductCachesAsync()
    {
        await _cacheService.DeleteByPatternAsync("AllProductsWithImages");
        await _cacheService.DeleteByPatternAsync($"ProductsByCategoryWithImages_*");
        await _cacheService.DeleteByPatternAsync($"ProductsBySellerWithImages_*");
        await _cacheService.DeleteByPatternAsync("PagedProductsWithImages_*");
        await _cacheService.DeleteByPatternAsync("ProductWithImages_*");
    }

    public async Task<HTTPResponseClient<string>> CreateProductAsync(ProductVM product, int userId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();


            var sellerResponse = await _kafkaProducerService.GetSellerByUserIdAsync(userId, 15);

            if (!sellerResponse.Success)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = $"Không tìm thấy thông tin người bán: {sellerResponse.ErrorMessage}";
                return response;
            }

            if (sellerResponse.Data == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy thông tin seller trong response";
                return response;
            }

            var sellerId = sellerResponse.Data.SellerId;

            var newProduct = new Product
            {
                CategoryId = product.CategoryId,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Quantity = product.Quantity,
                CreatedAt = DateTime.UtcNow,
                TotalSold = 0,
                IsDeleted = false,
                SellerId = sellerId
            };

            await _unitOfWork._prodRepository.AddAsync(newProduct);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear caches
            await ClearProductCachesAsync();

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("ProductCreated", newProduct.ProductId, newProduct.ProductName);


            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo sản phẩm thành công";
            response.Data = "Tạo thành công";
            response.DateTime = DateTime.Now;
        }
        catch (TimeoutException ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 408;
            response.Message = "Timeout khi lấy thông tin người bán. Vui lòng thử lại.";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo sản phẩm: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> UpdateProductAsync(int id, ProductVM product)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();
            var existingProduct = await _unitOfWork._prodRepository.GetByIdAsync(id);
            if (existingProduct == null || existingProduct.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm";
                return response;
            }

            existingProduct.CategoryId = product.CategoryId;
            existingProduct.ProductName = product.ProductName;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.DiscountPrice = product.DiscountPrice;
            existingProduct.Quantity = product.Quantity;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            _unitOfWork._prodRepository.Update(existingProduct);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear caches
            await ClearProductCachesAsync();

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("ProductUpdated", id, existingProduct.ProductName);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật sản phẩm thành công";
            response.Data = "Cập nhật thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật sản phẩm: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> DeleteProductAsync(int id)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var product = await _unitOfWork._prodRepository.GetByIdAsync(id);
            if (product == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm";
                return response;
            }

            // Soft delete
            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;

            _unitOfWork._prodRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear caches
            await ClearProductCachesAsync();

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("ProductDeleted", id);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa sản phẩm thành công";
            response.Data = "Xóa thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa sản phẩm: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task DeleteProductsBySellerId(int sellerId)
    {
        try
        {
            await _unitOfWork.BeginTransaction();

            var products = await _unitOfWork._prodRepository.Query()
                .Where(p => p.SellerId == sellerId && p.IsDeleted == false)
                .ToListAsync();

            if (products.Any())
            {
                foreach (var product in products)
                {
                    product.IsDeleted = true;
                    product.UpdatedAt = DateTime.Now;
                    _unitOfWork._prodRepository.Update(product);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransaction();

                // Clear caches
                await ClearProductCachesAsync();

            }
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            throw;
        }
    }

    public async Task<HTTPResponseClient<string>> UpdateProductQuantityAsync(int id, int quantity)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var product = await _unitOfWork._prodRepository.GetByIdAsync(id);
            if (product == null || product.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm";
                return response;
            }

            product.Quantity = quantity;
            product.UpdatedAt = DateTime.UtcNow;

            _unitOfWork._prodRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear caches
            await ClearProductCachesAsync();

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("ProductQuantityUpdated", id, quantity);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật số lượng sản phẩm thành công";
            response.Data = "Cập nhật thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật số lượng sản phẩm: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<ProductUpdateMessage>> ProcessOrderItems(OrderCreatedMessage orderMessage)
    {
        var updateResult = new ProductUpdateMessage
        {
            RequestId = orderMessage.RequestId,
            OrderId = orderMessage.OrderId,
            Success = true,
            UpdatedProducts = new List<ProductUpdateResult>()
        };

        try
        {
            await _unitOfWork.BeginTransaction();

            foreach (var item in orderMessage.OrderItems)
            {
                var product = await _unitOfWork._prodRepository.GetByIdAsync(item.ProductId);

                if (product == null)
                {
                    updateResult.Success = false;
                    updateResult.ErrorMessage = $"Product {item.ProductId} not found";
                    break;
                }

                if (product.Quantity < item.Quantity)
                {
                    updateResult.Success = false;
                    updateResult.ErrorMessage = $"Insufficient stock for product {item.ProductId}. Available: {product.Quantity}, Required: {item.Quantity}";
                    break;
                }

                product.Quantity -= item.Quantity;
                product.UpdatedAt = DateTime.Now;
                _unitOfWork._prodRepository.Update(product);

                updateResult.UpdatedProducts.Add(new ProductUpdateResult
                {
                    ProductId = item.ProductId,
                    UpdatedQuantity = item.Quantity,
                    RemainingStock = product.Quantity
                });
            }

            if (updateResult.Success)
            {
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransaction();
                // Clear caches
                await ClearProductCachesAsync();
            }
            else
            {
                await _unitOfWork.RollbackTransaction();
            }

            return new HTTPResponseClient<ProductUpdateMessage>
            {
                Data = updateResult,
                Success = true,
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            updateResult.Success = false;
            updateResult.ErrorMessage = ex.Message;

            return new HTTPResponseClient<ProductUpdateMessage>
            {
                Data = updateResult,
                Success = false,
                StatusCode = 500
            };
        }
    }

    public async Task<HTTPResponseClient<string>> RestoreProductStockAsync(OrderCreatedMessage orderMessage)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            foreach (var item in orderMessage.OrderItems)
            {
                var product = await _unitOfWork._prodRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    response.Success = false;
                    response.StatusCode = 404;
                    response.Message = $"Không tìm thấy sản phẩm với ID {item.ProductId}";
                    return response;
                }

                product.Quantity += item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;

                _unitOfWork._prodRepository.Update(product);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear caches
            await ClearProductCachesAsync();

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Khôi phục số lượng sản phẩm thành công";
            response.Data = "Khôi phục thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi khôi phục số lượng sản phẩm: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
}