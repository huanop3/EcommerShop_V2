using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProductService.Models.ViewModel;

public class CartVM
{
    // Danh sách tĩnh để lưu trữ các sản phẩm trong giỏ hàng
    public static List<CartVM> CartItems { get; set; } = new List<CartVM>();

    public int? ProductId { get; set; }
    
    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    public int CartQuantity { get; set; } = 1;
    
    public string? PrimaryImageUrl { get; set; } // Ảnh chính
    public DateTime AddedAt { get; set; } = DateTime.Now;
    
    public DateTime? UpdatedAt { get; set; }

    // Calculated properties
    public decimal UnitPrice => DiscountPrice.HasValue && DiscountPrice > 0 && DiscountPrice < Price 
        ? DiscountPrice.Value 
        : Price;

    public decimal TotalPrice => UnitPrice * CartQuantity;

    public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice > 0 && DiscountPrice < Price;

    public decimal DiscountAmount => HasDiscount ? (Price - DiscountPrice.Value) * CartQuantity : 0;

    // Static methods for cart management
    
    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng
    /// </summary>
    public static void AddToCart(CartVM item)
    {
        var existingItem = CartItems.FirstOrDefault(x => 
            (x.ProductId.HasValue && item.ProductId.HasValue && x.ProductId == item.ProductId) ||
            (!x.ProductId.HasValue && !item.ProductId.HasValue && x.ProductName == item.ProductName));
        
        if (existingItem != null)
        {
            // Cập nhật số lượng nếu sản phẩm đã có trong giỏ
            existingItem.CartQuantity += item.CartQuantity;
            existingItem.UpdatedAt = DateTime.Now;
        }
        else
        {
            // Thêm sản phẩm mới
            CartItems.Add(item);
        }
    }

    /// <summary>
    /// Thêm sản phẩm từ ProductVM
    /// </summary>
    public static void AddToCart(PrdVMWithImages product, int quantity = 1)
    {
        var cartItem = new CartVM
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Description = product.Description,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            CartQuantity = quantity,
            AddedAt = DateTime.Now,
            PrimaryImageUrl = product.PrimaryImageUrl
        };

        AddToCart(cartItem);
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng - Overload với parameters riêng biệt
    /// </summary>
    public static void AddToCart(int productId, string productName, decimal currentPrice, decimal originalPrice, int quantity, string? primaryImageUrl = null)
    {
        var cartItem = new CartVM
        {
            ProductId = productId,
            ProductName = productName,
            Price = originalPrice,
            DiscountPrice = currentPrice != originalPrice ? currentPrice : null,
            CartQuantity = quantity,
            AddedAt = DateTime.Now,
            PrimaryImageUrl = primaryImageUrl
        };

        AddToCart(cartItem);
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng - Overload với tên sản phẩm
    /// </summary>
    public static void AddToCart(string productName, decimal currentPrice, decimal originalPrice, int quantity, string? primaryImageUrl = null)
    {
        var cartItem = new CartVM
        {
            ProductId = null,
            ProductName = productName,
            Price = originalPrice,
            DiscountPrice = currentPrice != originalPrice ? currentPrice : null,
            CartQuantity = quantity,
            AddedAt = DateTime.Now,
            PrimaryImageUrl = primaryImageUrl
        };

        AddToCart(cartItem);
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm trong giỏ
    /// </summary>
    public static void UpdateQuantity(int productId, int newQuantity)
    {
        var item = CartItems.FirstOrDefault(x => x.ProductId == productId);
        if (item != null && newQuantity > 0)
        {
            item.CartQuantity = newQuantity;
            item.UpdatedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm trong giỏ theo tên
    /// </summary>
    public static void UpdateQuantity(string productName, int newQuantity)
    {
        var item = CartItems.FirstOrDefault(x => x.ProductName == productName);
        if (item != null && newQuantity > 0)
        {
            item.CartQuantity = newQuantity;
            item.UpdatedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Xóa sản phẩm khỏi giỏ hàng theo ID
    /// </summary>
    public static void RemoveFromCart(int productId)
    {
        var item = CartItems.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            CartItems.Remove(item);
        }
    }

    /// <summary>
    /// Xóa sản phẩm khỏi giỏ hàng theo tên
    /// </summary>
    public static void RemoveFromCart(string productName)
    {
        var item = CartItems.FirstOrDefault(x => x.ProductName == productName);
        if (item != null)
        {
            CartItems.Remove(item);
        }
    }

    /// <summary>
    /// Lấy tất cả sản phẩm trong giỏ hàng
    /// </summary>
    public static List<CartVM> GetAllCartItems()
    {
        return CartItems.OrderBy(x => x.AddedAt).ToList();
    }

    /// <summary>
    /// Kiểm tra sản phẩm có trong giỏ hàng không (theo ID)
    /// </summary>
    public static bool IsInCart(int productId)
    {
        return CartItems.Any(x => x.ProductId == productId);
    }

    /// <summary>
    /// Kiểm tra sản phẩm có trong giỏ hàng không (theo tên)
    /// </summary>
    public static bool IsInCart(string productName)
    {
        return CartItems.Any(x => x.ProductName == productName);
    }

    /// <summary>
    /// Lấy sản phẩm trong giỏ theo ID
    /// </summary>
    public static CartVM? GetCartItem(int productId)
    {
        return CartItems.FirstOrDefault(x => x.ProductId == productId);
    }

    /// <summary>
    /// Lấy sản phẩm trong giỏ theo tên
    /// </summary>
    public static CartVM? GetCartItem(string productName)
    {
        return CartItems.FirstOrDefault(x => x.ProductName == productName);
    }

    /// <summary>
    /// Xóa tất cả sản phẩm trong giỏ hàng
    /// </summary>
    public static void ClearCart()
    {
        CartItems.Clear();
    }

    /// <summary>
    /// Đếm số lượng sản phẩm trong giỏ hàng (số items)
    /// </summary>
    public static int GetCartItemCount()
    {
        return CartItems.Count;
    }

    /// <summary>
    /// Đếm tổng số lượng sản phẩm trong giỏ hàng (tổng quantity)
    /// </summary>
    public static int GetTotalQuantity()
    {
        return CartItems.Sum(x => x.CartQuantity);
    }

    /// <summary>
    /// Tính tổng giá trị giỏ hàng (trước khi giảm giá)
    /// </summary>
    public static decimal GetSubTotal()
    {
        return CartItems.Sum(x => x.Price * x.CartQuantity);
    }

    /// <summary>
    /// Tính tổng giá trị giỏ hàng (sau khi giảm giá)
    /// </summary>
    public static decimal GetTotal()
    {
        return CartItems.Sum(x => x.TotalPrice);
    }

    /// <summary>
    /// Tính tổng số tiền tiết kiệm được
    /// </summary>
    public static decimal GetTotalSavings()
    {
        return CartItems.Sum(x => x.DiscountAmount);
    }

    /// <summary>
    /// Chuyển sản phẩm từ wishlist sang cart
    /// </summary>
    public static void MoveFromWishlistToCart(WishlistVM wishlistItem, int quantity = 1)
    {
        var cartItem = new CartVM
        {
            ProductId = wishlistItem.ProductId,
            ProductName = wishlistItem.ProductName,
            Description = wishlistItem.Description,
            Price = wishlistItem.Price,
            DiscountPrice = wishlistItem.DiscountPrice,
            CartQuantity = quantity,
            AddedAt = DateTime.Now,
            PrimaryImageUrl = wishlistItem.PrimaryImageUrl
        };

        AddToCart(cartItem);
    }
}