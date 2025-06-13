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
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal UsersGrowthPercentage { get; set; }
        public decimal SellersGrowthPercentage { get; set; }
        public decimal ProductsGrowthPercentage { get; set; }
        public decimal OrdersGrowthPercentage { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }
        public List<DashboardOrderVM> RecentOrders { get; set; } = new();
        public List<DashboardUserVM> NewUsers { get; set; } = new();
        public List<DashboardSellerVM> PendingSellers { get; set; } = new();
        public List<MonthlyStatsVM> MonthlyStats { get; set; } = new();
        public List<CategoryStatsVM> TopCategories { get; set; } = new();
        public List<ProductStatsVM> TopProducts { get; set; } = new();
        public List<SellerStatsVM> TopSellers { get; set; } = new();
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
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal ProductsGrowthPercentage { get; set; }
        public decimal OrdersGrowthPercentage { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }
        public List<DashboardOrderVM> RecentOrders { get; set; } = new();
        public List<ProductStatsVM> TopProducts { get; set; } = new();
        public List<MonthlyStatsVM> MonthlyStats { get; set; } = new();
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
    /// Monthly statistics for charts
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
    /// Category statistics
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
    /// Product statistics
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
    /// Seller statistics
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

    /// <summary>
    /// Dashboard basic stats for lightweight requests
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
    /// Product model for API response from ProductService
    /// </summary>
    public class ProductApiModel
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string ProductName { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? TotalSold { get; set; }
        public bool? IsDeleted { get; set; }
        public int SellerId { get; set; }
        public string CategoryName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }

    /// <summary>
    /// Category model for API response from ProductService
    /// </summary>
    public class CategoryApiModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool? IsDeleted { get; set; }
    }

    /// <summary>
    /// API Response model for ProductService
    /// </summary>
    public class APIResponseModel<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
        public DateTime DateTime { get; set; }
    }
}
