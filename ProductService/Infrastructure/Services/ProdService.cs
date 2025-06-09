using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProductService.Models.ViewModel;
using ProductService.Models.dbProduct;
using ProductService.Hubs;
using MainEcommerceService.Models.Kafka;

public interface IProdService
{
    Task<HTTPResponseClient<IEnumerable<ProductVM>>> GetAllProductsAsync();
    Task<HTTPResponseClient<ProductVM>> GetProductByIdAsync(int id);
    Task<HTTPResponseClient<IEnumerable<ProductVM>>> GetProductsByCategoryAsync(int categoryId);
    Task<HTTPResponseClient<IEnumerable<ProductVM>>> GetProductsBySellerAsync(int sellerId);
    Task<HTTPResponseClient<string>> CreateProductAsync(ProductVM product, int userId);
    Task<HTTPResponseClient<string>> UpdateProductAsync(int id, ProductVM product);
    Task<HTTPResponseClient<string>> DeleteProductAsync(int id);
    Task DeleteProductsBySellerId(int sellerId);
    Task<HTTPResponseClient<string>> UpdateProductQuantityAsync(int id, int quantity);
    Task<HTTPResponseClient<IEnumerable<ProductVM>>> SearchProductsAsync(string searchTerm);
    // Thêm method mới
    Task<HTTPResponseClient<IEnumerable<ProductVM>>> GetAllProductByPageAsync(int pageIndex, int pageSize);
    Task<HTTPResponseClient<ProductUpdateMessage>> ProcessOrderItems(OrderCreatedMessage orderMessage);
    Task<HTTPResponseClient<string>> RestoreProductStockAsync(OrderCreatedMessage orderMessage);

}

