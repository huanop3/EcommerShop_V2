using MainEcommerceService.Models.ViewModel;
using MainEcommerceService.Models.dbMainEcommer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using MainEcommerceService.Hubs;
using ProductService.Models.ViewModel;

namespace MainEcommerceService.Infrastructure.Services
{
    public interface IDashboardService
    {
        Task<HTTPResponseClient<AdminDashboardVM>> GetAdminDashboardComplete();
        Task<HTTPResponseClient<SellerDashboardVM>> GetSellerDashboardComplete(int sellerId);
        Task<HTTPResponseClient<DashboardStatsVM>> GetDashboardStats(string userRole, int? sellerId = null);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly RedisHelper _cacheService;
        private readonly HttpClient _httpClient;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IUnitOfWork unitOfWork,
            RedisHelper cacheService,
            HttpClient httpClient,
            IHubContext<NotificationHub> hubContext,
            ILogger<DashboardService> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _httpClient = httpClient;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tất cả dữ liệu dashboard cho Admin - CHỈ 1 API CALL
        /// </summary>
        public async Task<HTTPResponseClient<AdminDashboardVM>> GetAdminDashboardComplete()
        {
            var response = new HTTPResponseClient<AdminDashboardVM>();
            try
            {
                _logger.LogInformation("🎯 Getting Admin Dashboard Complete");

                string cacheKey = "AdminDashboardComplete";
                var cachedData = await _cacheService.GetAsync<AdminDashboardVM>(cacheKey);
                if (cachedData != null)
                {
                    response.Data = cachedData;
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Message = "Lấy dashboard từ cache thành công";
                    response.DateTime = DateTime.Now;
                    return response;
                }

                var dashboard = new AdminDashboardVM();

                // ✅ Lấy basic stats từ database
                var totalUsers = await _unitOfWork._userRepository.CountAsync(u => u.IsDeleted != true);
                var totalOrders = await _unitOfWork._orderRepository.CountAsync(o => o.IsDeleted != true);
                var totalRevenue = await _unitOfWork._orderRepository.Query()
                    .Where(o => o.IsDeleted != true)
                    .SumAsync(o => o.TotalAmount);

                var totalSellers = await _unitOfWork._sellerProfileRepository.CountAsync(s => s.IsDeleted != true);
                var pendingOrders = await _unitOfWork._orderRepository.Query()
                    .Include(o => o.OrderStatus)
                    .CountAsync(o => o.IsDeleted != true && o.OrderStatus.StatusName == "Pending");

                // ✅ Lấy products từ Product service
                var allProducts = await GetAllProductsData();
                var totalProducts = allProducts.Count(p => p.IsDeleted != true);
                var lowStockProducts = allProducts.Count(p => p.IsDeleted != true && p.Quantity <= 5);

                // Set basic stats
                dashboard.TotalUsers = totalUsers;
                dashboard.TotalSellers = totalSellers;
                dashboard.TotalOrders = totalOrders;
                dashboard.TotalRevenue = totalRevenue;
                dashboard.PendingOrdersCount = pendingOrders;
                dashboard.TotalProducts = totalProducts; // ✅ Thêm dữ liệu thực
                dashboard.LowStockProductsCount = lowStockProducts; // ✅ Thêm dữ liệu thực

                // ✅ Lấy recent orders với đầy đủ thông tin
                var recentOrders = await _unitOfWork._orderRepository.Query()
                    .Where(o => o.IsDeleted != true)
                    .Include(o => o.User)
                    .Include(o => o.OrderStatus)
                    .Include(o => o.OrderItems.Where(oi => oi.IsDeleted != true))
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new DashboardOrderVM
                    {
                        OrderId = o.OrderId,
                        CustomerName = $"{o.User.FirstName} {o.User.LastName}".Trim(),
                        CustomerEmail = o.User.Email ?? "",
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        Status = o.OrderStatus.StatusName ?? "",
                        ItemsCount = o.OrderItems.Count(oi => oi.IsDeleted != true)
                    })
                    .ToListAsync();

                dashboard.RecentOrders = recentOrders;

                // ✅ Lấy new users
                var newUsers = await _unitOfWork._userRepository.Query()
                    .Where(u => u.IsDeleted != true && u.CreatedAt >= DateTime.Now.AddDays(-7))
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(10)
                    .Select(u => new DashboardUserVM
                    {
                        UserId = u.UserId,
                        FullName = $"{u.FirstName} {u.LastName}".Trim(),
                        Email = u.Email ?? "",
                        JoinedDate = u.CreatedAt ?? DateTime.Now,
                        IsActive = u.IsActive == true
                    })
                    .ToListAsync();

                dashboard.NewUsers = newUsers;

                // ✅ Lấy pending sellers
                var pendingSellers = await _unitOfWork._sellerProfileRepository.Query()
                    .Where(s => s.IsDeleted != true && s.IsVerified != true)
                    .Include(s => s.User)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(10)
                    .Select(s => new DashboardSellerVM
                    {
                        SellerId = s.SellerId,
                        StoreName = s.StoreName ?? "",
                        OwnerName = $"{s.User.FirstName} {s.User.LastName}".Trim(),
                        Email = s.User.Email ?? "",
                        ApplicationDate = s.CreatedAt ?? DateTime.Now,
                        IsVerified = s.IsVerified == true
                    })
                    .ToListAsync();

                dashboard.PendingSellers = pendingSellers;
                dashboard.VerificationPendingCount = pendingSellers.Count;

                // ✅ Lấy orders với OrderItems để tính Top Products
                var ordersWithItems = await _unitOfWork._orderRepository.Query()
                    .Where(o => o.IsDeleted != true)
                    .Include(o => o.OrderItems.Where(oi => oi.IsDeleted != true))
                    .Include(o => o.User)
                    .ToListAsync();

                // ✅ Top products - Sản phẩm bán chạy nhất
                var topProductStats = ordersWithItems
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        TotalSold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.TotalPrice),
                        OrdersCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(10)
                    .ToList();

