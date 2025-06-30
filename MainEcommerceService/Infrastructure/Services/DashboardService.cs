using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MainEcommerceService.Infrastructure.Services
{
    public interface IDashboardService
    {
        Task<HTTPResponseClient<AdminDashboardVM>> GetAdminDashboardAsync();
        Task<HTTPResponseClient<SellerDashboardVM>> GetSellerDashboardAsync(int sellerId);
        Task<HTTPResponseClient<DashboardStatsVM>> GetDashboardStatsAsync();
    }
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly RedisHelper _cacheService;

        public DashboardService(
            IUnitOfWork unitOfWork,
            HttpClient httpClient,
            IConfiguration configuration,
            RedisHelper cacheService)
        {
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
            _configuration = configuration;
            _cacheService = cacheService;
        }

        public async Task<HTTPResponseClient<AdminDashboardVM>> GetAdminDashboardAsync()
        {
            var response = new HTTPResponseClient<AdminDashboardVM>();
            try
            {
                const string cacheKey = "AdminDashboard";
                var cachedData = await _cacheService.GetAsync<AdminDashboardVM>(cacheKey);
                if (cachedData != null)
                {
                    response.Data = cachedData;
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Message = "Lấy dữ liệu dashboard admin từ cache thành công";
                    response.DateTime = DateTime.Now;
                    return response;
                }

                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var lastMonth = startOfMonth.AddMonths(-1);

                var currentStats = await GetStatsForPeriod(startOfMonth, now);
                var previousStats = await GetStatsForPeriod(lastMonth, startOfMonth.AddDays(-1));

                var productsData = await GetProductsDataFromApi();
                var categoriesData = await GetCategoriesDataFromApi();

                var dashboard = new AdminDashboardVM
                {
                    // Overview Stats
                    TotalUsers = await _unitOfWork._userRepository.Query()
                        .CountAsync(u => u.IsDeleted != true),
                    TotalSellers = await _unitOfWork._sellerProfileRepository.Query()
                        .CountAsync(s => s.IsDeleted != true),
                    TotalProducts = productsData?.Count ?? 0,
                    TotalOrders = await _unitOfWork._orderRepository.Query()
                        .CountAsync(),
                    TotalRevenue = await _unitOfWork._orderRepository.Query()
                        .Include(o => o.OrderStatus)
                        .Where(o => o.OrderStatus.StatusName == "Completed")
                        .SumAsync(o => o.TotalAmount),

                    // Growth calculations
                    UsersGrowthPercentage = CalculateGrowthPercentage(
                        previousStats.UsersCount, currentStats.UsersCount),
                    SellersGrowthPercentage = CalculateGrowthPercentage(
                        previousStats.SellersCount, currentStats.SellersCount),
                    ProductsGrowthPercentage = await CalculateProductsGrowthPercentage(startOfMonth),
                    OrdersGrowthPercentage = CalculateGrowthPercentage(
                        previousStats.OrdersCount, currentStats.OrdersCount),
                    RevenueGrowthPercentage = CalculateGrowthPercentage(
                        previousStats.Revenue, currentStats.Revenue),

                    // Recent Activities
                    RecentOrders = await GetRecentOrdersAsync(10),
                    NewUsers = await GetNewUsersAsync(10),
                    PendingSellers = await GetPendingSellersAsync(10),

                    // Analytics
                    MonthlyStats = await GetMonthlyStatsAsync(12),
                    TopCategories = await GetTopCategoriesFromApiAsync(5),
                    TopProducts = await GetTopProductsFromApiAsync(10),
                    TopSellers = await GetTopSellersAsync(10),

                    // System Health
                    LowStockProductsCount = await GetLowStockProductsCountFromApi(),
                    PendingOrdersCount = await _unitOfWork._orderRepository.Query()
                        .Include(o => o.OrderStatus)
                        .CountAsync(o => o.OrderStatus.StatusName == "Pending"),
                    VerificationPendingCount = await _unitOfWork._sellerProfileRepository.Query()
                        .CountAsync(s => s.IsDeleted != true && s.IsVerified != true)
                };

                await _cacheService.SetAsync(cacheKey, dashboard, TimeSpan.FromDays(1));

                response.Data = dashboard;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy dữ liệu dashboard admin thành công";
                response.DateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.StatusCode = 500;
                response.Message = $"Lỗi khi lấy dữ liệu dashboard admin: {ex.Message}";
                response.DateTime = DateTime.Now;
            }
            return response;
        }

        public async Task<HTTPResponseClient<SellerDashboardVM>> GetSellerDashboardAsync(int sellerId)
        {
            var response = new HTTPResponseClient<SellerDashboardVM>();
            try
            {
                var cacheKey = $"SellerDashboard_{sellerId}";
                var cachedData = await _cacheService.GetAsync<SellerDashboardVM>(cacheKey);
                if (cachedData != null)
                {
                    response.Data = cachedData;
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Message = "Lấy dữ liệu dashboard seller từ cache thành công";
                    response.DateTime = DateTime.Now;
                    return response;
                }

                var seller = await _unitOfWork._sellerProfileRepository.Query()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SellerId == sellerId && s.IsDeleted != true);

                if (seller == null)
                {
                    response.Success = false;
                    response.StatusCode = 404;
                    response.Message = "Không tìm thấy seller";
                    return response;
                }

                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var lastMonth = startOfMonth.AddMonths(-1);

                var sellerProducts = await GetSellerProductsFromApi(sellerId);
                var productIds = sellerProducts?.Select(p => p.ProductId).ToList() ?? new List<int>();

                var sellerOrders = await _unitOfWork._orderItemRepository.Query()
                    .Include(oi => oi.Order)
                    .Where(oi => productIds.Contains(oi.ProductId))
                    .Select(oi => oi.Order)
                    .Distinct()
                    .ToListAsync();

                var totalRevenue = await _unitOfWork._orderItemRepository.Query()
                    .Include(oi => oi.Order)
                    .ThenInclude(o => o.OrderStatus)
                    .Where(oi => productIds.Contains(oi.ProductId) && oi.Order.OrderStatus.StatusName == "Completed")
                    .SumAsync(oi => oi.UnitPrice * oi.Quantity);

                var currentMonthOrders = sellerOrders.Where(o => o.OrderDate >= startOfMonth).ToList();
                var previousMonthOrders = sellerOrders.Where(o => o.OrderDate >= lastMonth && o.OrderDate < startOfMonth).ToList();

                var currentMonthProducts = sellerProducts?.Count(p => p.CreatedAt >= startOfMonth) ?? 0;
                var previousMonthProducts = sellerProducts?.Count(p => p.CreatedAt >= lastMonth && p.CreatedAt < startOfMonth) ?? 0;

                var dashboard = new SellerDashboardVM
                {
                    SellerId = sellerId,
                    StoreName = seller.StoreName,
                    TotalProducts = sellerProducts?.Count ?? 0,
                    TotalOrders = sellerOrders.Count,
                    TotalRevenue = totalRevenue,
                    AverageOrderValue = sellerOrders.Count > 0 ? totalRevenue / sellerOrders.Count : 0,

                    // Growth calculations
                    ProductsGrowthPercentage = CalculateGrowthPercentage(previousMonthProducts, currentMonthProducts),
                    OrdersGrowthPercentage = CalculateGrowthPercentage(previousMonthOrders.Count, currentMonthOrders.Count),

                    // Recent activities
                    RecentOrders = await GetSellerRecentOrdersAsync(sellerId, 10),
                    TopProducts = await GetSellerTopProductsFromApiAsync(sellerId, 5),
                    MonthlyStats = await GetSellerMonthlyStatsAsync(sellerId, 12),

                    // Alerts
                    LowStockProductsCount = await GetSellerLowStockProductsCountFromApi(sellerId),
                    PendingOrdersCount = currentMonthOrders.Count(o => o.OrderStatus.StatusName == "Pending"),
                    IsVerified = seller.IsVerified ?? false,
                };

                await _cacheService.SetAsync(cacheKey, dashboard, TimeSpan.FromDays(1));

                response.Data = dashboard;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy dữ liệu dashboard seller thành công";
                response.DateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.StatusCode = 500;
                response.Message = $"Lỗi khi lấy dữ liệu dashboard seller: {ex.Message}";
                response.DateTime = DateTime.Now;
            }
            return response;
        }

        public async Task<HTTPResponseClient<DashboardStatsVM>> GetDashboardStatsAsync()
        {
            var response = new HTTPResponseClient<DashboardStatsVM>();
            try
            {
                const string cacheKey = "DashboardStats";
                var cachedData = await _cacheService.GetAsync<DashboardStatsVM>(cacheKey);
                if (cachedData != null)
                {
                    response.Data = cachedData;
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Message = "Lấy dữ liệu thống kê từ cache thành công";
                    response.DateTime = DateTime.Now;
                    return response;
                }

                var productsData = await GetProductsDataFromApi();

                var stats = new DashboardStatsVM
                {
                    TotalUsers = await _unitOfWork._userRepository.Query()
                        .CountAsync(u => u.IsDeleted != true),
                    TotalOrders = await _unitOfWork._orderRepository.Query()
                        .CountAsync(),
                    TotalProducts = productsData?.Count ?? 0,
                    TotalRevenue = await _unitOfWork._orderRepository.Query()
                    .Include(o => o.OrderStatus)
                        .Where(o => o.OrderStatus.StatusName == "Completed")
                        .SumAsync(o => o.TotalAmount),
                    PendingOrders = await _unitOfWork._orderRepository.Query()
                        .Include(o => o.OrderStatus)
                        .CountAsync(o => o.OrderStatus.StatusName == "Pending"),
                    LowStockProducts = await GetLowStockProductsCountFromApi(),
                    VerificationPending = await _unitOfWork._sellerProfileRepository.Query()
                        .CountAsync(s => s.IsDeleted != true && s.IsVerified != true),
                    TotalSellers = await _unitOfWork._sellerProfileRepository.Query()
                        .CountAsync(s => s.IsDeleted != true)
                };

                await _cacheService.SetAsync(cacheKey, stats, TimeSpan.FromDays(1));

                response.Data = stats;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy dữ liệu thống kê thành công";
                response.DateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.StatusCode = 500;
                response.Message = $"Lỗi khi lấy dữ liệu thống kê: {ex.Message}";
                response.DateTime = DateTime.Now;
            }
            return response;
        }

        #region Private Helper Methods

        private async Task<List<ProductApiModel>> GetProductsDataFromApi()
        {
            var response = await _httpClient.GetAsync("http://localhost:5282/product/api/Product/GetAllProducts");
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiResponse = JsonSerializer.Deserialize<HTTPResponseClient<IEnumerable<ProductApiModel>>>(jsonString, options);
                return apiResponse?.Data?.ToList() ?? new List<ProductApiModel>();
            }
            return new List<ProductApiModel>();
        }

        private async Task<List<CategoryApiModel>> GetCategoriesDataFromApi()
        {
            var response = await _httpClient.GetAsync("http://localhost:5282/product/api/Category/GetAllCategories");
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiResponse = JsonSerializer.Deserialize<HTTPResponseClient<IEnumerable<CategoryApiModel>>>(jsonString, options);
                return apiResponse?.Data?.ToList() ?? new List<CategoryApiModel>();
            }
            return new List<CategoryApiModel>();
        }

        private async Task<List<ProductApiModel>> GetSellerProductsFromApi(int sellerId)
        {
            var response = await _httpClient.GetAsync($"http://localhost:5282/product/api/Product/GetProductsBySeller/{sellerId}");
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiResponse = JsonSerializer.Deserialize<HTTPResponseClient<IEnumerable<ProductApiModel>>>(jsonString, options);
                return apiResponse?.Data?.ToList() ?? new List<ProductApiModel>();
            }
            return new List<ProductApiModel>();
        }

        private async Task<int> GetLowStockProductsCountFromApi()
        {
            var products = await GetProductsDataFromApi();
            return products.Count(p => p.Quantity <= 10);
        }

        private async Task<int> GetSellerLowStockProductsCountFromApi(int sellerId)
        {
            var products = await GetSellerProductsFromApi(sellerId);
            return products.Count(p => p.Quantity <= 10);
        }

        private async Task<decimal> CalculateProductsGrowthPercentage(DateTime startOfMonth)
        {
            var products = await GetProductsDataFromApi();
            var currentMonthProducts = products.Count(p => p.CreatedAt >= startOfMonth);
            var previousMonthProducts = products.Count(p => p.CreatedAt < startOfMonth && p.CreatedAt >= startOfMonth.AddMonths(-1));
            return CalculateGrowthPercentage(previousMonthProducts, currentMonthProducts);
        }

        private decimal CalculateGrowthPercentage(decimal previous, decimal current)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return Math.Round(((current - previous) / previous) * 100, 2);
        }

        private async Task<(int UsersCount, int SellersCount, int ProductsCount, int OrdersCount, decimal Revenue)>
            GetStatsForPeriod(DateTime startDate, DateTime endDate)
        {
            var users = await _unitOfWork._userRepository.Query()
                .CountAsync(u => u.IsDeleted != true && u.CreatedAt >= startDate && u.CreatedAt <= endDate);
            var sellers = await _unitOfWork._sellerProfileRepository.Query()
                .CountAsync(s => s.IsDeleted != true && s.CreatedAt >= startDate && s.CreatedAt <= endDate);
            var orders = await _unitOfWork._orderRepository.Query()
                .CountAsync(o => o.OrderDate >= startDate && o.OrderDate <= endDate);
            var revenue = await _unitOfWork._orderRepository.Query()
                .Include(o => o.OrderStatus)
                .Where(o => o.OrderStatus.StatusName == "Completed" && o.OrderDate >= startDate && o.OrderDate <= endDate)
                .SumAsync(o => o.TotalAmount);

            var products = await GetProductsDataFromApi();
            var productsCount = products.Count(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);

            return (users, sellers, productsCount, orders, revenue);
        }

        private async Task<List<DashboardOrderVM>> GetRecentOrdersAsync(int count)
        {
            return await _unitOfWork._orderRepository.Query()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .Include(o => o.OrderStatus)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .Select(o => new DashboardOrderVM
                {
                    OrderId = o.OrderId,
                    CustomerName = o.User.FirstName + " " + o.User.LastName,
                    CustomerEmail = o.User.Email,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.OrderStatus.StatusName,
                    ItemsCount = o.OrderItems.Count
                })
                .ToListAsync();
        }

        private async Task<List<DashboardUserVM>> GetNewUsersAsync(int count)
        {
            return await _unitOfWork._userRepository.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.IsDeleted != true)
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .Select(u => new DashboardUserVM
                {
                    UserId = u.UserId,
                    FullName = u.FirstName + " " + u.LastName,
                    Email = u.Email,
                    Role = u.UserRoles.FirstOrDefault() != null ? u.UserRoles.FirstOrDefault().Role.RoleName : "User",
                    JoinedDate = u.CreatedAt ?? DateTime.Now,
                    IsActive = u.IsDeleted != true
                })
                .ToListAsync();
        }

        private async Task<List<DashboardSellerVM>> GetPendingSellersAsync(int count)
        {
            var pendingSellers = await _unitOfWork._sellerProfileRepository.Query()
                .Include(s => s.User)
                .Where(s => s.IsDeleted != true && s.IsVerified != true)
                .OrderByDescending(s => s.CreatedAt)
                .Take(count)
                .ToListAsync();

            var result = new List<DashboardSellerVM>();
            foreach (var seller in pendingSellers)
            {
                var productsCount = await GetSellerProductsFromApi(seller.SellerId);
                result.Add(new DashboardSellerVM
                {
                    SellerId = seller.SellerId,
                    StoreName = seller.StoreName,
                    OwnerName = seller.User.FirstName + " " + seller.User.LastName,
                    Email = seller.User.Email,
                    ApplicationDate = seller.CreatedAt ?? DateTime.Now,
                    IsVerified = seller.IsVerified ?? false,
                    ProductsCount = productsCount?.Count ?? 0
                });
            }
            return result;
        }

        private async Task<List<MonthlyStatsVM>> GetMonthlyStatsAsync(int months)
        {
            var result = new List<MonthlyStatsVM>();
            var currentDate = DateTime.Now;

            for (int i = months - 1; i >= 0; i--)
            {
                var targetDate = currentDate.AddMonths(-i);
                var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var orders = await _unitOfWork._orderRepository.Query()
                .Include(o => o.OrderStatus)
                    .Where(o => o.OrderDate >= startOfMonth && o.OrderDate <= endOfMonth)
                    .ToListAsync();

                var newCustomers = await _unitOfWork._userRepository.Query()
                    .CountAsync(u => u.IsDeleted != true && u.CreatedAt >= startOfMonth && u.CreatedAt <= endOfMonth);

                var productsSold = await _unitOfWork._orderItemRepository.Query()
                    .Include(oi => oi.Order)
                    .Where(oi => oi.Order.OrderDate >= startOfMonth && oi.Order.OrderDate <= endOfMonth)
                    .SumAsync(oi => oi.Quantity);

                result.Add(new MonthlyStatsVM
                {
                    Month = targetDate.Month,
                    Year = targetDate.Year,
                    MonthName = targetDate.ToString("MMMM yyyy"),
                    OrdersCount = orders.Count,
                    Revenue = orders.Where(o => o.OrderStatus.StatusName == "Completed").Sum(o => o.TotalAmount),
                    NewCustomers = newCustomers,
                    ProductsSold = productsSold
                });
            }
            return result;
        }

        private async Task<List<CategoryStatsVM>> GetTopCategoriesFromApiAsync(int count)
        {
            var categories = await GetCategoriesDataFromApi();
            var products = await GetProductsDataFromApi();

            var categoryStats = categories.Select(c => new CategoryStatsVM
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                ProductsCount = products.Count(p => p.CategoryId == c.CategoryId),
                OrdersCount = GetCategoryOrdersCount(c.CategoryId, products),
                Revenue = GetCategoryRevenue(c.CategoryId, products)
            }).OrderByDescending(c => c.Revenue).Take(count).ToList();

            return categoryStats;
        }

        private async Task<List<ProductStatsVM>> GetTopProductsFromApiAsync(int count)
        {
            var products = await GetProductsDataFromApi();
            var categories = await GetCategoriesDataFromApi();
            var productStats = new List<ProductStatsVM>();

            foreach (var product in products.Take(count))
            {
                var category = categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);
                var orderItems = await _unitOfWork._orderItemRepository.Query()
                    .Include(oi => oi.Order)
                    .ThenInclude(oi => oi.OrderStatus)
                    .Where(oi => oi.ProductId == product.ProductId)
                    .ToListAsync();

                var seller = await _unitOfWork._sellerProfileRepository.GetByIdAsync(product.SellerId);

                productStats.Add(new ProductStatsVM
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    CategoryName = category?.CategoryName ?? "Unknown",
                    QuantitySold = orderItems.Sum(oi => oi.Quantity),
                    Revenue = orderItems.Where(oi => oi.Order.OrderStatus.StatusName == "Completed")
                        .Sum(oi => oi.UnitPrice * oi.Quantity),
                    Price = product.Price,
                    Stock = product.Quantity,
                    StoreName = seller?.StoreName ?? "Unknown"
                });
            }
            return productStats.OrderByDescending(p => p.QuantitySold).ToList();
        }

        private async Task<List<ProductStatsVM>> GetSellerTopProductsFromApiAsync(int sellerId, int count)
        {
            var products = await GetSellerProductsFromApi(sellerId);
            var categories = await GetCategoriesDataFromApi();
            var productStats = new List<ProductStatsVM>();

            foreach (var product in products.Take(count))
            {
                var category = categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);
                var orderItems = await _unitOfWork._orderItemRepository.Query()
                    .Include(oi => oi.Order)
                    .ThenInclude(oi => oi.OrderStatus)
                    .Where(oi => oi.ProductId == product.ProductId)
                    .ToListAsync();

                productStats.Add(new ProductStatsVM
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    CategoryName = category?.CategoryName ?? "Unknown",
                    QuantitySold = orderItems.Sum(oi => oi.Quantity),
                    Revenue = orderItems.Where(oi => oi.Order.OrderStatus.StatusName == "Completed")
                        .Sum(oi => oi.UnitPrice * oi.Quantity),
                    Price = product.Price,
                    Stock = product.Quantity
                });
            }
            return productStats.OrderByDescending(p => p.QuantitySold).ToList();
        }

        private async Task<List<SellerStatsVM>> GetTopSellersAsync(int count)
        {
            var sellers = await _unitOfWork._sellerProfileRepository.Query()
                .Include(s => s.User)
                .Where(s => s.IsDeleted != true)
                .ToListAsync();

            var sellerStats = new List<SellerStatsVM>();

            foreach (var seller in sellers.Take(count * 2))
            {
                var sellerProducts = await GetSellerProductsFromApi(seller.SellerId);
                var productIds = sellerProducts?.Select(p => p.ProductId).ToList() ?? new List<int>();

                var orderItems = await _unitOfWork._orderItemRepository.Query()
                    .Include(oi => oi.Order)
                    .ThenInclude(o => o.OrderStatus)
                    .Where(oi => productIds.Contains(oi.ProductId))
                    .ToListAsync();

                var revenue = orderItems.Where(oi => oi.Order.OrderStatus.StatusName == "Completed")
                    .Sum(oi => oi.UnitPrice * oi.Quantity);

                sellerStats.Add(new SellerStatsVM
                {
                    SellerId = seller.SellerId,
                    StoreName = seller.StoreName,
                    OwnerName = seller.User.FirstName + " " + seller.User.LastName,
                    OrdersCount = orderItems.Select(oi => oi.OrderId).Distinct().Count(),
                    Revenue = revenue,
                    IsVerified = seller.IsVerified ?? false,
                    AverageOrderValue = orderItems.Any() ? revenue / orderItems.Select(oi => oi.OrderId).Distinct().Count() : 0
                });
            }
            return sellerStats.OrderByDescending(s => s.Revenue).Take(count).ToList();
        }

        private async Task<List<DashboardOrderVM>> GetSellerRecentOrdersAsync(int sellerId, int count)
        {
            var productIds = (await GetSellerProductsFromApi(sellerId))?.Select(p => p.ProductId).ToList() ?? new List<int>();

            return await _unitOfWork._orderItemRepository.Query()
                .Include(oi => oi.Order)
                .ThenInclude(o => o.User)
                .Include(oi => oi.Order)
                .ThenInclude(o => o.OrderStatus)
                .Where(oi => productIds.Contains(oi.ProductId))
                .Select(oi => oi.Order)
                .Distinct()
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .Select(o => new DashboardOrderVM
                {
                    OrderId = o.OrderId,
                    CustomerName = o.User.FirstName + " " + o.User.LastName,
                    CustomerEmail = o.User.Email,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.OrderStatus.StatusName,
                    ItemsCount = o.OrderItems.Count
                })
                .ToListAsync();
        }

        private async Task<List<MonthlyStatsVM>> GetSellerMonthlyStatsAsync(int sellerId, int months)
        {
            var productIds = (await GetSellerProductsFromApi(sellerId))?.Select(p => p.ProductId).ToList() ?? new List<int>();
            var result = new List<MonthlyStatsVM>();
            var currentDate = DateTime.Now;

            for (int i = months - 1; i >= 0; i--)
            {
                var targetDate = currentDate.AddMonths(-i);
                var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var orderItems = await _unitOfWork._orderItemRepository.Query()
                    .Include(oi => oi.Order)
                    .ThenInclude(o => o.OrderStatus)
                    .Where(oi => productIds.Contains(oi.ProductId) &&
                               oi.Order.OrderDate >= startOfMonth &&
                               oi.Order.OrderDate <= endOfMonth)
                    .ToListAsync();

                result.Add(new MonthlyStatsVM
                {
                    Month = targetDate.Month,
                    Year = targetDate.Year,
                    MonthName = targetDate.ToString("MMMM yyyy"),
                    OrdersCount = orderItems.Select(oi => oi.OrderId).Distinct().Count(),
                    Revenue = orderItems.Where(oi => oi.Order.OrderStatus.StatusName == "Completed")
                        .Sum(oi => oi.UnitPrice * oi.Quantity),
                    ProductsSold = orderItems.Sum(oi => oi.Quantity)
                });
            }
            return result;
        }

        private int GetCategoryOrdersCount(int categoryId, List<ProductApiModel> products)
        {
            var categoryProductIds = products.Where(p => p.CategoryId == categoryId).Select(p => p.ProductId).ToList();
            return _unitOfWork._orderItemRepository.Query().Count(oi => categoryProductIds.Contains(oi.ProductId));
        }

        private decimal GetCategoryRevenue(int categoryId, List<ProductApiModel> products)
        {
            var categoryProductIds = products.Where(p => p.CategoryId == categoryId).Select(p => p.ProductId).ToList();
            return _unitOfWork._orderItemRepository.Query()
                .Include(oi => oi.Order)
                .ThenInclude(o => o.OrderStatus)
                .Where(oi => categoryProductIds.Contains(oi.ProductId) && oi.Order.OrderStatus.StatusName == "Completed")
                .Sum(oi => oi.UnitPrice * oi.Quantity);
        }

        #endregion
    }
}