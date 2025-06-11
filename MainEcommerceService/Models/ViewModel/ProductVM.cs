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


