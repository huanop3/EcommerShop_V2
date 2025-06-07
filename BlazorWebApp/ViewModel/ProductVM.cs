using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductService.Models.ViewModel;

public class ProductVM
{
    public int ProductId { get; set; }

    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
    public string ProductName { get; set; } = null!;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Range(0.01, 999999.99, ErrorMessage = "Discount price must be between 0.01 and 999,999.99")]
    [DataType(DataType.Currency)]
    public decimal? DiscountPrice { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0")]
    public int Quantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Total sold must be greater than or equal to 0")]
    public int? TotalSold { get; set; }

    public bool? IsDeleted { get; set; }
    public int SellerId { get; set; }
}