                dashboard.TopProducts = topProductStats.Select(ps =>
                {
                    var product = allProducts.FirstOrDefault(p => p.ProductId == ps.ProductId);
                    return new ProductStatsVM
                    {
                        ProductId = ps.ProductId,
                        ProductName = product?.ProductName ?? "Unknown Product",
                        CategoryName = product?.CategoryName ?? "Unknown Category",
                        QuantitySold = ps.TotalSold,
                        Revenue = ps.TotalRevenue,
                        Price = product?.Price ?? 0,
                        Stock = product?.Quantity ?? 0,
                        StoreName = product?.SellerStoreName ?? "Unknown Store"
                    };
                }).ToList();

                // ✅ Top sellers
                var allSellers = await _unitOfWork._sellerProfileRepository.Query()
                    .Where(s => s.IsDeleted != true)
                    .Include(s => s.User)
                    .ToListAsync();

                var topSellerStats = ordersWithItems
                    .SelectMany(o => o.OrderItems)
                    .Join(allProducts, 
                        oi => oi.ProductId, 
                        p => p.ProductId, 
                        (oi, p) => new { OrderItem = oi, Product = p })
                    .Where(x => x.Product.SellerId.HasValue)
                    .GroupBy(x => x.Product.SellerId.Value)
                    .Select(g => new
                    {
                        SellerId = g.Key,
                        TotalRevenue = g.Sum(x => x.OrderItem.TotalPrice),
                        OrdersCount = g.Select(x => x.OrderItem.OrderId).Distinct().Count(),
                        ProductsSold = g.Sum(x => x.OrderItem.Quantity)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(10)
                    .ToList();

                dashboard.TopSellers = topSellerStats.Select(ss =>
                {
                    var seller = allSellers.FirstOrDefault(s => s.SellerId == ss.SellerId);
                    return new SellerStatsVM
                    {
                        SellerId = ss.SellerId,
                        StoreName = seller?.StoreName ?? "Unknown Store",
                        OwnerName = seller != null ? $"{seller.User?.FirstName} {seller.User?.LastName}".Trim() : "Unknown Owner",
                        Revenue = ss.TotalRevenue,
                        OrdersCount = ss.OrdersCount,
                        AverageOrderValue = ss.OrdersCount > 0 ? ss.TotalRevenue / ss.OrdersCount : 0,
                        IsVerified = seller?.IsVerified == true
                    };
                }).ToList();

                // ✅ Top categories
                var topCategoryStats = ordersWithItems
                    .SelectMany(o => o.OrderItems)
                    .Join(allProducts, 
                        oi => oi.ProductId, 
                        p => p.ProductId, 
                        (oi, p) => new { OrderItem = oi, Product = p })
                    .Where(x => x.Product.CategoryId.HasValue)
                    .GroupBy(x => x.Product.CategoryId.Value)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        CategoryName = g.First().Product.CategoryName,
                        TotalRevenue = g.Sum(x => x.OrderItem.TotalPrice),
                        OrdersCount = g.Select(x => x.OrderItem.OrderId).Distinct().Count(),
                        ProductsSold = g.Sum(x => x.OrderItem.Quantity)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(10)
                    .ToList();

                dashboard.TopCategories = topCategoryStats.Select(cs => new CategoryStatsVM
                {
                    CategoryId = cs.CategoryId,
                    CategoryName = cs.CategoryName ?? $"Category {cs.CategoryId}",
                    Revenue = cs.TotalRevenue,
                    OrdersCount = cs.OrdersCount,
                    ProductsCount = allProducts.Count(p => p.CategoryId == cs.CategoryId)
                }).ToList();

                // ✅ Monthly stats với dữ liệu thực
                var monthlyStats = new List<MonthlyStatsVM>();
                for (int i = 11; i >= 0; i--)
                {
                    var targetDate = DateTime.Now.AddMonths(-i);
                    var monthOrders = ordersWithItems
                        .Where(o => o.OrderDate.Month == targetDate.Month && 
                                   o.OrderDate.Year == targetDate.Year)
                        .ToList();

                    monthlyStats.Add(new MonthlyStatsVM
                    {
                        MonthName = targetDate.ToString("MMM yyyy"),
                        Month = targetDate.Month,
                        Year = targetDate.Year,
                        OrdersCount = monthOrders.Count,
                        Revenue = monthOrders.Sum(o => o.TotalAmount),
                        NewCustomers = monthOrders.Select(o => o.UserId).Distinct().Count(),
                        ProductsSold = monthOrders.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity)
                    });
                }

                dashboard.MonthlyStats = monthlyStats;

                // ✅ Growth percentages - So sánh với tháng trước
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var lastMonth = DateTime.Now.AddMonths(-1);

                var currentMonthOrders = ordersWithItems
                    .Where(o => o.OrderDate.Month == currentMonth && o.OrderDate.Year == currentYear)
                    .ToList();

                var lastMonthOrders = ordersWithItems
                    .Where(o => o.OrderDate.Month == lastMonth.Month && o.OrderDate.Year == lastMonth.Year)
                    .ToList();

                var allUsers = await _unitOfWork._userRepository.Query()
                    .Where(u => u.IsDeleted != true)
                    .ToListAsync();

                // Orders growth
                dashboard.OrdersGrowthPercentage = CalculateGrowthPercentage(currentMonthOrders.Count, lastMonthOrders.Count);

                // Revenue growth
                var currentMonthRevenue = currentMonthOrders.Sum(o => o.TotalAmount);
                var lastMonthRevenue = lastMonthOrders.Sum(o => o.TotalAmount);
                dashboard.RevenueGrowthPercentage = CalculateGrowthPercentage(currentMonthRevenue, lastMonthRevenue);

                // Users growth
                var currentMonthUsers = allUsers.Count(u => u.CreatedAt?.Month == currentMonth && u.CreatedAt?.Year == currentYear);
                var lastMonthUsers = allUsers.Count(u => u.CreatedAt?.Month == lastMonth.Month && u.CreatedAt?.Year == lastMonth.Year);
                dashboard.UsersGrowthPercentage = CalculateGrowthPercentage(currentMonthUsers, lastMonthUsers);

                // Sellers growth
                var currentMonthSellers = allSellers.Count(s => s.CreatedAt?.Month == currentMonth && s.CreatedAt?.Year == currentYear);
                var lastMonthSellers = allSellers.Count(s => s.CreatedAt?.Month == lastMonth.Month && s.CreatedAt?.Year == lastMonth.Year);
                dashboard.SellersGrowthPercentage = CalculateGrowthPercentage(currentMonthSellers, lastMonthSellers);

                // Products growth
                var currentMonthProducts = allProducts.Count(p => p.CreatedAt?.Month == currentMonth && p.CreatedAt?.Year == currentYear);
                var lastMonthProducts = allProducts.Count(p => p.CreatedAt?.Month == lastMonth.Month && p.CreatedAt?.Year == lastMonth.Year);
                dashboard.ProductsGrowthPercentage = CalculateGrowthPercentage(currentMonthProducts, lastMonthProducts);

                // Cache for 10 minutes
                await _cacheService.SetAsync(cacheKey, dashboard, TimeSpan.FromMinutes(10));

                response.Data = dashboard;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy dashboard thành công";
                response.DateTime = DateTime.Now;

                _logger.LogInformation("✅ Admin dashboard loaded successfully");
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.StatusCode = 500;
                response.Message = $"Lỗi khi lấy dashboard: {ex.Message}";
                response.DateTime = DateTime.Now;
                _logger.LogError(ex, "❌ Error getting admin dashboard complete");
            }
            return response;
        }

