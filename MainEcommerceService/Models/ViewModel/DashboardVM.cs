using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.ViewModel
{
    /// <summary>
    /// Dashboard analytics cho Admin - SIMPLIFIED
    /// </summary>
    public class AdminDashboardVM
    {
        // Overview Stats
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        // Growth Stats (set default 0)
        public decimal UsersGrowthPercentage { get; set; }
        public decimal SellersGrowthPercentage { get; set; }
        public decimal ProductsGrowthPercentage { get; set; }
        public decimal OrdersGrowthPercentage { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }

        // Recent Activity
        public List<DashboardOrderVM> RecentOrders { get; set; } = new();
        public List<DashboardUserVM> NewUsers { get; set; } = new();
        public List<DashboardSellerVM> PendingSellers { get; set; } = new();

        // Analytics - Simplified
        public List<MonthlyStatsVM> MonthlyStats { get; set; } = new();
        public List<CategoryStatsVM> TopCategories { get; set; } = new();
        public List<ProductStatsVM> TopProducts { get; set; } = new();
        public List<SellerStatsVM> TopSellers { get; set; } = new();

        // System Health
        public int LowStockProductsCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public int VerificationPendingCount { get; set; }
    }

    /// <summary>
    /// Dashboard analytics cho Seller - SIMPLIFIED
    /// </summary>
    public class SellerDashboardVM
    {
        public int SellerId { get; set; }
        public string StoreName { get; set; } = "";

        // Overview Stats
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }

        // Growth Stats (set default 0)
        public decimal ProductsGrowthPercentage { get; set; }
        public decimal OrdersGrowthPercentage { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }

        // Recent Activity
        public List<DashboardOrderVM> RecentOrders { get; set; } = new();
        public List<ProductStatsVM> TopProducts { get; set; } = new();

        // Analytics
        public List<MonthlyStatsVM> MonthlyStats { get; set; } = new();

        // Alerts
        public int LowStockProductsCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public bool IsVerified { get; set; }
    }

    /// <summary>
    /// Dashboard order summary - SIMPLIFIED
    /// </summary>
    public class DashboardOrderVM
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
        public int ItemsCount { get; set; }
        public string TopProductName { get; set; } = "";
    }

    /// <summary>
    /// Dashboard user summary - SIMPLIFIED
    /// </summary>
    public class DashboardUserVM
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime JoinedDate { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Dashboard seller summary - SIMPLIFIED
    /// </summary>
    public class DashboardSellerVM
    {
        public int SellerId { get; set; }
        public string StoreName { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime ApplicationDate { get; set; }
        public bool IsVerified { get; set; }
        public int ProductsCount { get; set; }
    }

    /// <summary>
    /// Thống kê theo tháng - SIMPLIFIED
    /// </summary>
    public class MonthlyStatsVM
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = "";
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
        public int NewCustomers { get; set; }
        public int ProductsSold { get; set; }
    }

    /// <summary>
    /// Thống kê category - SIMPLIFIED
    /// </summary>
    public class CategoryStatsVM
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Thống kê sản phẩm - SIMPLIFIED
    /// </summary>
    public class ProductStatsVM
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string StoreName { get; set; } = "";
    }

    /// <summary>
    /// Thống kê seller - SIMPLIFIED
    /// </summary>
    public class SellerStatsVM
    {
        public int SellerId { get; set; }
        public string StoreName { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
        public bool IsVerified { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    // Keep existing DashboardStatsVM and ProductDashboardVM as they are simple
    /// <summary>
    /// ViewModel cho thống kê tổng quan - dùng cho API nhẹ
    /// </summary>
    public class DashboardStatsVM
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockProducts { get; set; }
        public int VerificationPending { get; set; }
        public int TotalSellers { get; set; }
    }

    /// <summary>
    /// ViewModel cho Product trong Dashboard
    /// </summary>
    public class ProductDashboardVM
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int? SellerId { get; set; }
        public string SellerStoreName { get; set; } = "";
        public string SellerName { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public bool? IsDeleted { get; set; }
        public string ImageUrl { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
    }
}
