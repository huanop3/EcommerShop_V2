using System;
using System.Collections.Generic;

namespace ProductService.Models.ViewModel;

public class ProductVM
{
    public int ProductId { get; set; }

    public int CategoryId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    public int Quantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? TotalSold { get; set; }

    public bool? IsDeleted { get; set; }

    public int SellerId { get; set; }
    

}
public class PrdVMWithImages : ProductVM
{
    public string CategoryName { get; set; } = null!;
    public int ParentCategoryId { get; set; } // ID của danh mục cha, nếu có

    // Thông tin hình ảnh
    public string? PrimaryImageUrl { get; set; } // Ảnh chính
    public List<ProductImageInfo> Images { get; set; } = new List<ProductImageInfo>(); // Tất cả ảnh
    public int TotalImages { get; set; } // Tổng số ảnh
}
public class ProductImageInfo
{
    public int ImageId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}