        /// <summary>
        /// Lấy tất cả dữ liệu dashboard cho Seller - CHỈ 1 API CALL
        /// </summary>
        public async Task<HTTPResponseClient<SellerDashboardVM>> GetSellerDashboardComplete(int sellerId)
        {
            var response = new HTTPResponseClient<SellerDashboardVM>();
            try
            {
                _logger.LogInformation("🎯 Getting Seller Dashboard Complete for {SellerId}", sellerId);

                string cacheKey = $"SellerDashboardComplete_{sellerId}";
                var cachedData = await _cacheService.GetAsync<SellerDashboardVM>(cacheKey);
                if (cachedData != null)
                {
                    response.Data = cachedData;
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Message = "Lấy seller dashboard từ cache thành công";
                    response.DateTime = DateTime.Now;
                    return response;
                }

                // Get seller info
                var seller = await _unitOfWork._sellerProfileRepository.Query()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SellerId == sellerId && s.IsDeleted != true);

                if (seller == null)
                {
                    response.Success = false;
                    response.StatusCode = 404;
                    response.Message = "Seller không tồn tại";
                    return response;
                }

                var dashboard = new SellerDashboardVM
                {
                    SellerId = sellerId,
                    StoreName = seller.StoreName ?? "",
                    IsVerified = seller.IsVerified == true
                };

                // ✅ Lấy products của seller từ Product service
                var sellerProducts = await GetProductsBySellerData(sellerId);
                dashboard.TotalProducts = sellerProducts.Count(p => p.IsDeleted != true);
                dashboard.LowStockProductsCount = sellerProducts.Count(p => p.IsDeleted != true && p.Quantity <= 5);

                // ✅ Lấy orders có chứa products của seller
                var sellerOrderItems = await _unitOfWork._orderItemRepository.Query()
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Order.User)
                    .Include(oi => oi.Order.OrderStatus)
                    .Where(oi => oi.IsDeleted != true && 
                                oi.Order.IsDeleted != true &&
                                sellerProducts.Select(p => p.ProductId).Contains(oi.ProductId))
                    .ToListAsync();