public class ProdService : IProdService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly ILogger<ProdService> _logger;
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
        _logger = logger;
        _kafkaProducerService = kafkaProducerService;
    }

    public async Task<HTTPResponseClient<IEnumerable<ProductVM>>> GetAllProductsAsync()
    {
        var response = new HTTPResponseClient<IEnumerable<ProductVM>>();
        try
        {
            const string cacheKey = "AllProducts";

            // Kiểm tra cache trước
            var cachedProducts = await _cacheService.GetAsync<IEnumerable<ProductVM>>(cacheKey);
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

            var productVMs = products.Select(p => new ProductVM
            {
                ProductId = p.ProductId,
                CategoryId = p.CategoryId,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Quantity = p.Quantity,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TotalSold = p.TotalSold,
                IsDeleted = p.IsDeleted,
                SellerId = p.SellerId
            }).ToList();

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, productVMs, TimeSpan.FromMinutes(30));

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

    public async Task<HTTPResponseClient<ProductVM>> GetProductByIdAsync(int id)
    {
        var response = new HTTPResponseClient<ProductVM>();
        try
        {
            var product = await _unitOfWork._prodRepository.Query()
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsDeleted == false);

            if (product == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy sản phẩm";
                return response;
            }

            var productVM = new ProductVM
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
                SellerId = product.SellerId
            };

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

    public async Task<HTTPResponseClient<IEnumerable<ProductVM>>> GetProductsByCategoryAsync(int categoryId)
    {
        var response = new HTTPResponseClient<IEnumerable<ProductVM>>();
        try
        {
            string cacheKey = $"ProductsByCategory_{categoryId}";

            // Kiểm tra cache trước
            var cachedProducts = await _cacheService.GetAsync<IEnumerable<ProductVM>>(cacheKey);
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

            // Kiểm tra xem category này có phải là category cha không (ParentCategoryId = NULL)
            // Nếu có thì lấy thêm tất cả category con
            var childCategories = await _unitOfWork._categoryRepository.Query()
                .Where(c => c.ParentCategoryId == categoryId && c.IsDeleted == false)
                .Select(c => c.CategoryId)
                .ToListAsync();

            if (childCategories.Any())
            {
                categoryIds.AddRange(childCategories);
                _logger.LogInformation("Found {ChildCount} child categories for parent category {CategoryId}",
                    childCategories.Count, categoryId);
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

            var productVMs = products.Select(p => new ProductVM
            {
                ProductId = p.ProductId,
                CategoryId = p.CategoryId,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Quantity = p.Quantity,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TotalSold = p.TotalSold,
                IsDeleted = p.IsDeleted,
                SellerId = p.SellerId
            }).ToList();

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, productVMs, TimeSpan.FromMinutes(15));

            response.Data = productVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = $"Lấy danh sách sản phẩm theo danh mục thành công (bao gồm {categoryIds.Count} danh mục)";
            response.DateTime = DateTime.Now;

            _logger.LogInformation("Retrieved {ProductCount} products from {CategoryCount} categories (Parent: {ParentId})",
                productVMs.Count, categoryIds.Count, categoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by category {CategoryId}", categoryId);
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách sản phẩm theo danh mục: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<ProductVM>>> GetProductsBySellerAsync(int sellerId)
    {
        var response = new HTTPResponseClient<IEnumerable<ProductVM>>();
        try
        {
            string cacheKey = $"ProductsBySeller_{sellerId}";

            // Kiểm tra cache trước
            var cachedProducts = await _cacheService.GetAsync<IEnumerable<ProductVM>>(cacheKey);
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

            var productVMs = products.Select(p => new ProductVM
            {
                ProductId = p.ProductId,
                CategoryId = p.CategoryId,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Quantity = p.Quantity,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TotalSold = p.TotalSold,
                IsDeleted = p.IsDeleted,
                SellerId = p.SellerId
            }).ToList();

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, productVMs, TimeSpan.FromMinutes(15));

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

    public async Task<HTTPResponseClient<string>> CreateProductAsync(ProductVM product, int userId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            // 🔥 FIX: Gọi hàm lấy sellerId từ Kafka với proper error handling
            _logger.LogInformation("🔍 Getting seller info for userId: {UserId}", userId);

            var sellerResponse = await _kafkaProducerService.GetSellerByUserIdAsync(userId, 15); // Tăng timeout lên 15s

            if (!sellerResponse.Success)
            {
                _logger.LogWarning("⚠️ Failed to get seller info: {ErrorMessage}", sellerResponse.ErrorMessage);
                response.Success = false;
                response.StatusCode = 404;
                response.Message = $"Không tìm thấy thông tin người bán: {sellerResponse.ErrorMessage}";
                return response;
            }

            // 🔥 FIX: Kiểm tra Data có tồn tại không
            if (sellerResponse.Data == null)
            {
                _logger.LogWarning("⚠️ Seller response data is null for userId: {UserId}", userId);
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy thông tin seller trong response";
                return response;
            }

            // 🔥 FIX: Sử dụng Data.SellerId thay vì res.SellerId
            var sellerId = sellerResponse.Data.SellerId;
            _logger.LogInformation("✅ Found seller: SellerId={SellerId}, StoreName={StoreName}",
                sellerId, sellerResponse.Data.StoreName);

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
                SellerId = sellerId // 🔥 FIX: Sử dụng sellerId từ Data
            };

            await _unitOfWork._prodRepository.AddAsync(newProduct);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllProducts");
            await _cacheService.DeleteByPatternAsync($"ProductsByCategory_{product.CategoryId}");
            await _cacheService.DeleteByPatternAsync($"ProductsBySeller_{sellerId}");
            // Xóa cache phân trang
            await _cacheService.DeleteByPatternAsync("PagedProducts_*");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("ProductCreated", newProduct.ProductId, newProduct.ProductName);

            _logger.LogInformation("✅ Product created successfully: ProductId={ProductId}, SellerId={SellerId}",
                newProduct.ProductId, sellerId);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo sản phẩm thành công";
            response.Data = "Tạo thành công";
            response.DateTime = DateTime.Now;
        }
        catch (TimeoutException ex)
        {
            await _unitOfWork.RollbackTransaction();
            _logger.LogError(ex, "❌ Timeout when getting seller info for userId: {UserId}", userId);
            response.Success = false;
            response.StatusCode = 408;
            response.Message = "Timeout khi lấy thông tin người bán. Vui lòng thử lại.";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            _logger.LogError(ex, "❌ Error creating product for userId: {UserId}", userId);
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

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllProducts");
            await _cacheService.DeleteByPatternAsync($"ProductsByCategory_*");
            await _cacheService.DeleteByPatternAsync($"ProductsBySeller_*");
            // Xóa cache phân trang
            await _cacheService.DeleteByPatternAsync("PagedProducts_*");

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

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllProducts");
            await _cacheService.DeleteByPatternAsync($"ProductsByCategory_*");
            await _cacheService.DeleteByPatternAsync($"ProductsBySeller_*");
            // Xóa cache phân trang
            await _cacheService.DeleteByPatternAsync("PagedProducts_*");

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
    // ...existing code...
    public async Task DeleteProductsBySellerId(int sellerId)
    {
        try
        {
            await _unitOfWork.BeginTransaction();

            // Lấy tất cả sản phẩm của seller
            var products = await _unitOfWork._prodRepository.Query()
                .Where(p => p.SellerId == sellerId && p.IsDeleted == false)
                .ToListAsync();

            if (products.Any())
            {
                // Soft delete tất cả sản phẩm
                foreach (var product in products)
                {
                    product.IsDeleted = true;
                    product.UpdatedAt = DateTime.Now;
                    _unitOfWork._prodRepository.Update(product);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransaction();

                // Xóa cache sản phẩm
                foreach (var product in products)
                {
                    await _cacheService.DeleteByPatternAsync($"Product_*");
                }

                await _cacheService.DeleteByPatternAsync($"ProductsBySeller_{sellerId}");
                await _cacheService.DeleteByPatternAsync("AllProducts");
                // Xóa cache phân trang
                await _cacheService.DeleteByPatternAsync("PagedProducts_*");

                _logger.LogInformation("Successfully deleted {Count} products for seller {SellerId}", products.Count, sellerId);
            }
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            _logger.LogError(ex, "Error deleting products for seller {SellerId}", sellerId);
            throw;
        }
    }
    // ...existing code...
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

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllProducts");
            await _cacheService.DeleteByPatternAsync($"ProductsByCategory_*");
            await _cacheService.DeleteByPatternAsync($"ProductsBySeller_*");
            // Xóa cache phân trang
            await _cacheService.DeleteByPatternAsync("PagedProducts_*");

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

    public async Task<HTTPResponseClient<IEnumerable<ProductVM>>> SearchProductsAsync(string searchTerm)
    {
        var response = new HTTPResponseClient<IEnumerable<ProductVM>>();
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

            var productVMs = products.Select(p => new ProductVM
            {
                ProductId = p.ProductId,
                CategoryId = p.CategoryId,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Quantity = p.Quantity,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TotalSold = p.TotalSold,
                IsDeleted = p.IsDeleted,
                SellerId = p.SellerId
            }).ToList();

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

    // Thêm method implementation
    public async Task<HTTPResponseClient<IEnumerable<ProductVM>>> GetAllProductByPageAsync(int pageIndex, int pageSize)
    {
        var response = new HTTPResponseClient<IEnumerable<ProductVM>>();
        try
        {
            string cacheKey = $"PagedProducts_{pageIndex}_{pageSize}";

            // Kiểm tra cache trước
            var cachedProducts = await _cacheService.GetAsync<IEnumerable<ProductVM>>(cacheKey);
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
                .OrderByDescending(p => p.CreatedAt) // Sắp xếp theo ngày tạo mới nhất
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

            var productVMs = products.Select(p => new ProductVM
            {
                ProductId = p.ProductId,
                CategoryId = p.CategoryId,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Quantity = p.Quantity,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TotalSold = p.TotalSold,
                IsDeleted = p.IsDeleted,
                SellerId = p.SellerId
            }).ToList();

            // Lưu vào cache với thời gian ngắn hơn vì dữ liệu phân trang thay đổi thường xuyên
            await _cacheService.SetAsync(cacheKey, productVMs, TimeSpan.FromMinutes(15));

            response.Data = productVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách sản phẩm theo phân trang thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by page: PageIndex={PageIndex}, PageSize={PageSize}", pageIndex, pageSize);
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách sản phẩm theo phân trang: {ex.Message}";
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

                // 🔥 KIỂM TRA: Nếu không đủ số lượng
                if (product.Quantity < item.Quantity)
                {
                    updateResult.Success = false;
                    updateResult.ErrorMessage = $"Insufficient stock for product {item.ProductId}. Available: {product.Quantity}, Required: {item.Quantity}";
                    break;
                }

                // Trừ số lượng sản phẩm
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
                // Xóa cache
                await _cacheService.DeleteByPatternAsync("AllProducts");
                await _cacheService.DeleteByPatternAsync($"ProductsByCategory_*");
                await _cacheService.DeleteByPatternAsync($"ProductsBySeller_*");
                // Xóa cache phân trang
                await _cacheService.DeleteByPatternAsync("PagedProducts_*");
            }
            else
            {
                // 🔥 ROLLBACK: Khi có lỗi
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

                // Thêm lại số lượng đã bán
                product.Quantity += item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;

                _unitOfWork._prodRepository.Update(product);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllProducts");
            await _cacheService.DeleteByPatternAsync($"ProductsByCategory_*");
            await _cacheService.DeleteByPatternAsync($"ProductsBySeller_*");
            // Xóa cache phân trang
            await _cacheService.DeleteByPatternAsync("PagedProducts_*");

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