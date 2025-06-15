using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProductService.Models.ViewModel;

public class WishlistVM
{
    // Danh sách tĩnh để lưu trữ các sản phẩm yêu thích
    public static List<WishlistVM> WishlistItems { get; set; } = new List<WishlistVM>();

    public int? ProductId { get; set; }
    
    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    public int Quantity { get; set; }
    public string? PrimaryImageUrl { get; set; } // Ảnh chính
    
    public DateTime AddedAt { get; set; } = DateTime.Now;

    // Phương thức thêm sản phẩm vào wishlist
    public static void AddToWishlist(WishlistVM item)
    {
        var existingItem = WishlistItems.FirstOrDefault(x => x.ProductName == item.ProductName);
        if (existingItem == null)
        {
            WishlistItems.Add(item);
        }
    }

    /// <summary>
    /// Thêm sản phẩm vào wishlist - Overload với parameters riêng biệt
    /// </summary>
    public static void AddToWishlist(int productId, string productName, decimal currentPrice, decimal originalPrice, string? primaryImageUrl = null)
    {
        var wishlistItem = new WishlistVM
        {
            ProductId = productId,
            ProductName = productName,
            Price = originalPrice,
            DiscountPrice = currentPrice != originalPrice ? currentPrice : null,
            Quantity = 1, // Default to 1 for availability
            AddedAt = DateTime.Now,
            PrimaryImageUrl = primaryImageUrl
        };

        AddToWishlist(wishlistItem);
    }

    /// <summary>
    /// Thêm sản phẩm vào wishlist - Overload với tên sản phẩm
    /// </summary>
    public static void AddToWishlist(string productName, decimal currentPrice, decimal originalPrice)
    {
        var wishlistItem = new WishlistVM
        {
            ProductId = null,
            ProductName = productName,
            Price = originalPrice,
            DiscountPrice = currentPrice != originalPrice ? currentPrice : null,
            Quantity = 1, // Default to 1 for availability
            AddedAt = DateTime.Now
        };

        AddToWishlist(wishlistItem);
    }

    // Phương thức xóa sản phẩm khỏi wishlist
    public static void RemoveFromWishlist(string productName)
    {
        var item = WishlistItems.FirstOrDefault(x => x.ProductName == productName);
        if (item != null)
        {
            WishlistItems.Remove(item);
        }
    }

    // Phương thức xóa sản phẩm khỏi wishlist theo ID
    public static void RemoveFromWishlist(int productId)
    {
        var item = WishlistItems.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            WishlistItems.Remove(item);
        }
    }

    // Phương thức lấy tất cả sản phẩm trong wishlist
    public static List<WishlistVM> GetAllWishlistItems()
    {
        return WishlistItems;
    }

    // Phương thức kiểm tra sản phẩm có trong wishlist không (theo tên)
    public static bool IsInWishlist(string productName)
    {
        return WishlistItems.Any(x => x.ProductName == productName);
    }

    // Phương thức kiểm tra sản phẩm có trong wishlist không (theo ID)
    public static bool IsInWishlist(int productId)
    {
        return WishlistItems.Any(x => x.ProductId == productId);
    }

    // Phương thức xóa tất cả sản phẩm trong wishlist
    public static void ClearWishlist()
    {
        WishlistItems.Clear();
    }

    // Phương thức đếm số lượng sản phẩm trong wishlist
    public static int GetWishlistCount()
    {
        return WishlistItems.Count;
    }
}