                // ✅ Tính toán thống kê
                dashboard.TotalOrders = sellerOrderItems.Select(oi => oi.OrderId).Distinct().Count();
                dashboard.TotalRevenue = sellerOrderItems.Sum(oi => oi.TotalPrice);
                dashboard.AverageOrderValue = dashboard.TotalOrders > 0 ? 
                    dashboard.TotalRevenue / dashboard.TotalOrders : 0;

                // ✅ Pending orders count
                dashboard.PendingOrdersCount = sellerOrderItems
                    .Where(oi => oi.Order.OrderStatus.StatusName == "Pending")
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .Count();

                // ✅ Recent orders - Lấy 10 orders gần nhất
                var recentOrderIds = sellerOrderItems
                    .OrderByDescending(oi => oi.Order.CreatedAt)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .Take(10)
                    .ToList();

                dashboard.RecentOrders = await _unitOfWork._orderRepository.Query()
                    .Where(o => recentOrderIds.Contains(o.OrderId))
                    .Include(o => o.User)
                    .Include(o => o.OrderStatus)
                    .Include(o => o.OrderItems.Where(oi => oi.IsDeleted != true))
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new DashboardOrderVM
                    {
                        OrderId = o.OrderId,
                        CustomerName = $"{o.User.FirstName} {o.User.LastName}".Trim(),
                        CustomerEmail = o.User.Email ?? "",
                        OrderDate = o.OrderDate,
                        TotalAmount = o.OrderItems
                            .Where(oi => sellerProducts.Select(p => p.ProductId).Contains(oi.ProductId))
                            .Sum(oi => oi.TotalPrice),
                        Status = o.OrderStatus.StatusName ?? "",
                        ItemsCount = o.OrderItems
                            .Count(oi => oi.IsDeleted != true && 
                                       sellerProducts.Select(p => p.ProductId).Contains(oi.ProductId))
                    })
                    .ToListAsync();

                // ✅ Top products - Sản phẩm bán chạy nhất
                var topProductStats = sellerOrderItems
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        TotalSold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.TotalPrice),
                        OrdersCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(10)
                    .ToList();

                dashboard.TopProducts = topProductStats.Select(ps =>
                {
                    var product = sellerProducts.FirstOrDefault(p => p.ProductId == ps.ProductId);
                    return new ProductStatsVM
                    {
                        ProductId = ps.ProductId,
                        ProductName = product?.ProductName ?? "Unknown Product",
                        CategoryName = product?.CategoryName ?? "Unknown Category",
                        QuantitySold = ps.TotalSold,
                        Revenue = ps.TotalRevenue,
                        Price = product?.Price ?? 0,
                        Stock = product?.Quantity ?? 0,
                        StoreName = dashboard.StoreName
                    };
                }).ToList();

                // ✅ Monthly stats - 12 tháng gần nhất
                var monthlyStats = new List<MonthlyStatsVM>();
                for (int i = 11; i >= 0; i--)
                {
                    var targetDate = DateTime.Now.AddMonths(-i);
                    var monthOrderItems = sellerOrderItems
                        .Where(oi => oi.Order.OrderDate.Month == targetDate.Month && 
                                   oi.Order.OrderDate.Year == targetDate.Year)
                        .ToList();

                    monthlyStats.Add(new MonthlyStatsVM
                    {
                        MonthName = targetDate.ToString("MMM yyyy"),
                        Month = targetDate.Month,
                        Year = targetDate.Year,
                        OrdersCount = monthOrderItems.Select(oi => oi.OrderId).Distinct().Count(),
                        Revenue = monthOrderItems.Sum(oi => oi.TotalPrice),
                        NewCustomers = monthOrderItems.Select(oi => oi.Order.UserId).Distinct().Count(),
                        ProductsSold = monthOrderItems.Sum(oi => oi.Quantity)
                    });
                }

                dashboard.MonthlyStats = monthlyStats;

                // ✅ Growth percentages - So sánh với tháng trước
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var lastMonth = DateTime.Now.AddMonths(-1);

                var currentMonthItems = sellerOrderItems
                    .Where(oi => oi.Order.OrderDate.Month == currentMonth && 
                                oi.Order.OrderDate.Year == currentYear)
                    .ToList();

                var lastMonthItems = sellerOrderItems
                    .Where(oi => oi.Order.OrderDate.Month == lastMonth.Month && 
                                oi.Order.OrderDate.Year == lastMonth.Year)
                    .ToList();

                // Orders growth
                var currentMonthOrders = currentMonthItems.Select(oi => oi.OrderId).Distinct().Count();
                var lastMonthOrders = lastMonthItems.Select(oi => oi.OrderId).Distinct().Count();
                dashboard.OrdersGrowthPercentage = CalculateGrowthPercentage(currentMonthOrders, lastMonthOrders);

                // Revenue growth
                var currentMonthRevenue = currentMonthItems.Sum(oi => oi.TotalPrice);
                var lastMonthRevenue = lastMonthItems.Sum(oi => oi.TotalPrice);
                dashboard.RevenueGrowthPercentage = CalculateGrowthPercentage(currentMonthRevenue, lastMonthRevenue);

                // Products growth
                var currentMonthProducts = sellerProducts.Count(p => p.CreatedAt?.Month == currentMonth && 
                                                                    p.CreatedAt?.Year == currentYear);
                var lastMonthProducts = sellerProducts.Count(p => p.CreatedAt?.Month == lastMonth.Month && 
                                                                 p.CreatedAt?.Year == lastMonth.Year);
                dashboard.ProductsGrowthPercentage = CalculateGrowthPercentage(currentMonthProducts, lastMonthProducts);

                // Cache for 10 minutes
                await _cacheService.SetAsync(cacheKey, dashboard, TimeSpan.FromMinutes(10));

                response.Data = dashboard;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy seller dashboard thành công";
                response.DateTime = DateTime.Now;

                _logger.LogInformation("✅ Seller {SellerId} dashboard loaded successfully", sellerId);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.StatusCode = 500;
                response.Message = $"Lỗi khi lấy seller dashboard: {ex.Message}";
                response.DateTime = DateTime.Now;
                _logger.LogError(ex, "❌ Error getting seller dashboard complete for {SellerId}", sellerId);
            }
            return response;
        }

        /// <summary>
        /// Lấy thống kê tổng quan cho dashboard - ĐƠN GIẢN
        /// </summary>
        public async Task<HTTPResponseClient<DashboardStatsVM>> GetDashboardStats(string userRole, int? sellerId = null)
        {
            var response = new HTTPResponseClient<DashboardStatsVM>();
            try
            {
                _logger.LogInformation("🎯 Getting Dashboard Stats for {UserRole}", userRole);

                var stats = new DashboardStatsVM();

                if (userRole == "Admin")
                {
                    // Admin stats
                    stats.TotalUsers = await _unitOfWork._userRepository.CountAsync(u => u.IsDeleted != true);
                    stats.TotalOrders = await _unitOfWork._orderRepository.CountAsync(o => o.IsDeleted != true);
                    stats.TotalSellers = await _unitOfWork._sellerProfileRepository.CountAsync(s => s.IsDeleted != true);
                    
                    var totalRevenue = await _unitOfWork._orderRepository.Query()
                        .Where(o => o.IsDeleted != true)
                        .SumAsync(o => o.TotalAmount);
                    stats.TotalRevenue = totalRevenue;

                    var pendingOrders = await _unitOfWork._orderRepository.Query()
                        .Include(o => o.OrderStatus)
                        .CountAsync(o => o.IsDeleted != true && o.OrderStatus.StatusName == "Pending");
                    stats.PendingOrders = pendingOrders;

                    stats.TotalProducts = 0; // Sẽ lấy từ Product service
                    stats.LowStockProducts = 0; // Sẽ lấy từ Product service
                    stats.VerificationPending = await _unitOfWork._sellerProfileRepository.CountAsync(s => s.IsDeleted != true && s.IsVerified != true);
                }
                else if (userRole == "Seller" && sellerId.HasValue)
                {
                    // Seller stats - đơn giản
                    stats.TotalProducts = 0; // Sẽ lấy từ Product service
                    stats.TotalOrders = 0; // Sẽ tính từ OrderItems của seller
                    stats.TotalRevenue = 0; // Sẽ tính từ OrderItems của seller
                    stats.LowStockProducts = 0; // Sẽ lấy từ Product service
                    
                    stats.TotalUsers = 0;
                    stats.TotalSellers = 0;
                    stats.PendingOrders = 0;
                    stats.VerificationPending = 0;
                }

                response.Data = stats;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thống kê thành công";
                response.DateTime = DateTime.Now;

                _logger.LogInformation("✅ Dashboard stats loaded successfully for {UserRole}", userRole);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.StatusCode = 500;
                response.Message = $"Lỗi khi lấy thống kê: {ex.Message}";
                response.DateTime = DateTime.Now;
                _logger.LogError(ex, "❌ Error getting dashboard stats for {UserRole}", userRole);
            }
            return response;
        }

        #region Helper Methods - Data Fetching

        /// <summary>
        /// Lấy tất cả users từ local database
        /// </summary>
        private async Task<List<User>> GetAllUsersData()
        {
            try
            {
                return await _unitOfWork._userRepository.Query()
                    .Where(u => u.IsDeleted != true)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users data");
                return new List<User>();
            }
        }

        /// <summary>
        /// Lấy tất cả products từ Product service - 1 call
        /// </summary>
        private async Task<List<ProductDashboardVM>> GetAllProductsData()
        {
            try
            {
                const string cacheKey = "AllProductsForDashboard";
                var cachedProducts = await _cacheService.GetAsync<List<ProductDashboardVM>>(cacheKey);
                if (cachedProducts != null)
                {
                    return cachedProducts;
                }

                // Call Product Service
                var response = await _httpClient.GetAsync("https://localhost:7252/api/Product/GetAllProducts");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<ProductVM>>>();
                    if (result?.Success == true && result.Data != null)
                    {
                        var products = result.Data.Select(p => new ProductDashboardVM
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            CategoryId = p.CategoryId,
                            Price = p.Price,
                            Quantity = p.Quantity,
                            SellerId = p.SellerId,
                            CreatedAt = p.CreatedAt,
                            IsDeleted = p.IsDeleted
                        }).ToList();

                        await _cacheService.SetAsync(cacheKey, products, TimeSpan.FromMinutes(30));
                        return products;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products from Product service");
            }
            return new List<ProductDashboardVM>();
        }

        /// <summary>
        /// Lấy products của seller từ Product service
        /// </summary>
        private async Task<List<ProductDashboardVM>> GetProductsBySellerData(int sellerId)
        {
            try
            {
                string cacheKey = $"SellerProductsForDashboard_{sellerId}";
                var cachedProducts = await _cacheService.GetAsync<List<ProductDashboardVM>>(cacheKey);
                if (cachedProducts != null)
                {
                    return cachedProducts;
                }

                var response = await _httpClient.GetAsync($"https://localhost:7252/api/Product/GetProductsBySeller/{sellerId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<ProductVM>>>();
                    if (result?.Success == true && result.Data != null)
                    {
                        var products = result.Data.Select(p => new ProductDashboardVM
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            CategoryId = p.CategoryId,
                            Price = p.Price,
                            Quantity = p.Quantity,
                            SellerId = p.SellerId,
                            CreatedAt = p.CreatedAt,
                            IsDeleted = p.IsDeleted
                        }).ToList();

                        await _cacheService.SetAsync(cacheKey, products, TimeSpan.FromMinutes(30));
                        return products;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller products from Product service");
            }
            return new List<ProductDashboardVM>();
        }

        /// <summary>
        /// Lấy role của user
        /// </summary>
        private string GetUserRole(int userId)
        {
            try
            {
                var userRole = _unitOfWork._userRoleRepository.Query()
                    .Include(ur => ur.Role)
                    .FirstOrDefault(ur => ur.UserId == userId);
                return userRole?.Role?.RoleName ?? "User";
            }
            catch
            {
                return "User";
            }
        }

        #endregion

        #region Helper Methods - Calculations

        /// <summary>
        /// Tính toán growth stats cho Admin
        /// </summary>
        private void CalculateAdminGrowthStats(AdminDashboardVM dashboard, IEnumerable<dynamic> orders, List<User> users, List<SellerProfile> sellers, List<ProductDashboardVM> products, int currentMonth, int currentYear, int lastMonth, int lastMonthYear)
        {
            // Orders growth
            var currentMonthOrders = orders.Count(o => o.Order.OrderDate.Month == currentMonth && o.Order.OrderDate.Year == currentYear);
            var lastMonthOrders = orders.Count(o => o.Order.OrderDate.Month == lastMonth && o.Order.OrderDate.Year == lastMonthYear);
            dashboard.OrdersGrowthPercentage = CalculateGrowthPercentage(currentMonthOrders, lastMonthOrders);

            // Revenue growth
            var currentMonthRevenue = orders.Where(o => o.Order.OrderDate.Month == currentMonth && o.Order.OrderDate.Year == currentYear).Sum(o => o.Order.TotalAmount);
            var lastMonthRevenue = orders.Where(o => o.Order.OrderDate.Month == lastMonth && o.Order.OrderDate.Year == lastMonthYear).Sum(o => o.Order.TotalAmount);
            dashboard.RevenueGrowthPercentage = CalculateGrowthPercentage(currentMonthRevenue, lastMonthRevenue);

            // Users growth
            var currentMonthUsers = users.Count(u => u.CreatedAt?.Month == currentMonth && u.CreatedAt?.Year == currentYear);
            var lastMonthUsers = users.Count(u => u.CreatedAt?.Month == lastMonth && u.CreatedAt?.Year == lastMonthYear);
            dashboard.UsersGrowthPercentage = CalculateGrowthPercentage(currentMonthUsers, lastMonthUsers);

            // Sellers growth
            var currentMonthSellers = sellers.Count(s => s.CreatedAt?.Month == currentMonth && s.CreatedAt?.Year == currentYear);
            var lastMonthSellers = sellers.Count(s => s.CreatedAt?.Month == lastMonth && s.CreatedAt?.Year == lastMonthYear);
            dashboard.SellersGrowthPercentage = CalculateGrowthPercentage(currentMonthSellers, lastMonthSellers);

            // Products growth
            var currentMonthProducts = products.Count(p => p.CreatedAt?.Month == currentMonth && p.CreatedAt?.Year == currentYear);
            var lastMonthProducts = products.Count(p => p.CreatedAt?.Month == lastMonth && p.CreatedAt?.Year == lastMonthYear);
            dashboard.ProductsGrowthPercentage = CalculateGrowthPercentage(currentMonthProducts, lastMonthProducts);
        }

        /// <summary>
        /// Tính toán growth stats cho Seller
        /// </summary>
        private void CalculateSellerGrowthStats(SellerDashboardVM dashboard, IEnumerable<dynamic> sellerOrders, List<ProductDashboardVM> products, int currentMonth, int currentYear, int lastMonth, int lastMonthYear)
        {
            // Orders growth
            var currentMonthOrders = sellerOrders.Count(o => o.Order.OrderDate.Month == currentMonth && o.Order.OrderDate.Year == currentYear);
            var lastMonthOrders = sellerOrders.Count(o => o.Order.OrderDate.Month == lastMonth && o.Order.OrderDate.Year == lastMonthYear);
            dashboard.OrdersGrowthPercentage = CalculateGrowthPercentage(currentMonthOrders, lastMonthOrders);

            // Revenue growth
            var currentMonthRevenue = sellerOrders
                .Where(o => o.Order.OrderDate.Month == currentMonth && o.Order.OrderDate.Year == currentYear)
                .SelectMany(o => (IEnumerable<dynamic>)o.SellerOrderItems)
                .Sum(oi => oi.TotalPrice);
            var lastMonthRevenue = sellerOrders
                .Where(o => o.Order.OrderDate.Month == lastMonth && o.Order.OrderDate.Year == lastMonthYear)
                .SelectMany(o => (IEnumerable<dynamic>)o.SellerOrderItems)
                .Sum(oi => oi.TotalPrice);
            dashboard.RevenueGrowthPercentage = CalculateGrowthPercentage(currentMonthRevenue, lastMonthRevenue);

            // Products growth
            var currentMonthProducts = products.Count(p => p.CreatedAt?.Month == currentMonth && p.CreatedAt?.Year == currentYear);
            var lastMonthProducts = products.Count(p => p.CreatedAt?.Month == lastMonth && p.CreatedAt?.Year == lastMonthYear);
            dashboard.ProductsGrowthPercentage = CalculateGrowthPercentage(currentMonthProducts, lastMonthProducts);
        }

        /// <summary>
        /// Tính toán % growth
        /// </summary>
        private decimal CalculateGrowthPercentage(decimal current, decimal previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return Math.Round(((current - previous) / previous) * 100, 2);
        }

        private decimal CalculateGrowthPercentage(int current, int previous)
        {
            return CalculateGrowthPercentage((decimal)current, (decimal)previous);
        }

        /// <summary>
        /// Lấy tên product
        /// </summary>
        private string GetTopProductName(int productId, List<ProductDashboardVM> allProducts)
        {
            var product = allProducts.FirstOrDefault(p => p.ProductId == productId);
            return product?.ProductName ?? "Unknown Product";
        }

        /// <summary>
        /// Thống kê theo tháng
        /// </summary>
        private List<MonthlyStatsVM> GetMonthlyStats(IEnumerable<dynamic> ordersWithDetails)
        {
            var monthlyStats = new List<MonthlyStatsVM>();
            var currentDate = DateTime.Now;
            
            for (int i = 11; i >= 0; i--)
            {
                var targetDate = currentDate.AddMonths(-i);
                var targetMonth = targetDate.Month;
                var targetYear = targetDate.Year;
                
                var monthOrders = ordersWithDetails.Where(o => 
                    o.Order.OrderDate.Month == targetMonth && 
                    o.Order.OrderDate.Year == targetYear).ToList();
                
                monthlyStats.Add(new MonthlyStatsVM
                {
                    MonthName = targetDate.ToString("MMM yyyy"),
                    Month = targetMonth,
                    Year = targetYear,
                    OrdersCount = monthOrders.Count,
                    Revenue = monthOrders.Sum(o => o.Order.TotalAmount),
                    NewCustomers = monthOrders.Select(o => o.Order.UserId).Distinct().Count(),
                    ProductsSold = monthOrders.SelectMany(o => (IEnumerable<dynamic>)o.OrderItems).Sum(oi => oi.Quantity)
                });
            }
            
            return monthlyStats;
        }

        /// <summary>
        /// Thống kê theo tháng cho Seller
        /// </summary>
        private List<MonthlyStatsVM> GetSellerMonthlyStats(IEnumerable<dynamic> sellerOrdersData)
        {
            var monthlyStats = new List<MonthlyStatsVM>();
            var currentDate = DateTime.Now;
            
            for (int i = 11; i >= 0; i--)
            {
                var targetDate = currentDate.AddMonths(-i);
                var targetMonth = targetDate.Month;
                var targetYear = targetDate.Year;
                
                var monthOrders = sellerOrdersData.Where(o => 
                    o.Order.OrderDate.Month == targetMonth && 
                    o.Order.OrderDate.Year == targetYear).ToList();
                
                monthlyStats.Add(new MonthlyStatsVM
                {
                    MonthName = targetDate.ToString("MMM yyyy"),
                    Month = targetMonth,
                    Year = targetYear,
                    OrdersCount = monthOrders.Count,
                    Revenue = monthOrders.SelectMany(o => (IEnumerable<dynamic>)o.SellerOrderItems).Sum(oi => oi.TotalPrice),
                    NewCustomers = monthOrders.Select(o => o.Order.UserId).Distinct().Count(),
                    ProductsSold = monthOrders.SelectMany(o => (IEnumerable<dynamic>)o.SellerOrderItems).Sum(oi => oi.Quantity)
                });
            }
            
            return monthlyStats;
        }

        /// <summary>
        /// Top products cho Admin
        /// </summary>
        private List<ProductStatsVM> GetTopProducts(IEnumerable<dynamic> ordersWithDetails, List<ProductDashboardVM> allProducts)
        {
            var productStats = ordersWithDetails
                .SelectMany(o => (IEnumerable<dynamic>)o.OrderItems)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice),
                    OrdersCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            return productStats.Select(ps => 
            {
                var product = allProducts.FirstOrDefault(p => p.ProductId == ps.ProductId);
                return new ProductStatsVM
                {
                    ProductId = ps.ProductId,
                    ProductName = product?.ProductName ?? "Unknown Product",
                    CategoryName = product?.CategoryName ?? "Unknown Category",
                    QuantitySold = ps.TotalSold,
                    Revenue = ps.TotalRevenue,
                    Price = product?.Price ?? 0,
                    Stock = product?.Quantity ?? 0,
                    StoreName = product?.SellerStoreName ?? "Unknown Store"
                };
            }).ToList();
        }

        /// <summary>
        /// Top products cho Seller
        /// </summary>
        private List<ProductStatsVM> GetSellerTopProducts(IEnumerable<dynamic> sellerOrdersData, List<ProductDashboardVM> sellerProducts)
        {
            var productStats = sellerOrdersData
                .SelectMany(o => (IEnumerable<dynamic>)o.SellerOrderItems)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice),
                    OrdersCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            return productStats.Select(ps => 
            {
                var product = sellerProducts.FirstOrDefault(p => p.ProductId == ps.ProductId);
                return new ProductStatsVM
                {
                    ProductId = ps.ProductId,
                    ProductName = product?.ProductName ?? "Unknown Product",
                    CategoryName = product?.CategoryName ?? "Unknown Category",
                    QuantitySold = ps.TotalSold,
                    Revenue = ps.TotalRevenue,
                    Price = product?.Price ?? 0,
                    Stock = product?.Quantity ?? 0,
                    StoreName = product?.SellerStoreName ?? "My Store"
                };
            }).ToList();
        }

        /// <summary>
        /// Top sellers
        /// </summary>
        private List<SellerStatsVM> GetTopSellers(IEnumerable<dynamic> ordersWithDetails, List<ProductDashboardVM> allProducts, List<SellerProfile> allSellers)
        {
            var sellerStats = ordersWithDetails
                .SelectMany(o => (IEnumerable<dynamic>)o.OrderItems)
                .Join(allProducts, 
                    oi => oi.ProductId, 
                    p => p.ProductId, 
                    (oi, p) => new { OrderItem = oi, Product = p })
                .Where(x => x.Product.SellerId.HasValue)
                .GroupBy(x => x.Product.SellerId.Value)
                .Select(g => new
                {
                    SellerId = g.Key,
                    TotalRevenue = g.Sum(x => x.OrderItem.TotalPrice),
                    OrdersCount = g.Count(),
                    ProductsSold = g.Sum(x => x.OrderItem.Quantity)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            return sellerStats.Select(ss => 
            {
                var seller = allSellers.FirstOrDefault(s => s.SellerId == ss.SellerId);
                return new SellerStatsVM
                {
                    SellerId = ss.SellerId,
                    StoreName = seller?.StoreName ?? "Unknown Store",
                    OwnerName = seller != null ? $"{seller.User?.FirstName} {seller.User?.LastName}".Trim() : "Unknown Owner",
                    Revenue = ss.TotalRevenue,
                    OrdersCount = ss.OrdersCount,
                    AverageOrderValue = ss.OrdersCount > 0 ? ss.TotalRevenue / ss.OrdersCount : 0,
                    IsVerified = seller?.IsVerified == true
                };
            }).ToList();
        }

        /// <summary>
        /// Top categories
        /// </summary>
        private List<CategoryStatsVM> GetTopCategories(IEnumerable<dynamic> ordersWithDetails, List<ProductDashboardVM> allProducts)
        {
            var categoryStats = ordersWithDetails
                .SelectMany(o => (IEnumerable<dynamic>)o.OrderItems)
                .Join(allProducts, 
                    oi => oi.ProductId, 
                    p => p.ProductId, 
                    (oi, p) => new { OrderItem = oi, Product = p })
                .Where(x => x.Product.CategoryId.HasValue)
                .GroupBy(x => x.Product.CategoryId.Value)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    CategoryName = g.First().Product.CategoryName,
                    TotalRevenue = g.Sum(x => x.OrderItem.TotalPrice),
                    OrdersCount = g.Select(x => x.OrderItem.OrderId).Distinct().Count(),
                    ProductsSold = g.Sum(x => x.OrderItem.Quantity)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            return categoryStats.Select(cs => new CategoryStatsVM
            {
                CategoryId = cs.CategoryId,
                CategoryName = cs.CategoryName ?? $"Category {cs.CategoryId}",
                Revenue = cs.TotalRevenue,
                OrdersCount = cs.OrdersCount,
                ProductsCount = allProducts.Count(p => p.CategoryId == cs.CategoryId)
            }).ToList();
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Xóa cache dashboard khi có cập nhật
        /// </summary>
        public async Task InvalidateDashboardCaches()
        {
            await _cacheService.DeleteByPatternAsync("AdminDashboardComplete");
            await _cacheService.DeleteByPatternAsync("SellerDashboardComplete_*");
            await _cacheService.DeleteByPatternAsync("DashboardStats_*");
            await _cacheService.DeleteByPatternAsync("AllProductsForDashboard");
            await _cacheService.DeleteByPatternAsync("SellerProductsForDashboard_*");
        }

        #endregion
    }

}